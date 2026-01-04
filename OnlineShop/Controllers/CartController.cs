using Microsoft.AspNetCore.Mvc;
using OnlineShop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Models;

namespace OnlineShop.Controllers
{
    public class CartController(CartService cartService, ApplicationDbContext db) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var items = await cartService.GetCartItemsAsync();
            ViewBag.Total = await cartService.GetCartTotalAsync();
            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int productId, int quantity = 1, string? returnUrl = null)
        {
            var product = await db.Products.FindAsync(productId);
            if (product == null) return NotFound();

            try
            {
                await cartService.AddToCartAsync(productId, quantity);
            }
            catch (InvalidOperationException ex)
            {
                TempData["CartError"] = ex.Message;

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return LocalRedirect(returnUrl);

                return RedirectToAction("Details", "Products", new { id = productId });
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            await cartService.RemoveFromCartAsync(cartItemId);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var items = await cartService.GetCartItemsAsync();
            if (items.Count != 0)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Forbid();

                var user = await db.Users.FindAsync(userId);
                if (user == null) return Forbid();

                using var transaction = await db.Database.BeginTransactionAsync();
                try
                {
                    var productIds = items.Select(i => i.ProductId).Distinct().ToList();
                    var products = await db.Products
                        .Where(p => productIds.Contains(p.Id))
                        .ToDictionaryAsync(p => p.Id);

                    foreach (var ci in items)
                    {
                        if (!products.TryGetValue(ci.ProductId, out var product))
                        {
                            TempData["CartError"] = $"Produkt #{ci.ProductId} nie istnieje.";
                            await transaction.RollbackAsync();
                            return RedirectToAction(nameof(Index));
                        }

                        if (product.Stock < ci.Quantity)
                        {
                            TempData["CartError"] = $"Brak wystarczaj¹cej iloœci produktu \"{product.Name}\". Dostêpnych: {product.Stock}.";
                            await transaction.RollbackAsync();
                            return RedirectToAction(nameof(Index));
                        }
                    }

                    var order = new Order
                    {
                        UserId = userId,
                        User = user,
                        OrderDate = DateTime.UtcNow,
                        Status = "Nowe",
                        TotalPrice = items.Sum(i => i.UnitPrice * i.Quantity)
                    };

                    db.Orders.Add(order);

                    foreach (var ci in items)
                    {
                        var product = products[ci.ProductId];

                        var oi = new OrderItem
                        {
                            Order = order,
                            ProductId = ci.ProductId,
                            Product = product,
                            ProductName = product.Name,
                            ProductImageUrl = product.ImageUrl,
                            Quantity = ci.Quantity,
                            UnitPrice = ci.UnitPrice
                        };
                        db.OrderItems.Add(oi);

                        product.Stock -= ci.Quantity;
                        db.Products.Update(product);
                    }

                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    await cartService.ClearCartAsync();

                    return RedirectToAction("Details", "Orders", new { id = order.Id });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["CartError"] = "Wyst¹pi³ b³¹d podczas sk³adania zamówienia. Spróbuj ponownie.";
                    return RedirectToAction(nameof(Index));
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}