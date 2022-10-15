using System.Security.Cryptography;
using static TeaSuite.KV.IO.Formatters.PrimitiveFormatters;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormattersTests
{
    public sealed class StringFormatterTests
    {
        private readonly StringFormatter formatter = new StringFormatter();

        [Theory]
        [MemberData(nameof(ValuesForRoundTripTest))]
        public async Task ReadWriteRoundtripWorks(string valueToWrite)
        {
            using MemoryStream memstr = new MemoryStream();

            await formatter.WriteAsync(valueToWrite, memstr, default);

            memstr.Position = 0;
            string readValue = await formatter.ReadAsync(memstr, default);

            Assert.Equal(valueToWrite, readValue);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(123)]
        [InlineData(45)]
        [InlineData(255)]

        public async Task SkipReadAsyncWorks(byte sentinel)
        {
            using MemoryStream memstr = new MemoryStream();

            int stringLength = RandomNumberGenerator.GetInt32(0, 4 * 1024);
            await new Int32Formatter().WriteAsync(stringLength, memstr, default);
            StreamUtils.WriteRandom(stringLength, memstr);
            memstr.WriteByte(sentinel);

            memstr.Position = 0;
            await formatter.SkipReadAsync(memstr, default);
            Assert.Equal(sentinel, memstr.ReadByte());

            using Stream nonSeekable = StreamUtils.WrapNonSeekable(memstr);
            nonSeekable.Position = 0;
            await formatter.SkipReadAsync(nonSeekable, default);
            Assert.Equal(sentinel, nonSeekable.ReadByte());
        }

        public static IEnumerable<object[]> ValuesForRoundTripTest => FormatterTestsBase<string>.MakeMemberData(
            String.Empty,
            "abc",
            "AbC",
            "ThisIsALongerString",
            new String('x', 1234)
        );
    }
}
