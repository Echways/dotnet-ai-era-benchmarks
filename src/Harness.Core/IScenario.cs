namespace Harness.Core;

public interface IScenario : IDisposable
{
    string Name { get; }

    void Cold(Recorder recorder);

    void Iteration(Recorder recorder);
}
