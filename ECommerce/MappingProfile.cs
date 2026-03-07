using System.Linq;
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
        // Property-based records have init setters — AutoMapper maps by name
        // automatically. Only custom/computed members need ForMember.
        CreateMap<Product, ProductResponseDto>();

        CreateMap<CreateProductRequestDto, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

        // Null-safe partial update: only overwrite when the source value is not null
        CreateMap<UpdateProductRequestDto, Product>()
            .ForAllMembers(opts =>
                opts.Condition((src, dest, srcMember) => srcMember != null));

        // ── Cart ──────────────────────────────────────────────────────────────
        // Computed members (ProductName, UnitPrice, Subtotal) don't exist on the
        // entity so they must be mapped explicitly with ForMember.
        CreateMap<CartItem, CartItemResponseDto>()
            .ForMember(dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product.Name))
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
                opt => opt.MapFrom(src => src.Status.ToString()));
    }
}