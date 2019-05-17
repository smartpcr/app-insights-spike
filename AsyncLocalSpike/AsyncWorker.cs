using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AsyncLocalSpike
{
    public class AsyncWorker : BackgroundService
    {
        private readonly ILogger<CallTree> _callTreeLogger;
        private readonly ILogger<AsyncWorker> _logger;

        public AsyncWorker(ILoggerFactory loggerFactory)
        {
            _callTreeLogger = loggerFactory.CreateLogger<CallTree>();
            _logger = loggerFactory.CreateLogger<AsyncWorker>();
        }

        protected override async Task ExecuteAsync(CancellationToken cancel)
        {
            _logger.LogInformation("Async worker started...");
            while (!cancel.IsCancellationRequested)
            {
                var callTree = CallTree.CreateTestCallGraph();
                await callTree.Execute(_callTreeLogger);
                await Task.Delay(1000);
            }
            _logger.LogWarning("Async worker cancelled.");
        }
    }
}
