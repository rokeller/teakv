using static TeaSuite.KV.IO.Formatters.PrimitiveFormatters;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormattersTests
{
    public sealed class BooleanFormatterTests : FormatterTestsBase<bool>
    {
        public BooleanFormatterTests() : base(new BooleanFormatter()) { }

        [Theory]
        [MemberData(nameof(ValuesForRoundTripTest))]
        public override Task ReadWriteRoundtripWorks(bool valueToWrite)
        {
            return base.ReadWriteRoundtripWorks(valueToWrite);
        }

        protected override int DataLength => sizeof(bool);

        public static IEnumerable<object[]> ValuesForRoundTripTest => MakeMemberData(true, false);
    }
}
