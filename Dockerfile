# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy all project files first so dotnet restore is cached as its own layer.
# If no .csproj changes, Docker reuses this cached layer on subsequent builds.
COPY ECommerce.slnx .
COPY ECommerce/ECommerce.csproj                         ECommerce/
COPY ECommerce.Presentation/ECommerce.Presentation.csproj ECommerce.Presentation/
COPY Entities/Entities.csproj                           Entities/
COPY Contracts/Contracts.csproj                         Contracts/
COPY Repository/Repository.csproj                       Repository/
COPY Service/Service.csproj                             Service/
COPY Service.Contracts/Service.Contracts.csproj         Service.Contracts/
COPY Shared/Shared.csproj                               Shared/

RUN dotnet restore

# Copy source and publish
COPY . .
RUN dotnet publish ECommerce/ECommerce.csproj -c Release -o /app/publish --no-restore

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# ASP.NET Core listens on 8080 by default since .NET 8+
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ECommerce.dll"]