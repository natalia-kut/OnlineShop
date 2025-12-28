using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

public class Product
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [Required]
    [StringLength(500)]
    public required string Description { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Range(0.01, 100000)]
    public decimal Price { get; set; }

    [Required]
    [StringLength(50)]
    public required string Category { get; set; }

    [Range(0, 10000)]
    public int Stock { get; set; }

    public required string ImageUrl { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
