using AutoMapper;
using Entities.Models;
using Shared.DataTransferObjects.Cart;
using Shared.DataTransferObjects.Order;
using Shared.DataTransferObjects.Product;

namespace ECommerce;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ── Product ───────────────────────────────────────────────────────────
        CreateMap<Product, ProductResponseDto>();

        CreateMap<CreateProductRequestDto, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

        // UpdateProductRequestDto → Product mapping is intentionally omitted.
        // ProductService.UpdateProductAsync applies fields manually with HasValue
        // guards so value-type defaults (0, false) never silently overwrite real data.

        // ── Cart ──────────────────────────────────────────────────────────────
        CreateMap<CartItem, CartItemResponseDto>()
            .ForMember(dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(dest => dest.ImageUrl,
                opt => opt.MapFrom(src => src.Product.ImageUrl ?? string.Empty))
            .ForMember(dest => dest.Category,
                opt => opt.MapFrom(src => src.Product.Category ?? string.Empty))
            .ForMember(dest => dest.UnitPrice,
                opt => opt.MapFrom(src => src.Product.Price))
            .ForMember(dest => dest.Subtotal,
                opt => opt.MapFrom(src => src.Product.Price * src.Quantity));

        CreateMap<Cart, CartResponseDto>()
            .ForMember(dest => dest.Total,
                opt => opt.MapFrom(src =>
                    src.Items.Sum(i => i.Product.Price * i.Quantity)));

        // ── Order ─────────────────────────────────────────────────────────────
        CreateMap<OrderItem, OrderItemResponseDto>()
            .ForMember(dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(dest => dest.Subtotal,
                opt => opt.MapFrom(src => src.UnitPrice * src.Quantity));

        CreateMap<Order, OrderResponseDto>()
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.PaymentProvider,
                opt => opt.MapFrom(src => src.PaymentProvider));
    }
}
