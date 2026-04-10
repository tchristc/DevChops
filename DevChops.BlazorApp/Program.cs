using DevChops.BlazorApp.Auth;
using DevChops.BlazorApp.Components;
using DevChops.Infrastructure;
using DevChops.Infrastructure.Azure;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Authentication — Azure Entra ID via Microsoft.Identity.Web (OIDC + On-Behalf-Of)
builder.Services
    .AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd")
    .EnableTokenAcquisitionToCallDownstreamApi(
        ["https://management.azure.com/user_impersonation"])
    .AddInMemoryTokenCaches();

builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();
builder.Services.AddAuthorization();

// Azure credential bridge — wraps ITokenAcquisition as a TokenCredential for the Azure SDK
builder.Services.AddScoped<IAzureCredentialProvider, MicrosoftIdentityCredentialProvider>();

// MudBlazor
builder.Services.AddMudServices();

// Infrastructure + Application services (repos, query services, in-memory stores)
builder.Services.AddInfrastructure();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers(); // required for /MicrosoftIdentity/Account/SignIn|SignOut
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

