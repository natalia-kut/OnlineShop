using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using OnlineShop.Models;

namespace OnlineShop.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string[] _permittedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        private const long _fileSizeLimit = 5 * 1024 * 1024; // 5 MB

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Products
        public async Task<IActionResult> Index(string? category)
        {
            var categories = await _context.Products
                .Select(p => p.Category)
                .Where(c => c != null)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var productsQuery = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                productsQuery = productsQuery.Where(p => p.Category == category);
            }

            var products = await productsQuery.ToListAsync();

            var vm = new ProductCategoriesViewModel
            {
                Products = products,
                Categories = categories,
                SelectedCategory = category
            };

            return View(vm);
        }

        // GET: Products/Manage
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage()
        {
            return View(await _context.Products.ToListAsync());
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();

            return View(product);
        }

        // GET: Products/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Price,Category,Stock,ImageUrl")] Product product, IFormFile imageFile)
        {
            if (imageFile != null)
            {
                var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext) || !_permittedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("imageFile", "Nieobsługiwany typ pliku. Dozwolone: .jpg, .jpeg, .png, .gif");
                }
                if (imageFile.Length > _fileSizeLimit)
                {
                    ModelState.AddModelError("imageFile", "Plik jest za duży (max 5 MB).");
                }
            }

            //if (product.Price < 0.01m || product.Price > 100000m)
            //{
            //    ModelState.AddModelError("Price", "Cena musi być w zakresie 0.01 – 100000.0");
            //}

            ModelState.Remove("imageFile");

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploads = Path.Combine(_env.WebRootPath, "images", "products");
                    Directory.CreateDirectory(uploads);
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                    var filePath = Path.Combine(uploads, fileName);
                    await using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    product.ImageUrl = $"/images/products/{fileName}";
                }
                else
                {
                    product.ImageUrl = "/images/placeholder.png";
                }

                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Products/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile imageFile)
        {
            if (id != product.Id)
                return NotFound();

            var productFromDb = await _context.Products.FindAsync(id);
            if (productFromDb == null)
                return NotFound();

            if (product.Price < 0.01m || product.Price > 100000m)
            {
                ModelState.AddModelError("Price", "Cena musi być w zakresie 0.01 – 100000.0");
            }

            ModelState.Remove("imageFile");

            if (!ModelState.IsValid)
                return View(product); 

            productFromDb.Name = product.Name;
            productFromDb.Description = product.Description;
            productFromDb.Price = product.Price;
            productFromDb.Category = product.Category;
            productFromDb.Stock = product.Stock;

            if (imageFile != null && imageFile.Length > 0)
            {
                var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!_permittedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("imageFile", "Nieobsługiwany typ pliku.");
                    return View(product);
                }

                var uploads = Path.Combine(_env.WebRootPath, "images", "products");
                Directory.CreateDirectory(uploads);

                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploads, fileName);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await imageFile.CopyToAsync(stream);

                if (!string.IsNullOrEmpty(productFromDb.ImageUrl) &&
                    productFromDb.ImageUrl.StartsWith("/images/products/"))
                {
                    var oldPath = Path.Combine(
                        _env.WebRootPath,
                        productFromDb.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
                    );

                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                productFromDb.ImageUrl = $"/images/products/{fileName}";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                if (!string.IsNullOrEmpty(product.ImageUrl) && product.ImageUrl.StartsWith("/images/products/", StringComparison.OrdinalIgnoreCase))
                {
                    var path = Path.Combine(_env.WebRootPath, product.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
