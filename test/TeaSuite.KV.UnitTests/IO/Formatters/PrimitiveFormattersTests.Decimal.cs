using static TeaSuite.KV.IO.Formatters.PrimitiveFormatters;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormattersTests
{
    public sealed class DecimalFormatterTests : FormatterTestsBase<decimal>
    {
        public DecimalFormatterTests() : base(new DecimalFormatter()) { }

        [Theory]
        [MemberData(nameof(ValuesForRoundTripTest))]
        public override Task ReadWriteRoundtripWorks(decimal valueToWrite)
        {
            return base.ReadWriteRoundtripWorks(valueToWrite);
        }

        protected override int DataLength => 4 * sizeof(int);

        public static IEnumerable<object[]> ValuesForRoundTripTest => MakeMemberData(
            Decimal.MinValue,
            Decimal.MaxValue,
            0M,
            1M,
            3.1415M,
            -1234567890.123M);
    }
}
