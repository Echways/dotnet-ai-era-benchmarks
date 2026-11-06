using System.Diagnostics;
using System.Diagnostics.Metrics;
using EmberTrace.Abstractions.Attributes;
using Harness.Core;
using Harness.TrackB.Scenarios;

[assembly: TraceId(DiagnosticsScopes.Activity, "diagnostics.activity-start-tag-stop.x10k", "diagnostics")]
[assembly: TraceId(DiagnosticsScopes.Metrics, "diagnostics.counter-add-histogram-record.x10k", "diagnostics")]
[assembly: TraceId(DiagnosticsScopes.MeterCreate, "diagnostics.meter-create-counter.x1k", "diagnostics")]
[assembly: TraceId(DiagnosticsScopes.Listener, "diagnostics.listener-is-enabled-write.x10k", "diagnostics")]

namespace Harness.TrackB.Scenarios;

public static class DiagnosticsScopes
{
    public const int Activity = 2400;
    public const int Metrics = 2401;
    public const int MeterCreate = 2402;
    public const int Listener = 2403;
}

public sealed class NoOpObserver : IObserver<KeyValuePair<string, object?>>
{
    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(KeyValuePair<string, object?> value)
    {
    }
}

public sealed class DiagnosticsScenario : IScenario
{
    private const int Batch = 10_000;

    public string Name => "diagnostics";

    public void Cold(Recorder recorder)
    {
    }

    public void Iteration(Recorder recorder)
    {
        using var source = new ActivitySource("Harness.Diagnostics");
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = candidate => candidate.Name == "Harness.Diagnostics",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(activityListener);
        recorder.Measure(DiagnosticsScopes.Activity, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                var activity = source.StartActivity("operation");
                activity?.SetTag("request.id", i);
                activity?.Stop();
            }
        });

        using var meter = new Meter("Harness.Metrics");
        var counter = meter.CreateCounter<long>("requests");
        var histogram = meter.CreateHistogram<double>("latency");
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter == meter)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>(static (_, _, _, _) => { });
        meterListener.SetMeasurementEventCallback<double>(static (_, _, _, _) => { });
        meterListener.Start();
        recorder.Measure(DiagnosticsScopes.Metrics, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                counter.Add(1);
                histogram.Record(i * 0.5);
            }
        });

        recorder.Measure(DiagnosticsScopes.MeterCreate, 1000, () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                using var scratch = new Meter("Harness.Scratch");
                Sink.Keep(scratch.CreateCounter<long>("c"));
            }
        });

        using var diagnosticListener = new DiagnosticListener("Harness.Listener");
        using var subscription = diagnosticListener.Subscribe(new NoOpObserver());
        var payload = new { Id = 1 };
        recorder.Measure(DiagnosticsScopes.Listener, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                if (diagnosticListener.IsEnabled("Event"))
                {
                    diagnosticListener.Write("Event", payload);
                }
            }
        });
    }

    public void Dispose()
    {
    }
}
