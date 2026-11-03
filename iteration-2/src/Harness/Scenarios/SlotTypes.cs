namespace Harness.Scenarios;

public interface ISlot<TFirst, TSecond, TThird>;

public sealed class Slot<TFirst, TSecond, TThird> : ISlot<TFirst, TSecond, TThird>;

public sealed class M0;

public sealed class M1;

public sealed class M2;

public sealed class M3;

public sealed class M4;

public sealed class M5;

public sealed class M6;

public sealed class M7;

public sealed class M8;

public sealed class M9;

public static class SlotTypes
{
    public static readonly Type[] Markers =
        [typeof(M0), typeof(M1), typeof(M2), typeof(M3), typeof(M4), typeof(M5), typeof(M6), typeof(M7), typeof(M8), typeof(M9)];

    public static (Type Service, Type Implementation)[] Create(int count)
    {
        var result = new (Type, Type)[count];
        for (var i = 0; i < count; i++)
        {
            var arguments = new[] { Markers[i % 10], Markers[i / 10 % 10], Markers[i / 100 % 10] };
            result[i] = (typeof(ISlot<,,>).MakeGenericType(arguments), typeof(Slot<,,>).MakeGenericType(arguments));
        }

        return result;
    }
}
