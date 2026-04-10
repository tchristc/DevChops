# DevChops – Implementation Plan

> **Constraint:** No database for now. All user preferences and saved filters use in-memory storage.

---

## Solution Structure

```
DevChops/
├── DevChops.Domain/          # Entities, interfaces, value objects (class library)
├── DevChops.Application/     # Services, DTOs, use cases (class library)
├── DevChops.Infrastructure/  # Azure SDK adapters, in-memory repos (class library)
└── DevChops.BlazorApp/       # Blazor Server UI — already exists
```

> Phase 1 creates the three new class library projects. Until the database is added, `DevChops.Infrastructure` contains only Azure SDK adapters and in-memory repository implementations.

---

## NuGet Packages

### `DevChops.BlazorApp`
| Package | Purpose |
|---|---|
| `MudBlazor` | UI component library |
| `Blazor-ApexCharts` | Time-series charts for the metrics dashboard |
| `Microsoft.Identity.Web` | Azure Entra ID auth, MSAL token cache |
| `Microsoft.Identity.Web.UI` | Login/logout Razor UI helpers |

### `DevChops.Infrastructure`
| Package | Purpose |
|---|---|
| `Azure.ResourceManager` | Enumerate subscriptions and resource groups |
| `Azure.ResourceManager.AppService` | Enumerate App Service Plans |
| `Azure.Monitor.Query` | Log Analytics queries + metrics queries |
| `Azure.Identity` | `DefaultAzureCredential` / `OnBehalfOfCredential` |

### `DevChops.Application` / `DevChops.Domain`
No third-party packages — pure C#.

---

## Phase 1 — Solution Structure

**Goal:** Add the three class library projects and wire up references.

**Tasks:**
1. Add `DevChops.Domain` (.NET 10 class library)
2. Add `DevChops.Application` (.NET 10 class library) → references `DevChops.Domain`
3. Add `DevChops.Infrastructure` (.NET 10 class library) → references `DevChops.Domain` + `DevChops.Application`
4. Add project references to `DevChops.BlazorApp` → references all three

**Files to create in `DevChops.Domain`:**
```
Entities/
  ResourceGroup.cs
  AppServicePlan.cs
  LogEntry.cs
  MetricDataPoint.cs
  MetricSeries.cs
  UserPreference.cs
  SavedFilter.cs
ValueObjects/
  TimeRange.cs          # Start, End, and a preset label
  MetricDefinition.cs   # Name, unit, supported aggregations
Interfaces/
  ILogRepository.cs
  IMetricsRepository.cs
  IAzureResourceRepository.cs
  IUserPreferenceService.cs
  ISavedFilterService.cs
```

**Files to create in `DevChops.Application`:**
```
DTOs/
  CorrelatedOperationDto.cs
  MetricSeriesDto.cs
  AppServicePlanSummaryDto.cs
  LogFilterDto.cs
Services/
  LogQueryService.cs
  MetricsQueryService.cs
  ResourceGroupService.cs
```

**Files to create in `DevChops.Infrastructure`:**
```
Azure/
  AzureLogRepository.cs          # LogsQueryClient → KQL
  AzureMetricsRepository.cs      # MetricsQueryClient
  AzureResourceRepository.cs     # ArmClient
InMemory/
  InMemoryUserPreferenceService.cs   # ConcurrentDictionary, keyed by user OID
  InMemorySavedFilterService.cs
```

---

## Phase 2 — MudBlazor App Shell

**Goal:** Replace the default layout with a full MudBlazor shell.

**Tasks:**
1. Install `MudBlazor` NuGet package
2. Add MudBlazor services in `Program.cs` (`builder.Services.AddMudServices()`)
3. Add MudBlazor CSS/JS to `App.razor` (`<MudThemeProvider>`, `<MudPopoverProvider>`, `<MudDialogProvider>`, `<MudSnackbarProvider>`)
4. Rewrite `MainLayout.razor` with:
   - `MudLayout` outer wrapper
   - `MudAppBar` — app title, user avatar/login button, right-side icons
   - `MudDrawer` (persistent, left) — `MudNavMenu` with links to Dashboard, Logs, Metrics, Settings
   - `MudMainContent` — `@Body`
