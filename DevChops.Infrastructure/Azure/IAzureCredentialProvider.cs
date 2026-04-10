using Azure.Core;

namespace DevChops.Infrastructure.Azure;

public interface IAzureCredentialProvider
{
    TokenCredential GetCredential();
}
