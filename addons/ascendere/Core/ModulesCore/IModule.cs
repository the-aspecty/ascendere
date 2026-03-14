public interface IModule
{
    string Name { get; }
    bool IsInitialized { get; }
    void Initialize();
    void Cleanup();
}
