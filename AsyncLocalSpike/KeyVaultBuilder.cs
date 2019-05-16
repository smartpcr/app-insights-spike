using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace AsyncLocalSpike
{
    public static class KeyVaultBuilder
    {
        public static IServiceCollection AddKeyVault(this IServiceCollection services, IConfiguration configuration)
        {
            var vaultSettings = new VaultSettings();
            configuration.Bind("Vault", vaultSettings);
            KeyVaultClient.AuthenticationCallback callback = async (authority, resource, scope) =>
            {
                var authContext = new AuthenticationContext(authority);
                var certificate = new X509Certificate2(vaultSettings.ClientCertFile);
                var clientCred = new ClientAssertionCertificate(vaultSettings.ClientId, certificate);
                var result = await authContext.AcquireTokenAsync(resource, clientCred);

                if (result == null)
                    throw new InvalidOperationException("Failed to obtain the JWT token");

                return result.AccessToken;
            };
            var kvClient = new KeyVaultClient(callback);
            services.AddSingleton<IKeyVaultClient>(kvClient);

            return services;
        }
    }
}
