# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution/config and project files first so `dotnet restore` is cached
# separately from source changes.
COPY Arunika.slnx NuGet.Config ./
COPY src/Arunika.Api/Arunika.Api.csproj src/Arunika.Api/
COPY src/Arunika.Application/Arunika.Application.csproj src/Arunika.Application/
COPY src/Arunika.Domain/Arunika.Domain.csproj src/Arunika.Domain/
COPY src/Arunika.Infrastructure/Arunika.Infrastructure.csproj src/Arunika.Infrastructure/
RUN dotnet restore src/Arunika.Api/Arunika.Api.csproj

COPY src/ src/
RUN dotnet publish src/Arunika.Api/Arunika.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Render (and most container platforms) route external traffic to a fixed
# container port. 8080 is bound here and exposed; no code change needed.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Arunika.Api.dll"]