5. Install `Blazor-ApexCharts` and add its script tag to `App.razor`
6. Update `Home.razor` to a proper Dashboard page with `MudGrid` summary cards (stubs):
   - Recent Exceptions (count)
   - Active App Service Plans (count)
   - Last Log Query time
   - Quick-link buttons to Logs and Metrics pages

**Key MudBlazor components used in this phase:**
`MudLayout`, `MudAppBar`, `MudDrawer`, `MudNavMenu`, `MudNavLink`, `MudGrid`, `MudCard`, `MudText`, `MudIconButton`

---

## Phase 3 — Azure Entra ID Authentication

**Goal:** Users must sign in with their Azure Entra ID account. The resulting token is used for all Azure SDK calls.

**Tasks:**
1. Register the app in Azure Entra ID (App Registration):
   - Redirect URI: `https://localhost:{port}/signin-oidc`
   - API permissions: `user_impersonation` on Azure Service Management (`https://management.azure.com/`)
   - Grant `Azure Monitor Reader` and `Reader` RBAC at the subscription scope for testing
2. Install `Microsoft.Identity.Web` + `Microsoft.Identity.Web.UI`
3. Configure `appsettings.json` (secrets go in `dotnet user-secrets`, never in source):
   ```json
   "AzureAd": {
     "Instance": "https://login.microsoftonline.com/",
     "TenantId": "<tenant-id>",
     "ClientId": "<client-id>",
     "ClientSecret": "SET_VIA_USER_SECRETS",
     "CallbackPath": "/signin-oidc"
   }
   ```
   > The client secret is required because Blazor Server is a **confidential client** — the server must identify itself to Entra ID when exchanging the auth code and when silently refreshing tokens on the user's behalf. A certificate can be used instead of a secret for production.
4. Wire up in `Program.cs`:
   ```csharp
   builder.Services
       .AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd")
       .EnableTokenAcquisitionToCallDownstreamApi(
           ["https://management.azure.com/user_impersonation"])
       .AddInMemoryTokenCaches();
   builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();
   builder.Services.AddAuthorization();
   // Register the credential bridge (see token flow below)
   builder.Services.AddScoped<IAzureCredentialProvider, MicrosoftIdentityCredentialProvider>();
   ```
   Also add `app.UseAuthentication()`, `app.UseAuthorization()`, and `app.MapControllers()` to the middleware pipeline.
5. Add `@attribute [Authorize]` to each protected page (`Home`, `Logs`, `Metrics`, `Settings`)
6. Wrap `Routes.razor` with `<CascadingAuthenticationState>` and use `<AuthorizeRouteView>` with a `<NotAuthorized>` slot that renders `<RedirectToLogin />` (navigates to `/MicrosoftIdentity/Account/SignIn`)
7. Show the signed-in user's name and a **Sign Out** button in `MudAppBar` using `<AuthorizeView>` → `context.User.Identity?.Name`

**Token flow for Azure SDK calls:**
- `MicrosoftIdentityCredentialProvider` (in `DevChops.BlazorApp/Auth/`) implements `IAzureCredentialProvider` and wraps `ITokenAcquisition` in a custom `TokenCredential`
- When `GetToken` / `GetTokenAsync` is called by the Azure SDK, it calls `ITokenAcquisition.GetAccessTokenForUserAsync(["https://management.azure.com/user_impersonation"])`
- Infrastructure repositories (`AzureResourceRepository`, `AzureLogRepository`, `AzureMetricsRepository`) receive `IAzureCredentialProvider` via DI, call `GetCredential()`, and pass the resulting `TokenCredential` to `ArmClient` / `LogsQueryClient` / `MetricsQueryClient`
- Because `ITokenAcquisition` is scoped to the Blazor Server circuit (one per signed-in user), every repository call automatically uses that user's token

---

## Phase 4 — Domain & Application Layer

**Goal:** Implement all entities, interfaces, DTOs, and service logic.

### Domain Entities (key shapes)

