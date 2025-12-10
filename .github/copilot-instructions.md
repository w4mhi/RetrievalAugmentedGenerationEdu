# GitHub Copilot Instructions: Service Registration Project

## Project Overview

This repository implements a REST API-based service registration system with heartbeat monitoring for the platform's service catalog. Services automatically register via API endpoints, undergo administrator approval, and maintain health status through periodic heartbeat signals.

## Technology Stack

### Core Technologies
- **Language**: C# 10.0+
- **Framework**: .NET 8.0
- **Web Framework**: ASP.NET Core 8.0 (Web API)
- **Dashboard**: Blazor Server with SignalR
- **Storage**: Pluggable backend abstraction
  - **PostgreSQL 15+**: Entity Framework Core 8.0, EF Core migrations
  - **Redis**: StackExchange.Redis with RedisJSON module (redis-stack-server), RDB persistence
  - **SQL Server 2019+**: Entity Framework Core 8.0 (alternative)
  - **File**: JSON file storage (testing only)
- **Logging**: ILogger (Microsoft.Extensions.Logging)
- **Testing**: xUnit, Moq, FluentAssertions, bUnit (Blazor components)
- **Containerization**: Docker with multi-stage builds
- **Orchestration**: Kubernetes with HPA, StatefulSets for databases

### Key Dependencies
- ASP.NET Core 8.0 (API hosting, dependency injection)
- Blazor Server (dashboard UI with real-time updates)
- SignalR (real-time push notifications to dashboard)
- Entity Framework Core 8.0 (PostgreSQL/SQL Server providers)
- StackExchange.Redis (Redis client, RedisJSON support)
- Microsoft.AspNetCore.RateLimiting (API rate limiting)
- System.Security.Cryptography (SHA256 for service name normalization)
- bUnit (Blazor component unit testing)

## Architecture Patterns

### Clean Architecture with Pluggable Storage
- **Models Layer**: Domain entities, enums (no dependencies)
- **Interfaces Layer**: Contracts, IStorageProvider, IServiceRepository interfaces (depends on Models only)
- **Services Layer**: Business logic, validation (depends on Interfaces, Models)
- **Data Layer**: Multiple implementations
  - **Omni.ServiceRegistry.Data**: IStorageProvider abstraction, StorageProviderFactory
  - **Omni.ServiceRegistry.Data.Postgres**: EF Core DbContext, PostgresStorageProvider, repositories
  - **Omni.ServiceRegistry.Data.Redis**: RedisStorageProvider, RedisJSON-based repositories
  - **Omni.ServiceRegistry.Data.File**: FileStorageProvider (testing)
- **API Layer**: Controllers, DTOs, middleware (depends on Services, Interfaces, Models)
- **Dashboard Layer**: Blazor pages, components (depends on Services, Interfaces, Models)

### Design Patterns
- **Repository Pattern**: Two-layer abstraction (IStorageProvider + IServiceRepository)
- **Strategy Pattern**: StorageProviderFactory selects backend based on configuration
- **Dependency Injection**: All services injected via constructor
- **Background Services**: IHostedService for heartbeat timeout monitoring
- **State Machine**: Health status transitions in domain entities
- **Soft Delete**: Status enum pattern with provider-specific filtering
- **Optimized Writes**: Write to database only on status change or periodic snapshot

## Code Generation Guidelines

### General Rules
1. **Strong Typing**: Use explicit types, NO `var` keyword
2. **SOLID Principles**: Single Responsibility, Interface Segregation, Dependency Inversion
3. **One Class Per File**: Each class in its own file matching the class name
4. **Naming Conventions**:
   - PascalCase: Classes, methods, properties, enums
   - camelCase: Variables, parameters, private fields
   - NO underscore prefix for variables (`_fieldName` is prohibited)
5. **Line Length**: Maximum 120 characters per line
6. **Method Length**: Maximum 30 lines per method (refactor longer methods)
7. **Async/Await**: Use async/await for all I/O operations (database, HTTP)

### Dependency Injection Rules
1. **ILogger Parameter Position**: ALWAYS last parameter in constructor and method signatures
2. **ILogger Nullability**:
   - Libraries: `ILogger<T>?` (nullable)
   - Applications: `ILogger<T>` (non-nullable)
