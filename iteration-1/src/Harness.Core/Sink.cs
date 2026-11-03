using System.Runtime.CompilerServices;

namespace Harness.Core;

public static class Sink
{
    private static object? _last;
    private static long _accumulator;

    public static long Accumulator => Volatile.Read(ref _accumulator);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Keep(object? value) => Volatile.Write(ref _last, value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Keep(long value) => Interlocked.Add(ref _accumulator, value);

    public static bool HasValue => Volatile.Read(ref _last) is not null;
}
