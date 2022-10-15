using static TeaSuite.KV.IO.Formatters.PrimitiveFormatters;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormattersTests
{
    public sealed class DoubleFormatterTests : FormatterTestsBase<double>
    {
        public DoubleFormatterTests() : base(new DoubleFormatter()) { }

        [Theory]
        [MemberData(nameof(ValuesForRoundTripTest))]
        public override Task ReadWriteRoundtripWorks(double valueToWrite)
        {
            return base.ReadWriteRoundtripWorks(valueToWrite);
        }

        protected override int DataLength => sizeof(double);

        public static IEnumerable<object[]> ValuesForRoundTripTest => MakeMemberData(
            Double.MinValue,
            Double.MaxValue,
            0d,
            123.45d);
    }
}
