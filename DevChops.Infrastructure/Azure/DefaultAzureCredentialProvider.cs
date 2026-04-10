using Azure.Core;
using Azure.Identity;

namespace DevChops.Infrastructure.Azure;

public class DefaultAzureCredentialProvider : IAzureCredentialProvider
{
    private static readonly DefaultAzureCredential Credential = new();

    public TokenCredential GetCredential() => Credential;
}
