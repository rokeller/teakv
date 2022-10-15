namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormattersTests
{
    public abstract class FormatterTestsBase<T>
    {
        private readonly IFormatter<T> formatter;

        protected FormatterTestsBase(IFormatter<T> formatter)
        {
            this.formatter = formatter;
        }

        public async virtual Task ReadWriteRoundtripWorks(T valueToWrite)
        {
            using MemoryStream memstr = new MemoryStream();

            await formatter.WriteAsync(valueToWrite, memstr, default);

            memstr.Position = 0;
            T readValue = await formatter.ReadAsync(memstr, default);

            Assert.Equal(valueToWrite, readValue);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(123)]
        [InlineData(45)]
        [InlineData(255)]

        public async Task SkipReadAsyncWorks(byte sentinel)
        {
            using MemoryStream memstr = new MemoryStream();

            StreamUtils.WriteRandom(DataLength, memstr);
            memstr.WriteByte(sentinel);

            memstr.Position = 0;
            await formatter.SkipReadAsync(memstr, default);
            Assert.Equal(sentinel, memstr.ReadByte());

            using Stream nonSeekable = StreamUtils.WrapNonSeekable(memstr);
            nonSeekable.Position = 0;
            await formatter.SkipReadAsync(nonSeekable, default);
            Assert.Equal(sentinel, nonSeekable.ReadByte());
        }

        protected abstract int DataLength { get; }

        public static IEnumerable<object[]> MakeMemberData(params T[] values)
        {
            return MakeMemberData(values.AsEnumerable());
        }

        public static IEnumerable<object[]> MakeMemberData(IEnumerable<T> values)
        {
            foreach (T value in values)
            {
                yield return new object[] { value! };
            }
        }
    }
}
