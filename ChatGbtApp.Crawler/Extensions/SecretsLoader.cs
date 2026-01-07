using CredentialManagement;
using Microsoft.Extensions.Configuration;

namespace ChatGgtApp.Crawler.Extensions;

public class SecretsLoader(IConfiguration configuration)
{
    public (string Username, string Password) Load(string target)
    {
        using var cred = new Credential
        {
            Target = target
        };
        if (!cred.Load())
            throw new InvalidOperationException($"Credential not found: {target}");

        return (cred.Username, cred.Password); // keep usage short-lived; don't log it
    }
}