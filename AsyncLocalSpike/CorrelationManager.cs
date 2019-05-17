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
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    public static class CorrelationManager
    {
        public static OperationScope StartOperation(this ICallContext call, TelemetryClient telemetry)
        {
            var activity = new Activity(call.OperationName);
            if (call.CallActivity.Value != null)
            {
                var parentActivity = call.CallActivity.Value;
                activity.SetParentId(parentActivity.Id);
            }
            call.CallActivity.Value = activity;
            activity.Start();

            return new OperationScope(telemetry, activity);
        }
    }

    public class OperationScope : IDisposable
    {
        private readonly TelemetryClient _telemetry;
        private IOperationHolder<RequestTelemetry> _requestOperation;
        private IOperationHolder<DependencyTelemetry> _dependencyOperation;

        public OperationScope(TelemetryClient telemetry, Activity activity)
        {
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
            if (activity==null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            if (activity.Parent == null)
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