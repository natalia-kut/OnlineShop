namespace OnlineShop.Models
{
    using System.Collections.Generic;

    public class ProductCategoriesViewModel
    {
        public required List<Product> Products { get; set; }
        public required List<string> Categories { get; set; }
        public string? SelectedCategory { get; set; }
    }
}