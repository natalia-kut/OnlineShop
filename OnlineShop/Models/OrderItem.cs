using System.ComponentModel.DataAnnotations;

public class OrderItem
{
    public int Id { get; set; }

    [Required]
    public int OrderId { get; set; }
    public required Order Order { get; set; }

    [Required]
    public int ProductId { get; set; }
    public required Product Product { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}
