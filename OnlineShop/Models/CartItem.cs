using OnlineShop.Models;
using System.ComponentModel.DataAnnotations;

public class CartItem
{
    public int Id { get; set; }

    [Required]
    public int CartId { get; set; }
    public required Cart Cart { get; set; }

    [Required]
    public int ProductId { get; set; }
    public required Product Product { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}