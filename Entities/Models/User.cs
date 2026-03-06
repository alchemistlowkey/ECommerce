using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Entities.Models;

public class User : IdentityUser
{
    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "Customer";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Cart? Cart { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }
}
