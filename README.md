# ECommerce API [![.NET](https://github.com/dotnet/core/workflows/.NET/badge.svg)](https://github.com/dotnet/core) [![Docker](https://img.shields.io/badge/Docker-%2300AEEF?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)

[![ASP.NET](https://img.shields.io/badge/ASP.NET-8-blueviolet)](https://dotnet.microsoft.com/) [![PostgreSQL](https://img.shields.io/badge/PostgreSQL-336791.svg?&style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/) [![EF Core](https://img.shields.io/badge/EntityFramework-6fc2d0.svg?style=for-the-badge&logo=data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMTIzIiBoZWlnaHQ9IjI0IiB2aWV3Qm94PSIwIDAgMTIzIDI0IiBmaWxsPSJub25lIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciPjxwYXRoIGQ9Ik0xMDIgMjBDMTAyIDIwIDEwMiAyMCAxMDIgMjBDMTAyIDIwIDEwMiAyMCAxMDIgMjAiLz48L3N2Zz4=)](https://learn.microsoft.com/ef/core/)

A robust, production-ready **ASP.NET Core 10** e-commerce backend API using Clean Architecture with Repository & Service patterns. Features JWT authentication, PostgreSQL with EF Core migrations, Paystack/Flutterwave payments, Serilog logging, and Scalar API documentation.

**Live Demo**: [Scalar API Reference](/scalar)

## 📁 Project Structure

```
ECommerce.sln
│
├── ECommerce/                          ← Main API project (Controllers + Startup)
│   ├── Program.cs                      ← Entry point & middleware pipeline
│   ├── MappingProfile.cs               ← AutoMapper profiles
│   ├── Extensions/
│   │   └── ServiceExtensions.cs        ← DI extensions (Auth, EF, Services)
│   ├── Middleware/
│   │   ├── GlobalExceptionHandlerMiddleware.cs
│   │   └── ErrorDetails.cs
│   └── SeedData.cs                     ← Populates test data & admin user
│
├── ECommerce.Presentation/             ← Controller assembly
│   └── Controllers/
│       ├── AuthController.cs
│       ├── ProductsController.cs
│       ├── CartController.cs
│       ├── OrdersController.cs
│       └── WebhookController.cs        ← Payment webhooks
│
├── Entities/                           ← Domain entities & configs
│   ├── Models/                         ← User, Product, Cart, Order
│   └── ConfigurationModels/            ← JWT, Paystack, Flutterwave settings
│
├── Repository/                         ← EF Core data access
│   ├── RepositoryContext.cs            ← DbContext (PostgreSQL)
│   ├── RepositoryManager.cs
│   ├── Configurations/                 ← Fluent API configs
│   └── Migrations/
│
├── Service/                            ← Business logic layer
│   └── ServiceManager.cs
│
├── Contracts/                          ← Repository & Service interfaces
└── Shared/                             ← DTOs (Auth, Product, Cart, Order)
```

**Dependency Flow**: `ECommerce → Service → Repository → Entities → Shared`

## 🚀 Quick Start

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

## 🛠️ Tech Stack

| Category  | Technologies                     |
| --------- | -------------------------------- |
| Framework | ASP.NET Core 8                   |
| ORM       | Entity Framework Core 8 (Npgsql) |
| Database  | PostgreSQL                       |
| Auth      | ASP.NET Identity + JWT           |
| Logging   | Serilog                          |
| Mapping   | AutoMapper                       |
| Docs      | Scalar + OpenAPI                 |
| Payments  | Paystack, Flutterwave            |
| Container | Docker                           |

## 🚀 Getting Started

### 1. Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL](https://www.postgresql.org/download/) or Docker
- [EF Core Tools](https://learn.microsoft.com/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`

### 2. Clone & Configure

```bash
git clone <repo>
cd ECommerce
cp ECommerce/appsettings.example.json ECommerce/appsettings.json
```

**Update `appsettings.json`**:

- `ConnectionStrings:sqlConnection`: PostgreSQL conn string (e.g., `Host=localhost;Database=ECommerce;Username=postgres;Password=pass`)
- `JwtSettings`: Secret (32+ chars), Issuer/Audience
- `Paystack`/`Flutterwave`: API keys, webhook secrets (use test keys)

### 3. Database & Run

```bash
cd ECommerce
dotnet ef database update 0    # Apply migrations (Repository project)
dotnet run                    # Starts on https://localhost:5001 | Seeds admin user
```

**Ports** (from launchSettings.json):

- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5002`

### 4. API Documentation

- [Scalar UI](https://localhost:5001/scalar) ← Interactive docs + auth token input
- OpenAPI JSON: `/openapi/v1.json`

## 🐳 Docker Support

```bash
# Build
docker build -t ecom-api .

# Run (with PostgreSQL volume/DB)
docker run -d -p 5001:5001 --name ecom-api \
  -e ConnectionStrings__sqlConnection="Host=host.docker.internal;Database=ECommerce;Username=postgres;Password=pass" \
  ecom-api
```

**Tips**: Use `--no-restore` flag cautiously in Dockerfile; this repo's build handles it correctly.

## 🔌 API Endpoints

| Method | Endpoint                   | Auth  | Description                      |
| ------ | -------------------------- | ----- | -------------------------------- |
| POST   | `/api/auth/register`       | None  | Register user                    |
| POST   | `/api/auth/login`          | None  | Login (returns JWT)              |
| GET    | `/api/products`            | None  | List products (search, paginate) |
| GET    | `/api/products/{id}`       | None  | Get product                      |
| POST   | `/api/products`            | Admin | Create product                   |
| PUT    | `/api/products/{id}`       | Admin | Update product                   |
| DELETE | `/api/products/{id}`       | Admin | Delete product                   |
| POST   | `/api/cart/add`            | User  | Add to cart                      |
| GET    | `/api/cart`                | User  | Get cart                         |
| POST   | `/api/orders/checkout`     | User  | Checkout (Paystack/Flutterwave)  |
| POST   | `/api/webhook/paystack`    | None  | Paystack webhook                 |
| POST   | `/api/webhook/flutterwave` | None  | Flutterwave webhook              |

## 💫 Quickstart Examples

**1. Register & Login**

```bash
# Register
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Password123!","name":"Test User"}'

# Login (copy JWT from response)
TOKEN=$(curl -s -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Password123!"}' | jq -r '.token')

# Get Products
curl https://localhost:5001/api/products \
  -H "Authorization: Bearer $TOKEN"
```

**Admin seeded**: `admin@ecom.local` / `Admin123!`

## 🧪 Testing

No tests yet. Add xUnit + Moq:

```bash
dotnet new xunit -n ECommerce.Tests
dotnet add ECommerce.Tests package Moq
dotnet add ECommerce.Tests package Microsoft.NET.Test.Sdk
```

## 🚀 Deployment

- **Azure App Service**: Publish via `dotnet publish -c Release`, zip deploy
- **Docker/K8s**: Use multi-stage Dockerfile
- **Variables**: DB conn, JWT secret, payment keys

Set `ASPNETCORE_ENVIRONMENT=Production`, remove dev CORS.

## 🤝 Contributing

1. Fork → Clone → Create branch (`feat/add-tests`)
2. `dotnet ef migrations add <name>` → `dotnet ef database update`
3. Commit → PR to `main`

## 📄 License

MIT License - see [LICENSE](LICENSE) (create if missing).

![ECommerce](https://roadmap.sh/projects/ecommerce-api)

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