3. **IConfiguration**: Inject via constructor ONLY, not method parameters
4. **IConfiguration vs IOptions**: Do NOT mix in same class; choose one pattern
5. **IConfiguration/IOptions Position**: First parameters in constructor signature

### Namespace Convention
Follow pattern: `Omni.ServiceRegistry.{Layer}`
- `Omni.ServiceRegistry.Models`
- `Omni.ServiceRegistry.Interfaces`
- `Omni.ServiceRegistry.Services`
- `Omni.ServiceRegistry.Data` (abstraction)
- `Omni.ServiceRegistry.Data.Postgres`
- `Omni.ServiceRegistry.Data.Redis`
- `Omni.ServiceRegistry.Data.File`
- `Omni.ServiceRegistry.Api`
- `Omni.ServiceRegistry.Dashboard`

### Folder Structure
```
src/
├── Omni.ServiceRegistry.Models/        # Domain entities, enums
├── Omni.ServiceRegistry.Interfaces/    # Contracts (depends on Models only)
├── Omni.ServiceRegistry.Services/      # Business logic (depends on Interfaces, Models)
├── Omni.ServiceRegistry.Data/          # IStorageProvider abstraction, factory
├── Omni.ServiceRegistry.Data.Postgres/ # EF Core, PostgresStorageProvider, repositories
├── Omni.ServiceRegistry.Data.Redis/    # RedisStorageProvider, RedisJSON repositories
├── Omni.ServiceRegistry.Data.File/     # FileStorageProvider (testing)
├── Omni.ServiceRegistry.Api/           # Controllers, DTOs (depends on all layers)
└── Omni.ServiceRegistry.Dashboard/     # Blazor Server UI (depends on Services, Models)

deployment/
├── docker/                             # Dockerfile, docker-compose.yml (profiles: postgres, redis)
└── kubernetes/                         # K8s manifests (postgres.yaml, redis.yaml, etc.)
├── Omni.ServiceRegistry.Interfaces/    # Contracts (depends on Models only)
├── Omni.ServiceRegistry.Services/      # Business logic (depends on Interfaces, Models)
├── Omni.ServiceRegistry.Data/          # EF Core, repositories (depends on Interfaces, Models)
├── Omni.ServiceRegistry.Api/           # Controllers, DTOs (depends on all layers)
└── Omni.ServiceRegistry.Dashboard/     # Blazor Server UI (depends on Services, Models)

deployment/
├── docker/                             # Dockerfile, docker-compose.yml
└── kubernetes/                         # K8s manifests (deployment, service, configmap, etc.)

tests/
├── Omni.ServiceRegistry.Services.Tests/
├── Omni.ServiceRegistry.Api.Tests/
├── Omni.ServiceRegistry.Dashboard.Tests/  # bUnit component tests
└── Omni.ServiceRegistry.Tests.Common/
```

## Domain Model

### Key Entities
1. **RegistrationRequest**: Pending service registration submissions
2. **Service**: Approved, active services in catalog
3. **ServiceChangeHistory**: Audit log of service modifications

### Enumerations
- **RegistrationStatus**: Pending, Approved, Denied
- **HealthStatus**: Healthy, Unhealthy, Degraded, Dead, Recovered, Deleted
- **DeletionStatus**: Active, PendingDeletion, Deleted

### State Transitions
**Health Status Flow**:
```
HEALTHY → UNHEALTHY (missed heartbeat) 
UNHEALTHY → DEGRADED (50% max missed) 
DEGRADED → DEAD (exceeded max) 
DEAD → RECOVERED (heartbeat received)
RECOVERED → HEALTHY (consecutive successful heartbeats)
DEGRADED → HEALTHY (heartbeat received)
```

## API Endpoints

### Registration
- `POST /api/v1/register` - Submit registration
- `GET /api/v1/status/registration/{id}` - Check registration status

### Service Status
- `GET /api/v1/status/service/{id}` - Get service details and health

### Heartbeat
- `POST /api/v1/heartbeat/{serviceId}` - Submit heartbeat signal

### Administrator
- `GET /api/v1/admin/registrations/pending` - List pending registrations
- `POST /api/v1/admin/registrations/{id}/approve` - Approve registration
- `POST /api/v1/admin/registrations/{id}/deny` - Deny registration (comments required)
- `POST /api/v1/delete/{id}` - Request service deletion

## Implementation Patterns

