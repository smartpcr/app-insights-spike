﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;

namespace AsyncLocalSpike
{
    public class CallTree 
    {
        public string Name { get; set; }
        public CallTree Parent { get; set; }
        public bool IsAsynchronous { get; set; }
        public List<CallTree> Children { get; set; }

        public CallTree(string name) : this(name, null)
        {
        }

        public CallTree(string name, CallTree parent)
        {
            Name = name;
            Parent = parent;
            IsAsynchronous = false;
            Children = new List<CallTree>();
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            if (Parent != null)
            {
                builder.Append(Parent.ToString() + ".");
            }
            builder.Append(Name);
            return builder.ToString();
        }

        public async Task Execute(TelemetryClient telemetry, ILogger<CallTree> logger)
        {
            using (var operation = this.StartOperation(telemetry, this.ToString()))
            {
                if (IsAsynchronous)
                {
                    await ExecuteAsync(telemetry, logger);
                }
                else
                {
                    ExecuteSync(telemetry, logger);
                }
            }
        }

        private async Task ExecuteAsync(TelemetryClient telemetry, ILogger<CallTree> logger)
        {
            using (var operation = this.StartOperation(telemetry, this.ToString()))
            {
                logger.LogInformation($"Entering '{operation.OperationName}'");
                await Task.Delay(new Random().Next(1000));
                var tasks = new List<Task>();

                if (Children?.Count > 0)
                {
                    foreach (var child in Children.Where(c => c.IsAsynchronous))
                    {
                        tasks.Add(child.ExecuteAsync(telemetry, logger));
                    }

                    foreach (var child in Children.Where(c => !c.IsAsynchronous))
                    {
                        child.ExecuteSync(telemetry, logger);
                    }

                }

                await Task.WhenAll(tasks.ToArray());

                logger.LogInformation($"Exiting '{operation.OperationName}'");
            }
        }

        private void ExecuteSync(TelemetryClient telemetry, ILogger<CallTree> logger)
        {
            using (var operation = this.StartOperation(telemetry, this.ToString()))
            {
                logger.LogInformation($"Entering '{operation.OperationName}'");
                var tasks = new List<Task>();

                if (Children?.Count > 0)
                {
                    foreach (var child in Children.Where(c => c.IsAsynchronous))
                    {
                        tasks.Add(child.ExecuteAsync(telemetry, logger));
                    }

                    foreach (var child in Children.Where(c => !c.IsAsynchronous))
                    {
                        child.ExecuteSync(telemetry, logger);
                    }

                }

                Task.WaitAll(tasks.ToArray());

                logger.LogInformation($"Exiting '{operation.OperationName}'");
            }
        }

        /// <summary>
        /// call A
        /// within A, call async a1 and a2
        /// Then call B
        /// within B, call b1, then call b2
        /// Call C
        /// within C, call async c1 and c2
        /// also call c3
        /// 
        /// A(a1,a2)->B(b1->b2)->C((c1,c2)->c3)
        /// </summary>
        /// <returns></returns>
        public static CallTree CreateTestCallGraph()
        {
            var root = new CallTree("root");
            var A = new CallTree("A", root);
            var a1 = new CallTree("a1", A) { IsAsynchronous = true };
            var a2 = new CallTree("a2", A) { IsAsynchronous = true };
            A.Children = new List<CallTree> { a1, a2 };
            var B = new CallTree("B", root);
            var b1 = new CallTree("b1", B);
            var b2 = new CallTree("b2", B);
            B.Children = new List<CallTree> { b1, b2 };
            var C = new CallTree("C", root);
            var c1 = new CallTree("c1", C) { IsAsynchronous = true };
            var c2 = new CallTree("c2", C) { IsAsynchronous = true };
            var c3 = new CallTree("c3", C);
            C.Children = new List<CallTree> { c1, c2, c3 };
            root.Children = new List<CallTree> { A, B, C };

            return root;
        }

        
    }
}
