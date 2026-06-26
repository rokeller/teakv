using AutoFixture.Xunit3;

namespace TeaSuite.KV.Data;

public sealed class DefaultMemoryKeyValueStoreFactoryTests
{
    [Theory, AutoData]
    public void CreateWorks(DefaultMemoryKeyValueStoreFactory<int, int> factory)
    {
        IMemoryKeyValueStore<int, int> store = factory.Create();
        Assert.IsType<DefaultMemoryKeyValueStore<int, int>>(store);
    }
}
