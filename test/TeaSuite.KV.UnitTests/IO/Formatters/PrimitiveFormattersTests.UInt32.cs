using static TeaSuite.KV.IO.Formatters.PrimitiveFormatters;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormattersTests
{
    public sealed class UInt32FormatterTests : FormatterTestsBase<uint>
    {
        public UInt32FormatterTests() : base(new UInt32Formatter()) { }

        [Theory]
        [MemberData(nameof(ValuesForRoundTripTest))]
        public override Task ReadWriteRoundtripWorks(uint valueToWrite)
        {
            return base.ReadWriteRoundtripWorks(valueToWrite);
        }

        protected override int DataLength => sizeof(uint);

        public static IEnumerable<object[]> ValuesForRoundTripTest => MakeMemberData(
            UInt32.MinValue,
            UInt32.MaxValue,
            10,
            123);
    }
}
