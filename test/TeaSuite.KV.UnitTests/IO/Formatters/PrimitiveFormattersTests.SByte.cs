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

        protected override int DataLength => sizeof(sbyte);

        public static IEnumerable<object[]> ValuesForRoundTripTest => MakeMemberData(
            SByte.MinValue,
            SByte.MaxValue,
            0, 1, 100);
    }
}
