using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AsyncLocalSpike
{
    class Program
    {
        static Program()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        }

        static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, configBuilder) =>
                {
                    configBuilder.AddJsonFile("appsettings.json", optional: true);
                })
                .ConfigureServices((hostingContext, services) =>
                {
                    services.AddKeyVault(hostingContext.Configuration);
                    services.AddOptions();
                    services.Configure<VaultSettings>(hostingContext.Configuration.GetSection("Vault"));
                    services.Configure<DocDbSettings>(hostingContext.Configuration.GetSection("DocDb"));
                    var serviceProvider = services.BuildServiceProvider();
                    services.AddAppInsights(hostingContext.Configuration, serviceProvider.GetRequiredService<IKeyVaultClient>());
                    services.AddLogging(hostingContext.Configuration);
                    
                    services.AddHostedService<AsyncWorker>();
                });

            await builder.RunConsoleAsync();
        }
    }
}
