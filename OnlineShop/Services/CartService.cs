using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace OnlineShop.Services
{
    public class CartService
    {
        private const string CartCookieName = "CartId";
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _http;

        public CartService(ApplicationDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        private HttpContext Context => _http.HttpContext!;

        public async Task<Cart> GetOrCreateCartAsync()
        {
            var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
            {
                var userCart = await _db.Carts
                    .Include(c => c.Items).ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                var cookieCart = await GetCookieCartAsync();

                if (cookieCart != null && userCart == null)
                {
                    cookieCart.UserId = userId;
                    cookieCart.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                    DeleteCartCookie();
                    return cookieCart;
                }

                if (cookieCart != null && userCart != null && cookieCart.Id != userCart.Id)
                {
                    foreach (var ci in cookieCart.Items)
                    {
                        var existing = userCart.Items.FirstOrDefault(x => x.ProductId == ci.ProductId);
                        if (existing != null)
                        {
                            existing.Quantity += ci.Quantity;
                        }
                        else
                        {
                            ci.CartId = userCart.Id;
                            ci.Cart = userCart;
                            userCart.Items.Add(new CartItem
                            {
                                CartId = userCart.Id,
                                Cart = userCart,
                                ProductId = ci.ProductId,
                                Product = ci.Product,
                                Quantity = ci.Quantity,
                                UnitPrice = ci.UnitPrice
                            });
                        }
                    }

                    _db.CartItems.RemoveRange(cookieCart.Items);
                    _db.Carts.Remove(cookieCart);
                    userCart.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                    DeleteCartCookie();
                    return userCart;
                }

                if (userCart != null) return userCart;

                var newCart = new Cart { UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                _db.Carts.Add(newCart);
                await _db.SaveChangesAsync();
                return newCart;
            }

            var cookieOnlyCart = await GetCookieCartAsync();
            if (cookieOnlyCart != null) return cookieOnlyCart;

            var anonCart = new Cart { UserId = null, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _db.Carts.Add(anonCart);
            await _db.SaveChangesAsync();
            SetCartCookie(anonCart.Id);
            return anonCart;
        }

        private async Task<Cart?> GetCookieCartAsync()
        {
            if (Context.Request.Cookies.TryGetValue(CartCookieName, out var value) && int.TryParse(value, out var cartId))
            {
                return await _db.Carts
                    .Include(c => c.Items).ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.Id == cartId);
            }
            return null;
        }

        private void SetCartCookie(int cartId)
        {
            var opts = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = false,
                IsEssential = true
            };
            Context.Response.Cookies.Append(CartCookieName, cartId.ToString(), opts);
        }

        private void DeleteCartCookie()
        {
            Context.Response.Cookies.Delete(CartCookieName);
        }

        public async Task AddToCartAsync(int productId, int quantity = 1)
        {
            var product = await _db.Products.FindAsync(productId);
            if (product == null) throw new InvalidOperationException("Produkt nie istnieje.");

            quantity = Math.Max(1, quantity);

            if (product.Stock <= 0) throw new InvalidOperationException("Produkt niedostêpny w magazynie.");
            if (quantity > product.Stock) throw new InvalidOperationException($"Brak wystarczaj¹cej iloœci w magazynie. Dostêpnych: {product.Stock}.");

            var cart = await GetOrCreateCartAsync();

            var item = await _db.CartItems.FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == productId);
            if (item == null)
            {
                item = new CartItem
                {
                    CartId = cart.Id,
                    Cart = cart,
                    ProductId = productId,
                    Product = product,
                    Quantity = quantity,
                    UnitPrice = product.Price
                };
                _db.CartItems.Add(item);
            }
            else
            {
                var newQty = item.Quantity + quantity;
                if (newQty > product.Stock)
                {
                    var availableToAdd = product.Stock - item.Quantity;
                    if (availableToAdd <= 0)
                        throw new InvalidOperationException("Nie mo¿esz dodaæ wiêcej — osi¹gniêto maksymalny stan magazynowy dla tego produktu.");
                    throw new InvalidOperationException($"Mo¿esz dodaæ maksymalnie {availableToAdd} sztuk wiêcej (dostêpnych: {product.Stock}).");
                }
                item.Quantity = newQty;
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            SetCartCookie(cart.Id);
        }

        public async Task RemoveFromCartAsync(int cartItemId)
        {
            var cart = await GetOrCreateCartAsync();
            var item = await _db.CartItems.FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.CartId == cart.Id);
            if (item == null) return;
            _db.CartItems.Remove(item);
            cart.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task ClearCartAsync()
        {
            var cart = await GetOrCreateCartAsync();
            _db.CartItems.RemoveRange(_db.CartItems.Where(ci => ci.CartId == cart.Id));
            cart.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            DeleteCartCookie();
        }

        public async Task<List<CartItem>> GetCartItemsAsync()
        {
            var cart = await GetOrCreateCartAsync();
            return await _db.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.CartId == cart.Id)
                .ToListAsync();
        }

        public async Task<decimal> GetCartTotalAsync()
        {
            var items = await GetCartItemsAsync();
            return items.Sum(i => i.UnitPrice * i.Quantity);
        }
    }
}