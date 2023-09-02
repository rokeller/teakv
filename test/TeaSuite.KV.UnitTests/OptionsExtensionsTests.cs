namespace TeaSuite.KV;

public sealed class OptionsExtensionsTests
{
    [Fact]
    public void GetOptionsNameSameForSameTypes()
    {
        Assert.Equal(OptionsExtensions.GetOptionsName<uint, bool>(),
                     OptionsExtensions.GetOptionsName<UInt32, Boolean>());

        Assert.Equal(OptionsExtensions.GetOptionsName<Tuple<uint, int>, bool>(),
                     OptionsExtensions.GetOptionsName<Tuple<UInt32, Int32>, Boolean>());
    }

    [Fact]
    public void GetOptionsNameDifferentForDifferentTypes()
    {
        Assert.NotEqual(OptionsExtensions.GetOptionsName<uint, bool>(),
                        OptionsExtensions.GetOptionsName<ulong, bool>());

        Assert.NotEqual(OptionsExtensions.GetOptionsName<Boolean, uint>(),
                        OptionsExtensions.GetOptionsName<bool, int>());

        Assert.NotEqual(OptionsExtensions.GetOptionsName<Tuple<uint, int>, bool>(),
                        OptionsExtensions.GetOptionsName<Tuple<int, int>, Boolean>());

        Assert.NotEqual(OptionsExtensions.GetOptionsName<Tuple<uint, int>, bool>(),
                        OptionsExtensions.GetOptionsName<Tuple<int, int>, bool>());

        Assert.NotEqual(OptionsExtensions.GetOptionsName<Tuple<int, uint>, bool>(),
                        OptionsExtensions.GetOptionsName<Tuple<int, int>, bool>());
    }
}
