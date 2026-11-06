using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using EmberTrace.Abstractions.Attributes;
using Harness.Core;
using Harness.TrackB.Scenarios;

[assembly: TraceId(MetadataScopes.Reader, "metadata.read-types-methods-signatures.x10", "metadata")]
[assembly: TraceId(MetadataScopes.Builder, "metadata.builder-types-blobs.x100", "metadata")]

namespace Harness.TrackB.Scenarios;

public static class MetadataScopes
{
    public const int Reader = 2420;
    public const int Builder = 2421;
}

public sealed class MetadataScenario : IScenario
{
    private const int TypeCount = 1000;

    private readonly byte[] _image = File.ReadAllBytes(typeof(JsonSerializer).Assembly.Location);
    private readonly string[] _typeNames = Enumerable.Range(0, TypeCount).Select(i => $"Type{i}").ToArray();

    public string Name => "metadata";

    public void Cold(Recorder recorder)
    {
    }

    public void Iteration(Recorder recorder)
    {
        var image = _image;
        recorder.Measure(MetadataScopes.Reader, 10, () =>
        {
            for (var i = 0; i < 10; i++)
            {
                using var stream = new MemoryStream(image, writable: false);
                using var peReader = new PEReader(stream);
                var reader = peReader.GetMetadataReader();
                long total = 0;
                foreach (var handle in reader.TypeDefinitions)
                {
                    var definition = reader.GetTypeDefinition(handle);
                    total += reader.GetString(definition.Name).Length;
                }

                foreach (var handle in reader.MethodDefinitions)
                {
                    var signature = reader.GetBlobReader(reader.GetMethodDefinition(handle).Signature);
                    total += signature.ReadCompressedInteger();
                }

                Sink.Keep(total);
            }
        });

        var names = _typeNames;
        recorder.Measure(MetadataScopes.Builder, 100, () =>
        {
            for (var i = 0; i < 100; i++)
            {
                var metadata = new MetadataBuilder();
                var blobs = new BlobBuilder();
                var @namespace = metadata.GetOrAddString("Harness.Generated");
                foreach (var name in names)
                {
                    metadata.AddTypeDefinition(
                        TypeAttributes.Public | TypeAttributes.Class,
                        @namespace,
                        metadata.GetOrAddString(name),
                        default,
                        MetadataTokens.FieldDefinitionHandle(1),
                        MetadataTokens.MethodDefinitionHandle(1));
                }

                for (var j = 0; j < TypeCount; j++)
                {
                    blobs.WriteCompressedInteger(j);
                }

                Sink.Keep(metadata.GetRowCount(TableIndex.TypeDef));
                Sink.Keep(blobs.Count);
            }
        });
    }

    public void Dispose()
    {
    }
}
