using System.Text;
using System.Text.Encodings.Web;
using EmberTrace.Abstractions.Attributes;
using Harness.Core;
using Harness.TrackB.Scenarios;

[assembly: TraceId(TextScopes.Encode, "text.encoders-encode.x10k", "text")]
[assembly: TraceId(TextScopes.EncodeUtf8, "text.encode-utf8-find-first.x10k", "text")]
[assembly: TraceId(TextScopes.CodePages, "text.code-pages-get-encoding.x10k", "text")]

namespace Harness.TrackB.Scenarios;

public static class TextScopes
{
    public const int Encode = 2410;
    public const int EncodeUtf8 = 2411;
    public const int CodePages = 2412;
}

public sealed unsafe class TextScenario : IScenario
{
    private const int Batch = 10_000;
    private const string Markup = "<a href=\"/search?q=cats&dogs\">Tom & Jerry's «café»</a>";

    private readonly byte[] _utf8 = Encoding.UTF8.GetBytes(Markup);

    public string Name => "text";

    public void Cold(Recorder recorder)
    {
    }

    public void Iteration(Recorder recorder)
    {
        recorder.Measure(TextScopes.Encode, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(HtmlEncoder.Default.Encode(Markup));
                Sink.Keep(UrlEncoder.Default.Encode(Markup));
                Sink.Keep(JavaScriptEncoder.Default.Encode(Markup));
            }
        });

        var source = _utf8;
        var destination = new byte[source.Length * 6];
        recorder.Measure(TextScopes.EncodeUtf8, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                HtmlEncoder.Default.EncodeUtf8(source, destination, out var consumed, out var written);
                Sink.Keep(consumed + written);
                fixed (char* text = Markup)
                {
                    Sink.Keep(HtmlEncoder.Default.FindFirstCharacterToEncode(text, Markup.Length));
                }
            }
        });

        var provider = CodePagesEncodingProvider.Instance;
        recorder.Measure(TextScopes.CodePages, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(provider.GetEncoding(1252));
                Sink.Keep(provider.GetEncoding("windows-1251"));
            }
        });
    }

    public void Dispose()
    {
    }
}