```csharp
// TimeRange.cs
record TimeRange(DateTimeOffset Start, DateTimeOffset End, string? PresetLabel = null)
{
    public static TimeRange Last1Hour   => new(DateTimeOffset.UtcNow.AddHours(-1),  DateTimeOffset.UtcNow, "1h");
    public static TimeRange Last6Hours  => new(DateTimeOffset.UtcNow.AddHours(-6),  DateTimeOffset.UtcNow, "6h");
    public static TimeRange Last24Hours => new(DateTimeOffset.UtcNow.AddHours(-24), DateTimeOffset.UtcNow, "24h");
    public static TimeRange Last7Days   => new(DateTimeOffset.UtcNow.AddDays(-7),   DateTimeOffset.UtcNow, "7d");
    public static TimeRange Last30Days  => new(DateTimeOffset.UtcNow.AddDays(-30),  DateTimeOffset.UtcNow, "30d");
}

// LogEntry.cs
record LogEntry(
    string OperationId, string OperationName, string Type,  // "request" | "exception" | "trace" | "dependency"
    DateTimeOffset Timestamp, string Severity,
    int? ResultCode, TimeSpan? Duration, string? Message, string? Details);

// MetricDataPoint.cs
record MetricDataPoint(DateTimeOffset Timestamp, double? Average, double? Maximum, double? Minimum, double? Total);
```

### Application Service Interfaces

```csharp
// ILogRepository.cs
Task<IReadOnlyList<LogEntry>> GetLogsAsync(string workspaceId, LogFilterDto filter, CancellationToken ct);

// IMetricsRepository.cs
Task<MetricSeries> GetMetricAsync(string resourceId, string metricName, TimeRange range, CancellationToken ct);

// IAzureResourceRepository.cs
Task<IReadOnlyList<ResourceGroup>> GetResourceGroupsAsync(CancellationToken ct);
Task<IReadOnlyList<AppServicePlan>> GetAppServicePlansAsync(string subscriptionId, CancellationToken ct);
Task<IReadOnlyList<string>> GetLogAnalyticsWorkspaceIdsAsync(string resourceGroupId, CancellationToken ct);
```

---

## Phase 5 — Infrastructure: Azure Resource Repository

**Goal:** Implement `AzureResourceRepository` using `Azure.ResourceManager`.

**Tasks:**
1. Add `Azure.ResourceManager` and `Azure.ResourceManager.AppService` to `DevChops.Infrastructure`
2. Implement `AzureResourceRepository`:
   - Inject `IAzureCredentialProvider` and call `GetCredential()` to obtain the user's `TokenCredential`
   - `GetResourceGroupsAsync` → iterate `armClient.GetSubscriptions()`, then `sub.GetResourceGroups()`
   - `GetAppServicePlansAsync` → `sub.GetAppServicePlans()`
   - `GetLogAnalyticsWorkspaceIdsAsync` → filter resources by type `Microsoft.OperationalInsights/workspaces`
3. Register in DI in `Program.cs`
4. Add a `ResourceGroupService` in Application layer that caches the list for the session

---

## Phase 6 — Infrastructure: Log Repository (KQL)

**Goal:** Implement `AzureLogRepository` to fetch correlated telemetry via KQL.

**Tasks:**
1. Add `Azure.Monitor.Query` to `DevChops.Infrastructure`
2. Implement `AzureLogRepository`:
   - Use `LogsQueryClient` with the user's `TokenCredential`
   - Build a KQL query that unions `requests`, `exceptions`, `traces`, `dependencies` filtered by time range and joins on `operation_Id`
3. Parse `LogsQueryResult` rows into `LogEntry` domain objects
4. Handle partial failures (some tables may not exist in all workspaces)

**Core KQL template:**
```kql
let opIds = requests
| where timestamp between (datetime({start}) .. datetime({end}))
| where {filters}
| project operation_Id;
union requests, exceptions, traces, dependencies
| where timestamp between (datetime({start}) .. datetime({end}))
| where operation_Id in (opIds)
| order by timestamp asc
```

---

## Phase 7 — Log Viewer Page (`/logs`)

**Goal:** Full correlated log viewer with filters, results table, and detail drawer.

**Tasks:**
1. Create `Components/Pages/Logs.razor` (`@page "/logs"`, `@attribute [Authorize]`)
2. Filter bar (`MudPaper` top section):
   - Subscription + Resource Group dropdowns (populated from `ResourceGroupService`)
   - Time range preset chips (`MudChipSet`) + custom date picker (`MudDateRangePicker`)
   - Severity filter chips (Verbose, Info, Warning, Error, Critical)
   - Free-text search (`MudTextField`) + Search button
3. Results area:
   - `MudTable` with columns: Timestamp, Operation Name, Type icon, Duration, Status, Severity
   - Group rows by `OperationId` — expand/collapse per operation
   - Clicking a row opens a `MudDrawer` (right side) with full detail:
     - Timeline of all correlated events for that `operation_Id`
     - Raw properties in a `MudExpansionPanel`
