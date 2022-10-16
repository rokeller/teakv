using TeaSuite.KV.Policies;

namespace TeaSuite.KV;

public sealed class StoreSettingsTests
{
    private readonly StoreSettings settings = new StoreSettings();

    [Fact]
    public void DefaultPersistPolicyIsUsedByDefault()
    {
        Assert.Equal(new DefaultPersistPolicy(), settings.PersistPolicy);
        Assert.NotEqual(new DefaultPersistPolicy(1, TimeSpan.Zero), settings.PersistPolicy);
    }

    [Fact]
    public void DefaultIndexPolicyIsUsedByDefault()
    {
        Assert.Equal(new DefaultIndexPolicy(), settings.IndexPolicy);
        Assert.NotEqual(new DefaultIndexPolicy(1, 1), settings.IndexPolicy);
    }

    [Fact]
    public void DefaultMergePolicyIsUsedByDefault()
    {
        Assert.Equal(new DefaultMergePolicy(), settings.MergePolicy);
        Assert.NotEqual(new DefaultMergePolicy(1), settings.MergePolicy);
    }
}
