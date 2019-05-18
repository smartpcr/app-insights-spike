// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CorrelationManager.cs" company="Microsoft Corporation">
//   Copyright 2019 Microsoft Corporation, all rights reserved
// </copyright>
// <summary>
//  
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AsyncLocalSpike
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    public static class CorrelationManager
    {
        private static AsyncLocal<Activity> _callActivity = new AsyncLocal<Activity>();

        public static OperationScope StartOperation(this object caller, TelemetryClient telemetry, string operationName)
        {
            var activity = new Activity(operationName);
            if (_callActivity.Value != null)
            {
                var parentActivity = _callActivity.Value;
                activity.SetParentId(parentActivity.Id);
            }
            _callActivity.Value = activity;
            activity.Start();

            return new OperationScope(telemetry, activity);
        }
    }

    public class OperationScope : IDisposable
    {
        private readonly TelemetryClient _telemetry;
        private IOperationHolder<RequestTelemetry> _requestOperation;
        private IOperationHolder<DependencyTelemetry> _dependencyOperation;
        public string OperationName { get; private set; }

        public OperationScope(TelemetryClient telemetry, Activity activity)
        {
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
            if (activity==null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            OperationName = activity.OperationName;
            if (activity.ParentId == null)
            {
                _requestOperation = _telemetry.StartOperation<RequestTelemetry>(activity);
            }
            else
            {
                _dependencyOperation = _telemetry.StartOperation<DependencyTelemetry>(activity);
            }
        }

        public void Dispose()
        {
            if (_requestOperation != null)
            {
                _telemetry.StopOperation(_requestOperation);
                _requestOperation.Dispose();
                _requestOperation = null;
            }
            if (_dependencyOperation != null)
            {
                _telemetry.StopOperation(_dependencyOperation);
                _dependencyOperation.Dispose();
                _dependencyOperation = null;
            }
        }
    }
}