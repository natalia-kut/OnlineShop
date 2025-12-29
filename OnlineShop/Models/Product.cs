using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Product
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Nazwa")]
    public required string Name { get; set; }

    [Required]
    [StringLength(500)]
    [Display(Name = "Opis")]
    public required string Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    [Display(Name = "Cena")]
    public decimal Price { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Kategoria")]
    public required string Category { get; set; }

    [Required]
    [Range(0, 10000)]
    [Display(Name = "Ilość w magazynie")]
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}