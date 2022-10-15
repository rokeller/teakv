using static TeaSuite.KV.IO.Formatters.PrimitiveFormatters;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormattersTests
{
    public sealed class DateTimeOffsetFormatterTests : FormatterTestsBase<DateTimeOffset>
    {
        public DateTimeOffsetFormatterTests() : base(new DateTimeOffsetFormatter()) { }

        [Theory]
        [MemberData(nameof(ValuesForRoundTripTest))]
        public override Task ReadWriteRoundtripWorks(DateTimeOffset valueToWrite)
        {
            return base.ReadWriteRoundtripWorks(valueToWrite);
        }

        protected override int DataLength => 2 * sizeof(long);

        public static IEnumerable<object[]> ValuesForRoundTripTest => MakeMemberData(
            DateTimeOffset.Now,
            new DateTimeOffset(2022, 10, 15, 15, 19, 34, 123, TimeSpan.FromHours(2)),
            DateTimeOffset.UtcNow.AddHours(-5));
    }
}
