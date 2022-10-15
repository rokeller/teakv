using static TeaSuite.KV.IO.Formatters.PrimitiveFormatters;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormattersTests
{
    public sealed class UInt64FormatterTests : FormatterTestsBase<ulong>
    {
        public UInt64FormatterTests() : base(new UInt64Formatter()) { }


        [Theory]
        [MemberData(nameof(ValuesForRoundTripTest))]
        public override Task ReadWriteRoundtripWorks(ulong valueToWrite)
        {
            return base.ReadWriteRoundtripWorks(valueToWrite);
        }

        protected override int DataLength => sizeof(ulong);

        public static IEnumerable<object[]> ValuesForRoundTripTest => MakeMemberData(
            UInt64.MinValue,
            UInt64.MaxValue,
            10,
            123);
    }
}
