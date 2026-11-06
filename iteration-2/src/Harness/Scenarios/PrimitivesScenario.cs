using EmberTrace.Abstractions.Attributes;
using Harness.Core;
using Harness.Scenarios;
using Microsoft.Extensions.Primitives;

[assembly: TraceId(PrimitivesScopes.StringValues, "primitives.string-values.x10k", "primitives")]
[assembly: TraceId(PrimitivesScopes.StringSegment, "primitives.string-segment.x10k", "primitives")]
[assembly: TraceId(PrimitivesScopes.ChangeToken, "primitives.change-token-on-change.x1k", "primitives")]
[assembly: TraceId(PrimitivesScopes.CompositeChangeToken, "primitives.composite-change-token.x1k", "primitives")]

namespace Harness.Scenarios;

public static class PrimitivesScopes
{
    public const int StringValues = 1800;
    public const int StringSegment = 1801;
    public const int ChangeToken = 1802;
    public const int CompositeChangeToken = 1803;
}

public sealed class PrimitivesScenario : IScenario
{
    private const int Batch = 10_000;
    private const int Notifications = 1000;
    private const string Text = "alpha, beta, gamma ,delta, epsilon, zeta";

    private static readonly char[] Separators = [','];

    public string Name => "primitives";

    public void Cold(Recorder recorder)
    {
    }

    public void Iteration(Recorder recorder)
    {
        var single = new StringValues("alpha");
        var many = new StringValues(new[] { "x", "y", "z" });
        var other = new StringValues(new[] { "x", "y", "z" });
        recorder.Measure(PrimitivesScopes.StringValues, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(StringValues.Concat(single, many));
                Sink.Keep(many.ToString());
                Sink.Keep(many.Equals(other) ? 1 : 0);
            }
        });

        var segment = new StringSegment(Text);
        recorder.Measure(PrimitivesScopes.StringSegment, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                long length = segment.IndexOf(',');
                foreach (var token in segment.Split(Separators))
                {
                    length += token.Trim().Length;
                }

                Sink.Keep(length + (segment.Equals(Text, StringComparison.Ordinal) ? 1 : 0));
            }
        });

        recorder.Measure(PrimitivesScopes.ChangeToken, Notifications, () =>
        {
            var current = new CancellationTokenSource();
            var count = 0;
            using var subscription = ChangeToken.OnChange(() => new CancellationChangeToken(current.Token), () => count++);
            for (var i = 0; i < Notifications; i++)
            {
                var previous = current;
                current = new CancellationTokenSource();
                previous.Cancel();
                previous.Dispose();
            }

            current.Dispose();
            Sink.Keep(count);
        });

        recorder.Measure(PrimitivesScopes.CompositeChangeToken, Notifications, () =>
        {
            var calls = 0;
            for (var i = 0; i < Notifications; i++)
            {
                using var first = new CancellationTokenSource();
                using var second = new CancellationTokenSource();
                using var third = new CancellationTokenSource();
                var composite = new CompositeChangeToken(new IChangeToken[]
                {
                    new CancellationChangeToken(first.Token),
                    new CancellationChangeToken(second.Token),
                    new CancellationChangeToken(third.Token),
                });
                using var registration = composite.RegisterChangeCallback(_ => calls++, null);
                second.Cancel();
            }

            Sink.Keep(calls);
        });
    }

    public void Dispose()
    {
    }
}
