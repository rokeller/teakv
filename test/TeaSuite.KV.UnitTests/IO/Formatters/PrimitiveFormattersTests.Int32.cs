using static TeaSuite.KV.IO.Formatters.PrimitiveFormatters;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormattersTests
{
    public sealed class Int32FormatterTests : FormatterTestsBase<int>
    {
        public Int32FormatterTests() : base(new Int32Formatter()) { }

        [Theory]
        [MemberData(nameof(ValuesForRoundTripTest))]
        public override Task ReadWriteRoundtripWorks(int valueToWrite)
        {
            return base.ReadWriteRoundtripWorks(valueToWrite);
        }

        protected override int DataLength => sizeof(int);

        public static IEnumerable<object[]> ValuesForRoundTripTest => MakeMemberData(
            Int32.MinValue,
            Int32.MaxValue,
            0,
            123);
    }
}
