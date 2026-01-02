using Microsoft.AspNetCore.Identity;
public class ApplicationUser : IdentityUser
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

