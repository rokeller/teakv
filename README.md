# TeaSuite Key-Value Store

A simple in-process / embedded Key-Value store for .Net. Writes and deletes are
first made in-memory (so they are very fast). The data stored in-memory can
periodically be flushed to segments on disk. Segments are stored in sorted order
of the keys. Once a segment has been written, it will never change, but it can
get deleted _after_ having been merged with other segments into a new segment.
Each segment consist of a data file and an index file.

When data is not found in-memory, the segments are searched by starting with the
most recent segment first. As a result, the more segments are accumulated, the
more segments need to be searched for entries that do not exist. Therefore, the
segments can be merged (aka compacted) so that reads in segments can be made
faster.

## Usage Examples

### `int`/`string` Key-Value Store on local disk

The Key-Value store can easily be configured through dependency injection. For
instance, the following example registers a Key-Value store for integer/string
key-value pairs:

```csharp
services
    .AddKeyValueStore<int, string>()
    .AddStoreSettings((options) =>
    {
        // When writing segments to disk, index after at most 512 bytes or 10 entries, whichever comes first. Use this
        // to speed up reads from segments at the expense of larger index files.
        options.IndexPolicy = new DefaultIndexPolicy(512, 10);
        // Persist after 2000 writes/deletes or if at least 1 minute has passed since last write/delete.
        options.PersistPolicy = new DefaultPersistPolicy(2000, TimeSpan.FromMinutes(1));
        // Merge segments when there's at least 3 of them around.
        options.MergePolicy = new DefaultMergePolicy(3);
    })
    // Use file-based segments with settings from the configuration:
    .AddFileStorage(context.Configuration.GetSection("TeaSuite:KV:Int32ToString:Location"));
```

With the following JSON configuration, the data would be persisted to segments in the directory `my-stores/int-string/`.

```JSON
{
    "TeaSuite": {
        "KV": {
            "Int32ToString": {
                "Location": {
                    "SegmentsDirectoryPath": "my-stores/int-string/"
                }
            }
        }
    }

```

### Using Custom/Complex Types

All that is necessary for custom types of any complexity is to implement and registeryour own `IFormatter<T>` for that
type. Let's assume you have records of people as follows:

```csharp
readonly record struct Person
{
    public Person(string firstName, string lastName, int age)
    {
        FirstName = firstName;
        LastName = lastName;
        Age = age;
    }

    public string FirstName { get; init; }
    public string LastName { get; init; }
    public int Age { get; init; }

    public override string ToString()
    {
        return $"{FirstName} {LastName}, age {Age}";
    }
}
```

And you build a `IFormatter<Person>` as follows, relying on the existing formatters for `string` and `int`. Of course,
you could also use any other implementation including but not limited to Google Protocol Buffers, and even `JSON`. The
important thing to remember is that whatever encoding/formatting is used, you must be able to read only the exact number
of bytes from the stream when deserializing. Reading beyond will cause corruption for records that follow.

```csharp
readonly struct PersonFormatter : IFormatter<Person>
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
```

Now you register the Key-Value store for `Guid` keys and `Person` values as follows to persist values in the `people`
directory:

```csharp
services
    .AddFormatter<Person, PersonFormatter>()
    .AddKeyValueStore<Guid, Person>()
    .AddFileStorage((options) => options.SegmentsDirectoryPath = "people");
```

When you want to consume the store now, inject an instance of `IKeyValueStore<Guid, Person>`  in your code and use it
as follows:

```csharp
using TeaSuite.KV;

namespace PeopleStore;

sealed class PeopleDirectory
{
    private readonly IKeyValueStore<Guid, Person> store;

    public PeopleDirectory(IKeyValueStore<Guid, Person> store)
    {
        this.store = store;
    }

    public Person? Get(Guid key)
    {
        if (store.TryGet(key, out Person person))
        {
            return person;
        }

        return default;
    }

    public void Write(Guid key, Person person)
    {
        store.Set(key, person);
    }

    public void Delete(Guid key)
    {
        store.Delete(key);
    }
}
```

## More Examples

This repository contains more [examples](examples):

1. A simple [URL shortening service](examples/ShortUrl/).
2. A command line [tool to manage a people directory](examples/PeopleStore/).
