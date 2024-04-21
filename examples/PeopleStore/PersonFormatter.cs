using TeaSuite.KV.IO.Formatters;

namespace PeopleStore;

internal readonly struct PersonFormatter : IFormatter<Person>
{
    private readonly IFormatter<string> stringFormatter;
    private readonly IFormatter<int> intFormatter;

    public PersonFormatter(IFormatter<string> stringFormatter, IFormatter<int> intFormatter)
    {
        this.stringFormatter = stringFormatter;
        this.intFormatter = intFormatter;
    }

    public async ValueTask<Person> ReadAsync(Stream source, CancellationToken cancellationToken)
    {
        string firstName = await stringFormatter.ReadAsync(source, cancellationToken);
        string lastName = await stringFormatter.ReadAsync(source, cancellationToken);
        int age = await intFormatter.ReadAsync(source, cancellationToken);

        return new Person()
        {
            FirstName = firstName,
            LastName = lastName,
            Age = age,
        };
    }

    public async ValueTask SkipReadAsync(Stream source, CancellationToken cancellationToken)
    {
        await stringFormatter.SkipReadAsync(source, cancellationToken);
        await stringFormatter.SkipReadAsync(source, cancellationToken);
        await intFormatter.SkipReadAsync(source, cancellationToken);
    }

    public async ValueTask WriteAsync(Person value, Stream destination, CancellationToken cancellationToken)
    {
        await stringFormatter.WriteAsync(value.FirstName, destination, cancellationToken);
        await stringFormatter.WriteAsync(value.LastName, destination, cancellationToken);
        await intFormatter.WriteAsync(value.Age, destination, cancellationToken);
    }
}
