using System;
using System.ComponentModel.DataAnnotations;

namespace Entities.Models;

public class Cart
{
    [Key]
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
