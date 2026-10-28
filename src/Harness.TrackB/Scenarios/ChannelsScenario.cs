using System.Threading.Channels;
using EmberTrace.Abstractions.Attributes;
using Harness.Core;
using Harness.TrackB.Scenarios;

[assembly: TraceId(ChannelsScopes.ColdPipeline, "channels.cold.first-pipeline", "channels")]
[assembly: TraceId(ChannelsScopes.Bounded, "channels.bounded1024-1p4c.x100k-items", "channels")]

namespace Harness.TrackB.Scenarios;

public static class ChannelsScopes
{
    public const int ColdPipeline = 2100;
    public const int Bounded = 2110;
}

public sealed class ChannelsScenario : IScenario
{
    private const int Items = 100_000;
    private const int Consumers = 4;

    public string Name => "channels";

    public void Cold(Recorder recorder) => recorder.Cold(ChannelsScopes.ColdPipeline, () => Sink.Keep(RunPipeline(1_000)));

    public void Iteration(Recorder recorder) => recorder.MeasureProcessWide(ChannelsScopes.Bounded, Items, () => Sink.Keep(RunPipeline(Items)));

    public void Dispose()
    {
    }

    private static long RunPipeline(int items)
    {
        var channel = Channel.CreateBounded<int>(new BoundedChannelOptions(1024)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = true,
            SingleReader = false,
        });

        var consumers = new Task<long>[Consumers];
        for (var c = 0; c < Consumers; c++)
        {
            consumers[c] = Task.Run(async () =>
            {
                long sum = 0;
                while (await channel.Reader.WaitToReadAsync().ConfigureAwait(false))
                {
                    while (channel.Reader.TryRead(out var item))
                    {
                        sum += item;
                    }
                }

                return sum;
            });
        }

        var producer = Task.Run(async () =>
        {
            for (var i = 0; i < items; i++)
            {
                if (!channel.Writer.TryWrite(i))
                {
                    await channel.Writer.WriteAsync(i).ConfigureAwait(false);
                }
            }

            channel.Writer.Complete();
        });

        producer.GetAwaiter().GetResult();
        long total = 0;
        foreach (var consumer in consumers)
        {
            total += consumer.GetAwaiter().GetResult();
        }

        return total;
    }
}
