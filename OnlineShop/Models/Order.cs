using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class Order
{
    public int Id { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;

    [Required]
    public required string UserId { get; set; }
    public required ApplicationUser User { get; set; }

    [Required]
    [StringLength(30)]
    public required string Status { get; set; }

    public decimal TotalPrice { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
