using static TeaSuite.KV.IO.Formatters.PrimitiveFormatters;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormattersTests
{
    public sealed class SingleFormatterTests : FormatterTestsBase<float>
    {
        public SingleFormatterTests() : base(new SingleFormatter()) { }

        [Theory]
        [MemberData(nameof(ValuesForRoundTripTest))]
        public override Task ReadWriteRoundtripWorks(float valueToWrite)
        {
            return base.ReadWriteRoundtripWorks(valueToWrite);
        }

        protected override int DataLength => sizeof(float);

        public static IEnumerable<object[]> ValuesForRoundTripTest => MakeMemberData(
            Single.MinValue,
            Single.MaxValue,
            0.0f,
            123.45f);
    }
}
