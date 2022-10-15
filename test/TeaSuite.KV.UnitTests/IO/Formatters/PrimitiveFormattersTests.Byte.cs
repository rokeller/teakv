using static TeaSuite.KV.IO.Formatters.PrimitiveFormatters;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormattersTests
{
    public sealed class ByteFormatterTests : FormatterTestsBase<byte>
    {
        public ByteFormatterTests() : base(new ByteFormatter()) { }

        [Theory]
        [MemberData(nameof(ValuesForRoundTripTest))]
        public override Task ReadWriteRoundtripWorks(byte valueToWrite)
        {
            return base.ReadWriteRoundtripWorks(valueToWrite);
        }

        protected override int DataLength => sizeof(byte);

        public static IEnumerable<object[]> ValuesForRoundTripTest => MakeMemberData(0, 1, 5, 16, 64, 123, 255);
    }
}
