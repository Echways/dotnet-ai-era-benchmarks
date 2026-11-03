namespace Harness.Core;

public interface IScenario : IDisposable
{
    string Name { get; }

    void Cold(Recorder recorder);

    void Iteration(Recorder recorder);
}

public interface IScenarioArtifacts
{
    void WriteArtifacts(string directory, string stem);
}
