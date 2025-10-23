# Smartstore Project - Claude Code Configuration

## Project Overview
Smartstore is a cross-platform, modular, scalable and ultra-fast open source all-in-one eCommerce platform based on ASP.NET Core 9, Entity Framework Core 9, Vue.js, Sass, Bootstrap and more.

## Technology Stack
- **Framework**: ASP.NET Core 9
- **Database**: Entity Framework Core 9
- **Frontend**: Vue.js, Sass, Bootstrap
- **Architecture**: Domain Driven Design, Modular Design
- **Supported Databases**: SQL Server, MySQL, PostgreSQL, SQLite

## Project Structure
- `src/Smartstore` - Core framework and utilities
- `src/Smartstore.Data` - Database providers
- `src/Smartstore.Core` - Business logic modules (catalog, checkout, identity, etc.)
- `src/Smartstore.Web.Common` - Web infrastructure components
- `src/Smartstore.Modules` - Plugin/module projects
- `src/Smartstore.Web` - Main web application entry point

## Key Build Commands
- **Solution File**: `Smartstore.sln`
- **Build Scripts**: `build.cmd` (Windows), `build.sh` (Linux/Mac), `build.ps1` (PowerShell)
- **Main Project**: `src/Smartstore.Web/Smartstore.Web.csproj`

## Development Setup
1. Clone repository
2. Open `Smartstore.sln` in Visual Studio 2022
3. Set `Smartstore.Web` as startup project
4. Build and run

## Testing
- **Test Projects**: 4 comprehensive test projects
  - `Smartstore.Core.Tests` - NUnit, Moq, In-Memory EF
  - `Smartstore.Web.Tests` - Web layer testing
  - `Smartstore.Tests` - General framework tests
  - `Smartstore.Test.Common` - Shared test utilities

## Deployment
- Supports Docker deployment with scripts in `build/` directory
- Can be published as self-contained application
- Supports multiple platforms: Windows, Linux, macOS

## Current Branch
- Working on: `6.2.0.x_CRSOFT`
- Main branch: `main`

## Code Quality Assessment (8.5/10)

### Architecture & Patterns
- **Excellent Modularity**: Clear separation with 26+ modules
- **Service Layer**: 87 services with corresponding interfaces
- **Hook System**: 79 hooks for cross-cutting concerns
- **Repository Pattern**: Clean abstraction over Entity Framework
- **Domain-Driven Design**: Well-structured domain models

### Dependency Management
- **Modern Stack**: .NET 9, Entity Framework Core 9
- **Build Automation**: Nuke build system
- **Multi-Platform**: Windows, Linux, macOS support
- **Package Management**: Structured .props/.targets files

### Data Layer
- **Entity Framework**: Fluent API with separate mapping classes
- **Soft Delete**: Global query filters
- **Database Optimization**: Strategic indexes
- **Hook-based Validation**: Automated data integrity

### Error Handling & Logging
- **Structured Logging**: Serilog with 71 logger instances
- **Exception Handling**: 159 catch blocks across 66 files
- **Centralized Logging**: TraceLogger, RequestLoggingMiddleware

## Plugin System Architecture

### Plugin Structure
```
src/Smartstore.Modules/[PluginName]/
├── module.json             # Plugin metadata
├── Module.cs               # Entry point (IModule)
├── Startup.cs              # Service registration (StarterBase)
├── Providers/              # Provider implementations
├── Controllers/            # MVC Controllers
├── Components/             # View Components
├── Models/                 # ViewModels/DTOs
├── Configuration/          # Settings classes
├── Localization/           # Language resources (XML)
├── Views/                  # Razor Views
└── wwwroot/               # Static assets
```

### Plugin Metadata (module.json)
- **Schema Validation**: JSON Schema with required fields
- **Categories**: Payment, Shipping, Tax, Analytics, CMS, etc.
- **Dependencies**: Module dependency management
- **Versioning**: MinAppVersion compatibility
- **Localization**: ResourceRootKey for namespacing

### Plugin Loading Process
1. **Discovery**: Scan all module.json files
2. **Validation**: Schema validation and dependency checks
3. **Loading**: Assembly loading with isolation
4. **Registration**: Auto-register services and providers
5. **Installation**: Execute pending module installations

### Key Plugin Interfaces
- **IModule**: Install/Uninstall lifecycle management
- **ModuleBase**: Base class with helper methods
- **StarterBase**: Service registration and pipeline configuration
- **IModuleDescriptor**: Metadata access interface

### Plugin Features
- **Auto-Discovery**: No manual registration required
- **Hot-Reload**: Runtime installation/uninstallation
- **Provider Pattern**: Extensible service architectures
- **Asset Integration**: Automatic bundle extension
- **Localization**: Multi-language resource management
- **Settings Management**: Typed configuration classes
- **Dependency Injection**: Seamless ASP.NET Core DI integration

### Example Plugin Entry Points
```csharp
// Module.cs - Plugin lifecycle
internal class Module : ModuleBase
{
    public override async Task InstallAsync(ModuleInstallationContext context)
    {
        await ImportLanguageResourcesAsync();
        await TrySaveSettingsAsync<PluginSettings>();
    }
}

// Startup.cs - Service registration
internal class Startup : StarterBase
{
    public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
    {
        services.AddScoped<IPluginService, PluginService>();
    }
}

// Provider registration via attributes
[SystemName("Plugin.SystemName")]
[FriendlyName("Display Name")]
[Order(1)]
public class PluginProvider : ProviderBase
{
    // Provider implementation
}
```

### Plugin Categories & Examples
- **Payment**: PayPal, Stripe, AmazonPay
- **Shipping**: ByWeight, ByTotal, Fixed Rate
- **Tax**: ByRegion, Fixed Rate
- **Analytics**: Google Analytics
- **Auth**: Facebook, Google, Microsoft
- **Developer**: DevTools, WebAPI

## Documentation
- Official docs: https://smartstore.atlassian.net/wiki/spaces/SMNET60/pages/2511044691/Getting+Started
- Community: http://community.smartstore.com