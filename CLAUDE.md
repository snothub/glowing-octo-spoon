# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a containerized Identity Server solution using Duende IdentityServer with a custom login UI. The system consists of three main components:

1. **IdentityService** - Duende IdentityServer implementation (.NET 8)
2. **ClientApplication** - Sample client application (.NET 8)  
3. **idui** - Custom React-based login UI (TypeScript/Vite)

## Architecture

### Core Components

- **IdentityService**: Main identity provider using Duende IdentityServer 7.3.1
  - Configuration in `Config.cs` defines identity resources, API scopes, and clients
  - Custom authorization request validator (`DomainCustomAuthorizeRequestValidator`)
  - Custom resource store (`DomainResourceStore`)
  - External login UI integration via environment variables

- **ClientApplication**: Sample OAuth2/OIDC client
  - Uses Microsoft.AspNetCore.Authentication.OpenIdConnect
  - Shares `DomainConstants.cs` with IdentityService

- **idui**: React TypeScript SPA for login/consent UI
  - Built with Vite, served via nginx
  - Communicates with IdentityService via environment-configured API URL

### Docker Setup

The solution uses multi-stage Docker builds with three services:
- `identity`: IdentityService on port 7000 (configurable via `IDP_AUTHORITY_PORT`)
- `client`: ClientApplication on ports 5000/5001
- `login-app`: React UI on port 8001 (configurable via `IDP_LOGINAPP_PORT`)

All services use shared SSL certificates (`aspnetcore-dev-cert.pfx`).

## Development Commands

### .NET Projects
```bash
# Build solution
dotnet build "IdentityServer in Docker.sln"

# Run IdentityService locally
dotnet run --project IdentityService

# Run ClientApplication locally  
dotnet run --project ClientApplication

# Publish for production
dotnet publish IdentityService/IdentityService.csproj -c Release -o /app
dotnet publish ClientApplication/ClientApplication.csproj -c Release -o /app
```

### React UI (idui directory)
```bash
# Install dependencies
npm ci

# Run development server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview
```

### Docker Operations
```bash
# Start all services
docker-compose up --build

# Start specific service
docker-compose up identity
docker-compose up client
docker-compose up login-app

# View logs
docker-compose logs -f identity
```

## Environment Configuration

Key environment variables in `compose.yaml`:
- `IDP_AUTHORITY`: Identity server base URL
- `IDP_AUTHORITY_PORT`: Identity server port (default: 7000)
- `IDP_LOGINAPP_PORT`: Login app port (default: 8001)
- `IDP_LOGINURL`: Custom login URL endpoint
- `IDP_CONSENTURL`: Custom consent URL endpoint
- `VITE_API_URL`: API base URL for React app

## Key Files and Locations

- **Identity Configuration**: `IdentityService/Config.cs` - Identity resources, API scopes, clients
- **Domain Constants**: `IdentityService/DomainConstants.cs` - Shared constants across projects
- **Custom Validators**: `IdentityService/DomainCustomAuthorizeRequestValidator.cs`
- **React Components**: `idui/src/components/` - Login and Consent components
- **SSL Certificates**: `aspnetcore-dev-cert.pfx` - Shared across all services

## Testing and Debugging

- IdentityService runs on HTTPS (port 7001) and HTTP (port 7000 configurable)
- ClientApplication runs on ports 5000/5001
- React UI development server runs on port 3000
- All services log via Serilog with console output
- Certificate password: `MyPw123` (development only)

## External Provider Integration

The system supports external authentication providers through:
- Custom authorization request validator for domain-specific logic
- Configurable login/logout/consent URLs
- External login callback handling in `IdentityService/Pages/ExternalLogin/`