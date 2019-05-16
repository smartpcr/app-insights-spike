using System;
using System.Threading;
using System.Threading.Tasks;
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

                    var config = configBuilder.Build();
                    var certThumbprint = config["Vault:ClientCertThumbprint"];
                    var cert = CertUtil.GetCertificate(certThumbprint);
                    configBuilder.AddAzureKeyVault(
                        $"https://{config["Vault:Name"]}.vault.azure.net",
                        config["Vault:ClientId"],
                        cert);
                })
                .ConfigureServices((hostingContext, services) =>
                {
                    services.AddOptions();
                    services.Configure<VaultSettings>(hostingContext.Configuration.GetSection("Vault"));
                    services.Configure<DocDbSettings>(hostingContext.Configuration.GetSection("DocDb"));
                    services.AddAppInsights(hostingContext.Configuration);
                    services.AddLogging(hostingContext.Configuration);

                    services.AddHostedService<AsyncWorker>();
                });

            await builder.RunConsoleAsync();
        }

    }
}
