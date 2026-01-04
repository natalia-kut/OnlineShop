using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using OnlineShop.Models;

namespace OnlineShop.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _db;

        public OrdersController(ApplicationDbContext db) => _db = db;

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            if (!(User?.Identity?.IsAuthenticated ?? false))
                return Forbid();

            //only for ADMIN
            if (User.IsInRole("Admin"))
            {
                var allOrders = await _db.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
                return View(allOrders);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Forbid();

            var myOrders = await _db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(myOrders);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // only for ADMIN
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!(User.IsInRole("Admin") || order.UserId == userId))
                return Forbid();

            return View(order);
        }

        // GET: Orders/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Order posted)
        {
            if (id != posted.Id) return NotFound();

            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var postedItems = (posted.OrderItems ?? new List<OrderItem>()).ToList();

                var productIds = postedItems.Select(pi => pi.ProductId).Where(pid => pid != 0).Distinct().ToList();
                Dictionary<int, Product> products = productIds.Count != 0
                    ? await _db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id)
                    : new Dictionary<int, Product>();

                var toRemove = order.OrderItems
                    .Where(dbItem => !postedItems.Any(pi => pi.Id == dbItem.Id))
                    .ToList();
                if (toRemove.Any())
                {
                    _db.OrderItems.RemoveRange(toRemove);
                    foreach (var r in toRemove)
                    {
                        order.OrderItems.Remove(r);
                    }
                }

                foreach (var pItem in postedItems)
                {
                    var existing = order.OrderItems.FirstOrDefault(oi => oi.Id == pItem.Id);
                    if (existing != null)
                    {
                        existing.Quantity = pItem.Quantity;
                        existing.UnitPrice = pItem.UnitPrice;
                        existing.Notes = pItem.Notes;
                    }
                    else
                    {
                        if (pItem.Quantity >= 0)
                        {
                            var newOi = new OrderItem
                            {
                                OrderId = order.Id,
                                ProductId = pItem.ProductId,
                                Quantity = pItem.Quantity,
                                UnitPrice = pItem.UnitPrice,
                                Order = order,
                                Product = products.TryGetValue((int)pItem.ProductId, out var prod) ? prod : null!,
                                Notes = pItem.Notes
                            };
                            _db.OrderItems.Add(newOi);
                            order.OrderItems.Add(newOi);
                        }
                    }
                }

                order.Status = posted.Status ?? order.Status;

                order.TotalPrice = order.OrderItems.Sum(i => i.UnitPrice * i.Quantity);

                await _db.SaveChangesAsync();

                await tx.CommitAsync();
                return RedirectToAction("Details", "Orders", new { id = order.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync();
                ModelState.AddModelError("", "Konflikt przy zapisie. Spróbuj ponownie.");
                return View(order);
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                ModelState.AddModelError("", "Wystąpił błąd podczas aktualizacji zamówienia.");
                return View(order);
            }
        }

        // GET: Orders/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var order = await _db.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order != null)
            {
                if (order.OrderItems != null && order.OrderItems.Any())
                {
                    _db.OrderItems.RemoveRange(order.OrderItems);
                }

                _db.Orders.Remove(order);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}