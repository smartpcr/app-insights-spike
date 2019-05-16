using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace AsyncLocalSpike
{
    public class AsyncWorker : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken cancel)
        {
            while (!cancel.IsCancellationRequested)
            {
                var callTree = CallTree.CreateTestCallGraph();
                await callTree.Execute();
                await Task.Delay(1000);
            }
            Console.WriteLine("Task cancelled");
        }
    }
}
