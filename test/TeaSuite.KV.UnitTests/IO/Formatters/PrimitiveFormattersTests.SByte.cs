using static TeaSuite.KV.IO.Formatters.PrimitiveFormatters;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormattersTests
{
    public sealed class SByteFormatterTests : FormatterTestsBase<sbyte>
    {
        public SByteFormatterTests() : base(new SByteFormatter()) { }

        [Theory]
        [MemberData(nameof(ValuesForRoundTripTest))]
        public override Task ReadWriteRoundtripWorks(sbyte valueToWrite)
        {
            return base.ReadWriteRoundtripWorks(valueToWrite);
        }

        [Fact]
        public async Task ReadAsyncThrowsOnEOF()
        {
            using MemoryStream memstr = new();
            EndOfStreamException ex = await Assert.ThrowsAsync<EndOfStreamException>(
                () => formatter.ReadAsync(memstr, default).AsTask());

            Assert.Equal("Expected at least 1 more byte.", ex.Message);
        }

        [Fact]
        public async Task SkipAsyncThrowsOnEOF()
        {
            using MemoryStream memstr = new();
            EndOfStreamException ex = await Assert.ThrowsAsync<EndOfStreamException>(
                () => formatter.SkipReadAsync(memstr, default).AsTask());

            Assert.Equal("Expected at least 1 more byte.", ex.Message);
        }

        protected override int DataLength => sizeof(sbyte);

        public static IEnumerable<object[]> ValuesForRoundTripTest => MakeMemberData(
            SByte.MinValue,
            SByte.MaxValue,
            0, 1, 100);
    }
}
