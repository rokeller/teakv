using static TeaSuite.KV.IO.Formatters.PrimitiveFormatters;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormattersTests
{
    public sealed class Int64FormatterTests : FormatterTestsBase<long>
    {
        public Int64FormatterTests() : base(new Int64Formatter()) { }


        [Theory]
        [MemberData(nameof(ValuesForRoundTripTest))]
        public override Task ReadWriteRoundtripWorks(long valueToWrite)
        {
            return base.ReadWriteRoundtripWorks(valueToWrite);
        }

        protected override int DataLength => sizeof(long);

        public static IEnumerable<object[]> ValuesForRoundTripTest => MakeMemberData(
            Int64.MinValue,
            Int64.MaxValue,
            0,
            123);
    }
}
