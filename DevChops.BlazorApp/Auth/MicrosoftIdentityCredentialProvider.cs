using Azure.Core;
using DevChops.Infrastructure.Azure;
using Microsoft.Identity.Web;

namespace DevChops.BlazorApp.Auth;

/// <summary>
/// Provides an Azure <see cref="TokenCredential"/> backed by the currently
/// signed-in user's token via Microsoft.Identity.Web (On-Behalf-Of flow).
/// </summary>
public class MicrosoftIdentityCredentialProvider(ITokenAcquisition tokenAcquisition)
    : IAzureCredentialProvider
{
    private static readonly string[] ManagementScopes =
        ["https://management.azure.com/user_impersonation"];

    public TokenCredential GetCredential() =>
        new AcquiredTokenCredential(tokenAcquisition, ManagementScopes);
}

internal sealed class AcquiredTokenCredential(
    ITokenAcquisition tokenAcquisition,
    string[] scopes) : TokenCredential
{
    public override AccessToken GetToken(
        TokenRequestContext requestContext, CancellationToken cancellationToken) =>
        GetTokenAsync(requestContext, cancellationToken).GetAwaiter().GetResult();

    public override async ValueTask<AccessToken> GetTokenAsync(
        TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        var token = await tokenAcquisition.GetAccessTokenForUserAsync(scopes);
        return new AccessToken(token, DateTimeOffset.UtcNow.AddMinutes(55));
    }
}
