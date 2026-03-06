using System;

namespace Entities.Models;

public enum OrderStatus
{
    Pending,
    PaymentProcessing,
    Paid,
    Shipped,
    Delivered,
    Cancelled
}
