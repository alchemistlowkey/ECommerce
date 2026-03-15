# ECommerce API

This repository contains a simple e‑commerce backend built with **ASP.NET Core 8** using a layered architecture. The solution is divided into multiple projects to separate concerns and make the codebase easier to maintain and test.

---

## 📁 Project Structure

```
ECommerce.sln
│
├── ECommerce/                          ← Main API project (startup)
│   ├── Program.cs                      ← application entry point
│   ├── MappingProfile.cs               ← AutoMapper configuration
│   ├── Extensions/
│   │   └── ServiceExtensions.cs        ← DI registration helpers
│   ├── Middleware/                     ← HTTP middleware helpers
│   │   ├── ErrorDetails.cs
│   │   └── GlobalExceptionHandlerMiddleware.cs
│   └── ContextFactory/
│       └── RepositoryContextFactory.cs ← design‑time DbContext
│
├── ECommerce.Presentation/             ← API controllers only
│   └── Controllers/
│       ├── AuthController.cs
│       ├── ProductsController.cs
│       ├── CartController.cs
│       └── OrdersController.cs
│
├── Entities/                           ← Domain models & configuration
│   ├── Models/
│   │   ├── User.cs
│   │   ├── Product.cs
│   │   ├── Cart.cs
│   │   ├── CartItem.cs
│   │   ├── Order.cs
│   │   └── OrderItem.cs
│   └── ConfigurationModels/
│       ├── JwtConfiguration.cs         ← JWT options bound from configuration
│       ├── PaymentSettings.cs          ← base class for payment providers
│       ├── PaystackSettings.cs         ← Paystack-specific options
│       └── FlutterwaveSettings.cs      ← Flutterwave-specific options
│
├── Contracts/                          ← Repository interfaces
│   ├── IRepositoryBase.cs
│   ├── IRepositoryManager.cs
│   ├── IUserRepository.cs
│   ├── IProductRepository.cs
│   ├── ICartRepository.cs
│   └── IOrderRepository.cs
│
├── Repository/                         ← EF Core implementations
│   ├── Configuration/                  ← Fluent API entity configs
│   │   ├── UserConfiguration.cs
│   │   ├── ProductConfiguration.cs
│   │   ├── CartConfiguration.cs
│   │   ├── OrderConfiguration.cs
│   │   └── OrderItemConfiguration.cs
│   ├── RepositoryContext.cs
│   ├── RepositoryBase.cs
│   ├── RepositoryManager.cs
│   ├── UserRepository.cs
│   ├── ProductRepository.cs
│   ├── CartRepository.cs
│   └── OrderRepository.cs
│
├── Service.Contracts/                  ← Service interfaces
│   ├── IServiceManager.cs
│   ├── IAuthService.cs
│   ├── IProductService.cs
│   ├── ICartService.cs
│   ├── IOrderService.cs
│   └── IPaymentService.cs
│
├── Service/                            ← Business logic implementations
│   ├── ServiceManager.cs
│   ├── AuthService.cs
│   ├── ProductService.cs
│   ├── CartService.cs
│   └── OrderService.cs
│
└── Shared/                             ← Data transfer objects (DTOs)
    └── DataTransferObjects/
        ├── Auth/
        │   ├── RegisterRequest.cs
        │   ├── LoginRequest.cs
        │   └── AuthResponse.cs
        ├── Product/
        │   ├── CreateProductRequest.cs
        │   ├── UpdateProductRequest.cs
        │   └── ProductResponse.cs
        ├── Cart/
        │   ├── AddToCartRequest.cs
        │   ├── UpdateCartItemRequest.cs
        │   ├── CartResponse.cs
        │   └── CartItemResponse.cs
        └── Order/
            ├── CheckoutResponse.cs
            └── OrderResponse.cs
```

Dependencies between projects (high‑level):

```
ECommerce              → Service, Repository, ECommerce.Presentation
ECommerce.Presentation → Service.Contracts
Service                → Service.Contracts, Contracts
Repository             → Contracts, Entities
Contracts              → Shared, Entities
Service.Contracts      → Shared, Entities
Entities               ← Shared
Shared                 ← (no dependencies)
```

---

## 🚀 Getting Started

1. **Requirements**
   - .NET SDK 10.0
   - SQL Server (localdb or container)
   - Optional: [dotnet-ef](https://learn.microsoft.com/ef/core/cli/dotnet) for migrations

2. **Configuration**
   - Copy `appsettings.example.json` to `appsettings.json` and adjust connection strings,
     JWT and payment settings (Paystack/Flutterwave).

3. **Database**
   ```bash
   cd ECommerce
   dotnet ef database update               # apply migrations
   dotnet run                              # seeds data on startup
   ```

4. **Run the API**
   ```bash
   cd ECommerce
   dotnet run
   ```

   The server listens on `https://localhost:5001` by default.

5. **API Documentation**
   Swagger is enabled by default. Visit `/swagger` after starting the app.

### 🐳 Docker

A `Dockerfile` is provided for creating a containerized build. See the file for
a typical multi‑stage build; key points:

- copy project files first, run `dotnet restore` to leverage layer caching
- copy remaining source, then `dotnet publish` to produce the runtime output

When running `dotnet publish` inside the build stage **do not** use
`--no-restore`. Omitting the flag ensures all files (including any resource
files) are available; a stale restore can trigger the following error during
container builds:

```
MSB3552: Resource file "**/*.resx" cannot be found.
```

The Dockerfile in this repo already calls publish without that option.

---

## 🛠️ Features

- User registration & authentication (JWT)
- CRUD for products
- Shopping cart management
- Checkout & order processing
- Payment integration (Paystack and Flutterwave)
- Layered architecture with repository and service patterns
- AutoMapper for DTO mapping
- Global exception handling middleware

---

## 📝 Notes

- The `SeedData` class populates sample products and a test user when the database is empty.
- Services are registered via `ServiceExtensions`.
- Presentation layer contains only controllers; business logic lives in `Service`.

---

## 📄 License

[roadmap](https://roadmap.sh/projects/ecommerce-api)
