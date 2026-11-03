namespace Harness.Core.Data;

public sealed class Poco50
{
    public int P00 { get; set; }

    public int P01 { get; set; }

    public string P02 { get; set; } = "";

    public bool P03 { get; set; }

    public double P04 { get; set; }

    public int P05 { get; set; }

    public int P06 { get; set; }

    public string P07 { get; set; } = "";

    public bool P08 { get; set; }

    public double P09 { get; set; }

    public int P10 { get; set; }

    public int P11 { get; set; }

    public string P12 { get; set; } = "";

    public bool P13 { get; set; }

    public double P14 { get; set; }

    public int P15 { get; set; }

    public int P16 { get; set; }

    public string P17 { get; set; } = "";

    public bool P18 { get; set; }

    public double P19 { get; set; }

    public int P20 { get; set; }

    public int P21 { get; set; }

    public string P22 { get; set; } = "";

    public bool P23 { get; set; }

    public double P24 { get; set; }

    public int P25 { get; set; }

    public int P26 { get; set; }

    public string P27 { get; set; } = "";

    public bool P28 { get; set; }

    public double P29 { get; set; }

    public int P30 { get; set; }

    public int P31 { get; set; }

    public string P32 { get; set; } = "";

    public bool P33 { get; set; }

    public double P34 { get; set; }

    public int P35 { get; set; }

    public int P36 { get; set; }

    public string P37 { get; set; } = "";

    public bool P38 { get; set; }

    public double P39 { get; set; }

    public int P40 { get; set; }

    public int P41 { get; set; }

    public string P42 { get; set; } = "";

    public bool P43 { get; set; }

    public double P44 { get; set; }

    public int P45 { get; set; }

    public int P46 { get; set; }

    public string P47 { get; set; } = "";

    public bool P48 { get; set; }

    public double P49 { get; set; }
}

public static class Poco50Values
{
    public static IEnumerable<KeyValuePair<string, string>> Create()
    {
        for (var i = 0; i < 50; i++)
        {
            var value = (i % 5) switch
            {
                0 or 1 => (i * 7).ToString(System.Globalization.CultureInfo.InvariantCulture),
                2 => $"text-{i}",
                3 => (i % 2 == 0) ? "true" : "false",
                _ => (i * 1.25).ToString(System.Globalization.CultureInfo.InvariantCulture),
            };
            yield return new KeyValuePair<string, string>($"P{i:00}", value);
        }
    }
}
