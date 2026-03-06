using System;
using System.ComponentModel.DataAnnotations;

namespace Entities.Models;

public class CartItem
{
    [Key]
    public Guid Id { get; set; }
    public Guid CartId { get; set; }
    public Cart Cart { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Required]
    public int Quantity { get; set; }
}
