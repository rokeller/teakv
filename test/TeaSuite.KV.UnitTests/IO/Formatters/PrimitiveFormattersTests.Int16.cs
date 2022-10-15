using static TeaSuite.KV.IO.Formatters.PrimitiveFormatters;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormattersTests
{
    public sealed class Int16FormatterTests : FormatterTestsBase<short>
    {
        public Int16FormatterTests() : base(new Int16Formatter()) { }

        [Theory]
        [MemberData(nameof(ValuesForRoundTripTest))]
        public override Task ReadWriteRoundtripWorks(short valueToWrite)
        {
            return base.ReadWriteRoundtripWorks(valueToWrite);
        }

        protected override int DataLength => sizeof(short);

        public static IEnumerable<object[]> ValuesForRoundTripTest => MakeMemberData(
            Int16.MinValue,
            Int16.MaxValue,
            0,
            123);
    }
}
