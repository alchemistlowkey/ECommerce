using System;
using System.Text.Json;

namespace ECommerce.Middleware;

public class ErrorDetails
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }

    public override string ToString() => JsonSerializer.Serialize(this);
}
