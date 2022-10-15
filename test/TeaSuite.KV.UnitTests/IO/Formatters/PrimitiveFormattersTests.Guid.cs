using static TeaSuite.KV.IO.Formatters.PrimitiveFormatters;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormattersTests
{
    public sealed class GuidFormatterTests : FormatterTestsBase<Guid>
    {
        public GuidFormatterTests() : base(new GuidFormatter()) { }

        [Theory]
        [MemberData(nameof(ValuesForRoundTripTest))]
        public override Task ReadWriteRoundtripWorks(Guid valueToWrite)
        {
            return base.ReadWriteRoundtripWorks(valueToWrite);
        }

        protected override int DataLength => 16;

        public static IEnumerable<object[]> ValuesForRoundTripTest => MakeMemberData(Guid.Empty, Guid.NewGuid());
    }
}
