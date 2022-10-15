using static TeaSuite.KV.IO.Formatters.PrimitiveFormatters;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormattersTests
{
    public sealed class DateTimeFormatterTests : FormatterTestsBase<DateTime>
    {
        public DateTimeFormatterTests() : base(new DateTimeFormatter()) { }

        [Theory]
        [MemberData(nameof(ValuesForRoundTripTest))]
        public override Task ReadWriteRoundtripWorks(DateTime valueToWrite)
        {
            return base.ReadWriteRoundtripWorks(valueToWrite);
        }

        protected override int DataLength => sizeof(long);

        public static IEnumerable<object[]> ValuesForRoundTripTest => MakeMemberData(
            DateTime.UtcNow,
            new DateTime(2022, 10, 15, 15, 19, 34, DateTimeKind.Utc),
            DateTime.UtcNow.AddHours(-5));
    }
}