### Storage Provider Abstraction
```csharp
// Two-layer abstraction: IStorageProvider (low-level) + IServiceRepository (domain-level)

// Low-level storage interface
public interface IStorageProvider
{
    Task<T?> ReadAsync<T>(string key) where T : class;
    Task WriteAsync<T>(string key, T entity) where T : class;
    Task<bool> DeleteAsync(string key);
    Task<IEnumerable<T>> QueryAsync<T>(Func<T, bool> predicate) where T : class;
}

// Domain-level repository interface
public interface IServiceRepository
{
    Task<Service?> GetByIdAsync(Guid serviceId);
    Task<List<Service>> GetByHealthStatusAsync(HealthStatus status);
    Task<List<Service>> GetByOwnerAsync(string ownerEmail);
    Task<Service> AddAsync(Service service);
    Task UpdateAsync(Service service);
    Task<bool> ExistsByNormalizedNameAsync(string normalizedName);
}

// Factory pattern for provider selection
public class StorageProviderFactory
{
    public IStorageProvider Create(string providerName, string connectionString)
    {
        return providerName switch
        {
            "Postgres" => new PostgresStorageProvider(connectionString),
            "Redis" => new RedisStorageProvider(connectionString),
            "File" => new FileStorageProvider(connectionString),
            _ => throw new InvalidOperationException($"Unknown provider: {providerName}")
        };
    }
}

// Configuration in appsettings.json
{
  "DatabaseProvider": "Postgres",  // or "Redis", "SqlServer", "File"
  "ConnectionStrings": {
    "ServiceRegistry": "Host=localhost;Database=serviceregistry;..."
  }
}
```

### Optimized Heartbeat Processing
```csharp
// Write to database ONLY on status change or periodic snapshot
// Reduces DB writes by 95%+ (from every heartbeat to status changes + periodic snapshots)

private readonly ConcurrentDictionary<Guid, HeartbeatState> heartbeatStates = new();

public async Task ProcessHeartbeatAsync(Guid serviceId, HeartbeatRequest request)
{
    HeartbeatState state = heartbeatStates.GetOrAdd(serviceId, _ => new HeartbeatState());
    
    state.LastHeartbeatTimestamp = DateTime.UtcNow;
    state.HeartbeatsSinceLastWrite++;
    
    HealthStatus previousStatus = state.CurrentHealthStatus;
    HealthStatus newStatus = CalculateHealthStatus(state);
    
    bool shouldWrite = false;
    
    // Condition 1: Status changed
    if (newStatus != previousStatus)
    {
        shouldWrite = true;
    }
    
    // Condition 2: Periodic snapshot (every MaxMissedHeartbeats heartbeats)
    Service service = await _repository.GetByIdAsync(serviceId);
    if (state.HeartbeatsSinceLastWrite >= service.MaxMissedHeartbeats)
    {
        shouldWrite = true;
        state.HeartbeatsSinceLastWrite = 0;
    }
    
    if (shouldWrite)
    {
        service.HealthStatus = newStatus;
        service.LastHeartbeatTimestamp = state.LastHeartbeatTimestamp;
        service.HeartbeatCount += state.HeartbeatsSinceLastWrite;
        await _repository.UpdateAsync(service);
    }
}
```

### Service Name Normalization
```csharp
// Use SHA256 hash of lowercase name for uniqueness validation
public string NormalizeServiceName(string serviceName)
{
    string lowercaseName = serviceName.ToLowerInvariant();
    using SHA256 sha256 = SHA256.Create();
    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(lowercaseName));
    return Convert.ToHexString(hashBytes);
}
```

### Idempotent Registration
- Check for existing pending registration by normalized name hash
- Return existing registration ID if found
- Database unique constraint on `ServiceNameNormalized` column

### Heartbeat Processing
- Accept heartbeat → Add to in-memory queue → Return ACK immediately (<500ms)
- Background service processes queue in batches (every 5 seconds)
- Update last heartbeat timestamp, reset missed counter, transition health status

### Background Monitoring
```csharp
public class HeartbeatMonitorService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Check for services with expired heartbeat timeouts
            // Update health status: HEALTHY → UNHEALTHY → DEGRADED → DEAD
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```

