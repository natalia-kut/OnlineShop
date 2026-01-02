using System.ComponentModel.DataAnnotations;

public class Order
{
    public int Id { get; set; }
    [Display(Name = "Data zamówienia")]
    public DateTime OrderDate { get; set; } = DateTime.Now;

    [Required]
    public required string UserId { get; set; }
    public required ApplicationUser User { get; set; }

    [Required]
    [StringLength(30)]
    public required string Status { get; set; }
    [Display(Name = "Kwota całkowita")] 
    public decimal TotalPrice { get; set; }

    public List<OrderItem> OrderItems { get; set; } = new();
}
