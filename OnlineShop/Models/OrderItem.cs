using OnlineShop.Models;
using System.ComponentModel.DataAnnotations;

public class OrderItem
{
    public int Id { get; set; }

    [Required]
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    // [Required]
    public int? ProductId { get; set; }

    public Product? Product { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }

    [Range(0, 1000)]
    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    [StringLength(500)]
    [Display(Name = "Uwagi")]
    public string? Notes { get; set; }
}