4. Empty state and loading skeleton (`MudSkeleton`)
5. Save current filter as a named preset → calls `ISavedFilterService`

---

## Phase 8 — Metrics Dashboard Page (`/metrics`)

**Goal:** App Service Plan metrics with charts.

**Tasks:**
1. Create `Components/Pages/Metrics.razor` (`@page "/metrics"`, `@attribute [Authorize]`)
2. Top controls row:
   - App Service Plan multi-select (`MudSelect` with checkboxes)
   - Metric selector (`MudSelect`) populated from `MetricDefinition` list
   - Time range preset buttons (`MudButtonGroup`): 15m, 1h, 6h, 24h, 7d, 30d + custom
   - Chart type toggle (Line / Area / Bar) using `MudToggleIconButton`
3. Chart area:
   - One `ApexChart` per selected metric
   - Each selected App Service Plan is a separate `ApexChartSeries` (comparison mode)
   - X-axis: time; Y-axis: metric value with unit label
4. Metrics to support (from spec): CPU%, Memory%, HTTP Queue Length, Disk Queue Length, RPS, Response Time
5. Loading state per chart (`MudProgressLinear` overlay)

**`MetricDefinition` catalog (static, in Domain):**
```csharp
static readonly IReadOnlyList<MetricDefinition> Catalog = [
    new("CpuPercentage",      "CPU %",            "Percent",   ["Average", "Maximum"]),
    new("MemoryPercentage",   "Memory %",         "Percent",   ["Average", "Maximum"]),
    new("HttpQueueLength",    "HTTP Queue",        "Count",     ["Average", "Maximum"]),
    new("DiskQueueLength",    "Disk Queue",        "Count",     ["Average", "Maximum"]),
    new("Requests",           "Requests/sec",      "Count",     ["Total"]),
    new("AverageResponseTime","Response Time (avg)","Seconds",  ["Average"]),
];
```

---

## Phase 9 — Settings Page & In-Memory Preferences

**Goal:** Persist user preferences for the session; no database yet.

**Tasks:**
1. Create `Components/Pages/Settings.razor` (`@page "/settings"`, `@attribute [Authorize]`)
2. `InMemoryUserPreferenceService`:
   - `ConcurrentDictionary<string, UserPreference>` keyed by Azure OID (from `ClaimsPrincipal`)
   - Register as `Singleton`
3. Settings UI:
   - Default subscription/resource group dropdowns
   - Default time range preset selector
   - Saved Filters list → rename, delete
4. `InMemorySavedFilterService`:
   - `ConcurrentDictionary<string, List<SavedFilter>>` keyed by OID
   - Filters created from the Log Viewer page's "Save Filter" action
5. Inject `IUserPreferenceService` into `LogQueryService` and `MetricsQueryService` to pre-populate defaults

---

## Phase 10 — Polish & Error Handling

**Goal:** Production-quality error states and UX details.

**Tasks:**
1. Global `MudSnackbar` error notifications for Azure API failures
2. RBAC-denied resource groups: display a `MudAlert` (warning) instead of crashing
3. `ReconnectModal.razor` (already exists) — verify it works with MudBlazor theming
4. `NotFound.razor` — replace with a MudBlazor-styled 404 page
5. `Error.razor` — replace with a styled error page
6. Add loading skeletons everywhere data is async
7. `appsettings.Development.json` — document all required keys
8. `README.md` — setup instructions (App Registration steps, user-secrets config)

---

## Build Order

```
Phase 1  → Solution structure + project references
Phase 2  → MudBlazor shell (no auth, stub pages)
Phase 3  → Authentication (all pages behind login)
Phase 4  → Domain entities + interfaces
Phase 5  → Infrastructure: ARM (resource groups + plans)
Phase 6  → Infrastructure: Azure Monitor logs (KQL)
Phase 7  → Log Viewer page (end-to-end)
Phase 8  → Infrastructure: Azure Monitor metrics + Metrics page
Phase 9  → Settings + in-memory preferences
Phase 10 → Polish, error handling, README
```

Each phase produces a fully buildable, runnable state.
When the database is introduced later, only `InMemoryUserPreferenceService` and `InMemorySavedFilterService` need to be replaced — all interfaces and Application-layer code remain unchanged.