### Soft Delete with Query Filters
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Global query filter excludes DELETED services
    modelBuilder.Entity<Service>()
        .HasQueryFilter(s => s.DeletionStatus != DeletionStatus.Deleted);
    
    // Unique constraint only for non-deleted services
    modelBuilder.Entity<Service>()
        .HasIndex(s => s.ServiceNameNormalized)
        .IsUnique()
        .HasFilter("[DeletionStatus] != 2");
}
```

### Optimistic Concurrency
```csharp
public class Service
{
    [Timestamp]
    public byte[] RowVersion { get; set; }
}

// Handle concurrency exceptions
catch (DbUpdateConcurrencyException)
{
    // Reload entity, re-apply changes, retry save
}
```

## Validation Rules (Requirement R6)

### Service Name
- Pattern: `^[a-zA-Z0-9-]{3,50}$`
- Length: 3-50 characters
- Alphanumeric with hyphens only

### Contact Email
- Valid email format
- Pattern: `^[^@]+@[^@]+\.[^@]+$`

### Endpoints
- JSON array of 1-10 URLs
- Each must have http/https protocol
- Valid URL format

### Heartbeat Configuration
- HeartbeatTimeout: Positive integer, range 15-300 seconds (recommended)
- MaxMissedHeartbeats: Positive integer, range 3-20 (recommended)

## Logging Guidelines

### ILogger Levels (Requirement R47-R51)
- **Trace**: Detailed diagnostic info (e.g., method entry/exit, parameter values)
- **Info**: General operation events (registration submitted, approved, heartbeat received)
- **Warning**: Potentially harmful situations (missed heartbeat, degraded status)
- **Error**: Errors that don't prevent operation (validation failures, DB exceptions)

### Log Events to Capture
- All registration requests (R48)
- All approval/denial actions (R49)
- Health status transitions (R50)
- Failed heartbeat attempts and missed heartbeat events (R51)

### Structured Logging Example
```csharp
_logger.LogInformation(
    "Service {ServiceId} health status changed from {PreviousStatus} to {NewStatus}",
    serviceId, previousStatus, newStatus);
```

## Performance Targets

- Registration API: <2s response time
- Status query API: <1s response time
- Heartbeat API: <500ms response time (ACK immediately, process async)
- Heartbeat throughput: ≥10,000 per minute
- Support ≥1,000 registered services
- Support ≥50 concurrent users

## Testing Requirements

### Unit Tests
- Service layer business logic
- Validation logic
- Service name normalization
- State machine transitions

### Integration Tests
- API endpoints (registration, status, heartbeat, admin)
- Database transactions (EF Core, concurrency)
- Background service behavior

### Test Data Builders
Use builder pattern for test entity creation in `Omni.ServiceRegistry.Tests.Common/Builders/`

## Dashboard Guidelines

### Blazor Server Architecture
- Use Blazor Server for real-time dashboard with SignalR integration
- Server-side rendering for reduced client payload
- SignalR hub for pushing health status updates to connected clients
- Update dashboard within 10 seconds of health status changes

### Component Structure
```csharp
// Blazor component with SignalR subscription
@page "/monitoring"
@inject IHubConnection HubConnection
@inject IServiceCatalogService CatalogService

<ServiceStatusCard Services="@services" />

@code {
    private List<ServiceViewModel> services;
    
    protected override async Task OnInitializedAsync()
    {
        services = await CatalogService.GetAllServicesAsync();
        
        // Subscribe to real-time updates
        HubConnection.On<Guid, string>("HealthStatusChanged", async (serviceId, newStatus) =>
        {
            ServiceViewModel service = services.FirstOrDefault(s => s.ServiceId == serviceId);
            if (service != null)
            {
                service.HealthStatus = newStatus;
                await InvokeAsync(StateHasChanged);
            }
        });
        
        await HubConnection.StartAsync();
    }
}
```

### Dashboard Pages
1. **Monitoring Dashboard** (`Index.razor`):
   - Service count by health status
   - Filterable service table
   - Visual health indicators (green/yellow/red)
   
2. **Pending Approvals** (`PendingApprovals.razor`):
   - Registration requests table
   - Deletion requests table
   - Approve/deny modals with comments
   
3. **Service Detail** (`ServiceDetail.razor`):
   - Full service metadata
   - Heartbeat history
   - Change history

### bUnit Testing
```csharp
[Fact]
public void ServiceStatusCard_HealthyService_DisplaysGreenIndicator()
{
    // Arrange
    using TestContext ctx = new TestContext();
    ServiceViewModel service = new() { HealthStatus = "HEALTHY" };
    
    // Act
    IRenderedComponent<ServiceStatusCard> component = ctx.RenderComponent<ServiceStatusCard>(
        parameters => parameters.Add(p => p.Service, service));
    
    // Assert
    component.Find(".health-indicator").ClassList.Should().Contain("green");
}
```

## Deployment Guidelines

### Docker Multi-Stage Build
```dockerfile
# Build stage with SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage - optimized image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
RUN adduser --disabled-password appuser && chown -R appuser /app
USER appuser
COPY --from=build /app/publish .
HEALTHCHECK CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Omni.ServiceRegistry.Api.dll"]
```

### Kubernetes Deployment
- **Horizontal Pod Autoscaler**: CPU-based, min 2 replicas, max 10 replicas, target 70% CPU
- **Database Migrations**: Kubernetes Job runs before deployment
- **ConfigMap**: Non-sensitive configuration (heartbeat intervals, timeouts)
- **Secret**: Database connection strings, authentication keys
- **Persistent Volume**: Database storage with appropriate storage class

### EF Core Migrations
```bash
# Create migration
dotnet ef migrations add InitialSchema -p src/Omni.ServiceRegistry.Data

