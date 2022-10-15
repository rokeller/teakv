using static TeaSuite.KV.IO.Formatters.PrimitiveFormatters;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormattersTests
{
    public sealed class UInt16FormatterTests : FormatterTestsBase<ushort>
    {
        public UInt16FormatterTests() : base(new UInt16Formatter()) { }

        [Theory]
        [MemberData(nameof(ValuesForRoundTripTest))]
        public override Task ReadWriteRoundtripWorks(ushort valueToWrite)
        {
            return base.ReadWriteRoundtripWorks(valueToWrite);
        }

        protected override int DataLength => sizeof(ushort);

        public static IEnumerable<object[]> ValuesForRoundTripTest => MakeMemberData(
            UInt16.MinValue,
            UInt16.MaxValue,
            10,
            123);
    }
}
