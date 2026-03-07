using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Entities.Models;

public class Order
{
    [Key]
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Required]
    [MaxLength(50)]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [MaxLength(255)]
    public string? StripePaymentIntentId { get; set; }

    [MaxLength(255)]
    public string? PaystackReference { get; set; }

    public string? PaymentProvider { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
