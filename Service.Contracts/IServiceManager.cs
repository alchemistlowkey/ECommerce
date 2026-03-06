using System;

namespace Service.Contracts;

public interface IServiceManager
{
    IAuthService Auth { get; }
    IProductService Product { get; }
    ICartService Cart { get; }
    IOrderService Order { get; }
}
