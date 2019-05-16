using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace AsyncLocalSpike
{
    public static class AppInsightsBuilder
    {
        public static IServiceCollection AddAppInsights(this IServiceCollection services, IConfiguration configuration)
        {
            var appInsightsConfig = TelemetryConfiguration.Active;
            appInsightsConfig.InstrumentationKey = configuration["AppInsights:InstrumentationKey"];
            appInsightsConfig.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
            appInsightsConfig.TelemetryInitializers.Add(new HttpDependenciesParsingTelemetryInitializer());
            var serviceContext = new ServiceContext();
            configuration.Bind("AppInsights:Context", serviceContext);
            appInsightsConfig.TelemetryInitializers.Add(new ContextTelemetryInitializer(serviceContext));

            var module = new DependencyTrackingTelemetryModule();
            module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
            module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.EventHubs");
            module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.KeyVault");
            module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.DocumentDB");
            module.Initialize(appInsightsConfig);

            var telemetryClient = new TelemetryClient();
            telemetryClient.TrackTrace("Program started...");
            services.AddSingleton(telemetryClient);

            return services;
        }

        public static IServiceCollection AddLogging(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConsole();
                loggingBuilder.AddSerilog();

                var log = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .Enrich.FromLogContext()
                    .ReadFrom.Configuration(configuration)
                    .WriteTo.Console()
                    .WriteTo.ApplicationInsights(TelemetryConfiguration.Active, TelemetryConverter.Traces)
                    .CreateLogger();
                Log.Logger = log;
            });

            return services;
        }

    }

    internal class ContextTelemetryInitializer : ITelemetryInitializer
    {
        public ContextTelemetryInitializer(ServiceContext serviceContext)
        {
            ServiceContext = serviceContext;
        }

        public ServiceContext ServiceContext { get; }

        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Cloud.RoleName = ServiceContext.Name;
            telemetry.Context.Component.Version = ServiceContext.Version;
            if (ServiceContext.Tags?.Any() == true)
            {
                telemetry.Context.GlobalProperties["tags"] = string.Join(",", ServiceContext.Tags);
            }
        }
    }

    public class ServiceContext
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string[] Tags { get; set; }
    }
}