# Apply migrations in Kubernetes Job
dotnet ef database update --connection "$ConnectionStrings__ServiceRegistry"
```

## Security Notes

**Authentication/Authorization**: Deferred to Priority 2 (P2)
- Current implementation assumes existing auth infrastructure
- Endpoints marked with `[Authorize]` attribute (placeholder)
- API contracts include BearerAuth security scheme
- Implement role-based access control: Admin vs ServiceOwner

## Rate Limiting

### Heartbeat Endpoint
- Policy: Fixed window
- Limit: 200 requests per second
- Queue: 50 requests

### Registration Endpoint
- Policy: Sliding window
- Limit: 10 requests per minute
- Segments: 6 per window

## Database Schema Notes

### Indexes
- `RegistrationRequest.ServiceNameNormalized` (unique)
- `RegistrationRequest.Status` (for pending queue)
- `Service.ServiceNameNormalized` (unique where not deleted)
- `Service.HealthStatus` (for catalog filtering)
- `Service.LastHeartbeatTimestamp` (for timeout detection)
- `ServiceChangeHistory.ServiceId` (for audit queries)

### Constraints
- `HeartbeatTimeout > 0`
- `MaxMissedHeartbeats > 0`
- `MissedHeartbeatCounter >= 0`

## Common Scenarios

### Scenario 1: Service Registration Flow
1. Service POSTs to `/api/v1/register` with metadata
2. System validates, assigns registration ID, returns "pending"
3. Administrator polls `/api/v1/admin/registrations/pending`
4. Administrator approves via `/api/v1/admin/registrations/{id}/approve`
5. System creates Service entity with new service ID
6. Service appears in catalog

### Scenario 2: Heartbeat Monitoring
1. Approved service sends POST to `/api/v1/heartbeat/{serviceId}`
2. API adds to queue, returns ACK
3. Background service processes batch every 5 seconds
4. Updates last heartbeat timestamp, resets missed counter
5. Maintains HEALTHY status as long as heartbeats arrive within timeout

### Scenario 3: Health Degradation
1. Service misses heartbeat (timeout expires)
2. Background service increments missed counter
3. Status transitions: HEALTHY → UNHEALTHY (1 missed) → DEGRADED (50% max) → DEAD (>max)
4. Logs warning/error for each transition

### Scenario 4: Service Deletion
1. Owner/admin POSTs to `/api/v1/delete/{id}`
2. System marks deletion status as PendingDeletion
3. Administrator approves deletion
4. System updates DeletionStatus to Deleted, excludes from queries
5. Re-registration requires new service ID

## References

- **Specification**: `specs/1-register-service/spec.md`
- **Implementation Plan**: `specs/1-register-service/plan.md`
- **Data Model**: `specs/1-register-service/data-model.md`
- **API Contracts**: `specs/1-register-service/contracts/*.yaml`
- **Research Decisions**: `specs/1-register-service/research.md`
- **Quickstart Guide**: `specs/1-register-service/quickstart.md`

---

**Important**: When generating code, ALWAYS follow the constitution rules and patterns documented above. Prioritize strong typing, SOLID principles, proper dependency injection order, and comprehensive logging.
