namespace TeaSuite.KV;

internal static class TestDataUtils
{
    public static void CopyTestData(string relSourcePath, string targetPath, bool respectEndianness = false)
    {
        if (respectEndianness)
        {
            if (BitConverter.IsLittleEndian)
            {
                relSourcePath = Path.Combine("LE", relSourcePath);
            }
            else
            {
                relSourcePath = Path.Combine("BE", relSourcePath);
            }
        }

        File.Copy(Path.Combine("./TestData", relSourcePath), targetPath);
    }
}
