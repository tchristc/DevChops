# DevChops Specification

## Overview
DevChops is a web application for helping developers and platform teams monitor their Azure-hosted applications. It surfaces correlated telemetry (logs, requests, exceptions, traces, and metrics) from Azure Monitor and Application Insights, and provides a rich metrics dashboard for App Service Plans — all scoped to the resource groups the currently signed-in Azure Entra ID user has access to.

---

## Core Features

### 1. Correlated Log Viewer
- Authenticate via Azure Entra ID and enumerate all resource groups the user has read access to
- Fetch correlated telemetry from Azure Monitor / Application Insights:
  - **Requests** – HTTP request traces with status codes, duration, and operation IDs
  - **Exceptions** – Exception details with stack traces correlated to requests
  - **Traces** – Custom trace messages (severity, message, timestamp)
  - **Dependencies** – Outbound calls (SQL, HTTP, etc.) correlated by operation ID
  - **Custom Events** – Application-defined events
- Group / correlate all of the above by `operation_id` so users can trace a single request end-to-end
- Filterable by: resource group, time range, severity level, result code, and free-text search
- Paginated results with a detail panel / drawer for a selected operation

### 2. App Service Plan Metrics Dashboard
- Enumerate all App Service Plans the user has access to across their subscriptions
- Display configurable performance metrics:
  - **CPU Percentage** – per instance average/max
  - **Memory Percentage** – per instance average/max
  - **HTTP Queue Length**
  - **Disk Queue Length**
  - **Requests per second**
  - **Response time (P50 / P95 / P99)**
- Built-in time-range presets: Last 15 min, 1 hour, 6 hours, 24 hours, 7 days, 30 days
- Custom time-range picker
- Chart type toggle: line, area, bar
- Multi-plan comparison mode (overlay charts for side-by-side comparison)

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Blazor Server (.NET 10) with MudBlazor UI framework |
| Auth | Azure Entra ID via `Microsoft.Identity.Web` (OIDC auth-code flow + On-Behalf-Of token for Azure APIs) |
| Azure SDK | `Azure.Monitor.Query`, `Azure.ResourceManager`, `Azure.Identity` |
| Storage | In-memory (`ConcurrentDictionary`) — no database in v1 |
| Design | Responsive, clean and modern; MudBlazor component library |

---

## Architecture

The application follows **Clean Architecture** with the following layers:

### Domain (`DevChops.Domain`)
Pure business entities and interfaces — no framework dependencies.

- **Entities:** `ResourceGroup`, `LogEntry`, `MetricSeries`, `AppServicePlan`, `UserPreference`, `SavedFilter`
- **Value Objects:** `TimeRange`, `MetricDefinition`, `OperationCorrelation`
- **Interfaces:** `ILogRepository`, `IMetricsRepository`, `IAzureResourceRepository`, `IUserPreferenceRepository`

### Application (`DevChops.Application`)
Use cases and orchestration logic.

- **Services:** `LogQueryService`, `MetricsQueryService`, `ResourceGroupService`, `AppServicePlanService`
- **DTOs:** `CorrelatedOperationDto`, `MetricSeriesDto`, `AppServicePlanSummaryDto`
- **Commands / Queries:** CQRS-style handlers (e.g., `GetCorrelatedLogsQuery`, `GetMetricSeriesQuery`)

### Infrastructure (`DevChops.Infrastructure`)
Adapters for all external systems.

- **Azure:** `AzureLogRepository` (Azure Monitor Logs), `AzureMetricsRepository` (Azure Monitor Metrics), `AzureResourceRepository` (ARM)
- **In-memory stores:** `InMemoryUserPreferenceService`, `InMemorySavedFilterService` (replaced by a DB layer in a future phase)
- **Authentication bridge:** `MicrosoftIdentityCredentialProvider` wraps `ITokenAcquisition` as an `IAzureCredentialProvider` so all Azure SDK clients use the signed-in user's delegated token

### Presentation (`DevChops.BlazorApp`)
Blazor Server app — the only project currently in the solution.

- Razor components, pages, and layouts
- MudBlazor for UI components
- Calls Application layer services via DI

---

## Pages & Routes

| Route | Page | Description |
|---|---|---|
| `/` | Dashboard | Summary cards: recent errors, active plans, quick links |
| `/logs` | Log Viewer | Correlated telemetry explorer |
| `/metrics` | Metrics Dashboard | App Service Plan charts and KPIs |
| `/settings` | Settings | User preferences, saved filters, default resource group |
| `/not-found` | Not Found | 404 page |

---

## Authentication & Authorization

1. User visits the app and is redirected to **Azure Entra ID** for browser sign-in (OIDC auth-code flow)
2. On successful sign-in the app (a confidential client registered in Entra ID) exchanges the auth code for tokens via `Microsoft.Identity.Web`
3. When an Azure SDK call is needed, `ITokenAcquisition.GetAccessTokenForUserAsync` acquires a cached or refreshed access token scoped to `https://management.azure.com/user_impersonation` on behalf of the signed-in user
4. A `MicrosoftIdentityCredentialProvider` wraps `ITokenAcquisition` in a custom `TokenCredential` and is injected into all Infrastructure repositories via `IAzureCredentialProvider`
5. All ARM and Azure Monitor calls execute as the signed-in user — resource group and subscription enumeration is automatically scoped to the user's RBAC assignments, with no separate permission mapping needed

---

## Data Models (in-memory, v1)

All user state is held in `ConcurrentDictionary` singletons keyed by the user's Azure OID claim. No database is used in v1; these stores are replaced by a persistent layer (e.g. EF Core + PostgreSQL) when the database phase is introduced.

### `UserPreference`
Default subscription, default resource group, default time-range preset.

### `SavedFilter`
Named log-filter presets created from the Log Viewer page.


## Azure Integration Details

### Log Queries (Azure Monitor / Application Insights)
- Uses **Azure Monitor Query** SDK (`Azure.Monitor.Query`)
- Queries are KQL (Kusto Query Language) sent to a Log Analytics Workspace
- Correlation is done by joining on `operation_Id` across `requests`, `exceptions`, `traces`, `dependencies` tables

### Metrics Queries (Azure Monitor Metrics)
- Uses **Azure Monitor Query** SDK (`MetricsQueryClient`)
- Metrics are fetched per App Service Plan resource ID
- Aggregations: Average, Maximum, Minimum, Total
- Granularity: 1 min, 5 min, 15 min, 1 hour, 1 day (auto-selected based on time range)

### Resource Enumeration (ARM)
- Uses **Azure Resource Manager** SDK (`Azure.ResourceManager`)
- `ArmClient` → `GetSubscriptions()` → `GetResourceGroups()` → filter by type
- App Service Plans discovered via `GetAppServicePlans()` on each subscription

---

## Non-Functional Requirements

- **Performance:** Log queries should return within 5 seconds for a 1-hour window; metrics within 3 seconds
- **Security:** No Azure credentials stored in the database; all Azure calls use the user's delegated token
- **Responsiveness:** Usable on 1024px+ desktop viewports; mobile is a stretch goal
- **Error Handling:** Graceful degradation if a resource group is inaccessible (RBAC denied); display partial results with warnings
- **Observability:** Application logs to its own Application Insights workspace

---

## Future / Out of Scope (v1)

- Alerting / notifications
- Multi-tenant support
- Cost analysis / Azure Cost Management integration
- Log export (CSV / JSON)
- Dark mode
