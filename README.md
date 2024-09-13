# TeaSuite Key-Value Store

![GitHub Release](https://img.shields.io/github/v/release/rokeller/teakv)
![GitHub Open Issues](https://img.shields.io/github/issues/rokeller/teakv)
![GitHub Open Pull Requests](https://img.shields.io/github/issues-pr/rokeller/teakv)
![NuGet Version](https://img.shields.io/nuget/v/teasuite.kv)
![NuGet Downloads](https://img.shields.io/nuget/dt/teasuite.kv)

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

Starting with version 0.3, the Key-Value store also supports the use of a
write-ahead log (WAL) for write operations (`Set` and `Delete`), but the default
is to not use the WAL (for backward compatibility). Read more about the use of
a WAL below in [Crash Recovery](#crash-recovery).

Starting with version 0.4, the Key-Value store also supports read/write locking
for the in-memory store through the use of the `ILockingPolicy` interface. For
backward compatibility, the default is to use the `NullLockingPolicy` which does
not do any locking. By registering the `ReaderWriterLockingPolicy` instead for
the `ILockingPolicy`, you can allow many parallel reads or a single write at
any given time, so you don't need to worry about locking access to the Key-Value
store yourself. Read more about the use of a locking policy below in
[Locking](#locking).

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

With the following JSON configuration, the data would be persisted to segments
in the directory `my-stores/int-string/`.

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
}

```

### Using Custom/Complex Types

All that is necessary for custom types of any complexity is to implement and
register your own `IFormatter<T>` for that type. Let's assume you have records
of people as follows:

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

And you build a `IFormatter<Person>` as follows, relying on the existing
formatters for `string` and `int`. Of course, you could also use any other
implementation including but not limited to Google Protocol Buffers, and even
`JSON`. The important thing to remember is that whatever encoding/formatting is
used, you must be able to read only the exact number of bytes from the stream
when deserializing. Reading beyond will cause corruption for records that follow.

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

Now you register the Key-Value store for `Guid` keys and `Person` values as
follows to persist values in the `people` directory:

```csharp
services
    .AddFormatter<Person, PersonFormatter>()
    .AddKeyValueStore<Guid, Person>()
    .AddFileStorage((options) => options.SegmentsDirectoryPath = "people");
```

When you want to consume the store now, inject an instance of
`IKeyValueStore<Guid, Person>`  in your code and use it as follows:

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

### Read-Only Key-Value Store

The `IReadOnlyKeyValueStore<TKey,TValue>` is also inherited by the
`IKeyValueStore<TKey,TValue>` interface and therefore you can cast any
implementation of the key-value store to that read-only interface should that be
needed.

Should you however need a key-value store that is alwyas read-only, for example
to read an offline-copy of an infrequently changing key-value store, or
something like a read replica, you can use a dedicated implementation of
`IReadOnlyKeyValueStore<,>`, like this;

```csharp
services
    .AddReadOnlyKeyValueStore<int,string>()
    .AddMemoryMappedFileStorage(options =>
    {
        options.SegmentsDirectoryPath = "my-kv-store";
    ]);
```

You can configure this very much like a writable key-value store, with they key
difference that there is no in-memory key-value store to accept writes and thus
the persistence policy has no effect, as do the merge, index, or locking
policies. There's also no write-ahead log because there are no writes. For such
read-only key-value store instances it is typically recommended to use a
memory-mapped file segment manager, like shown above.

### Crash Recovery

By default, the Key-Value store does not recover entries written to the in-memory
store but not persisted to segments yet after crashes. That functionality can
however easily be enabled through the use of a write-ahead log that persists
each write operation (`Set` and `Delete`) to a log file on disk before the
operation is applied to the in-memory store. Should a crash occur, the next time
the Key-Value store is instantiated again, it will read the write-ahead log and
recover all previously written yet uncommitted records from that log.

To add this cash recovery, all you need to do is register the default file-based
write-ahead log for your Key-Value store, like so:

```csharp
services
    .AddKeyValueStore<ulong, string>()
    .AddWriteAheadLog((settings) =>
    {
        // Where to put the write-ahead log files
        settings.LogDirectoryPath = ".wal";
        // How big a file to reserve upfront for the write-ahead log
        settings.ReservedSize = 128 * 1024 * 1024; // 128 MiB
    });
```

Please note that the write-ahead log really only makes sense for a writable
Key-Value store. Using it does have a performance impact on the Key-Value store
at least as far as the write operations are concerned, because each of them is
first committed to the write-ahead log before committing the changes to the
in-memory store. Accordingly, if your application does not need crash recovery,
you may fare better without the write-ahead log.

### Locking

By default, they Key-Value store does not do any locking for reading and writing
to the in-memory store. Consumers are expected do to this themselves, based on
the application's needs.

This behavior can however be changed through the use of the `ILockingPolicy`
interface, by registering an implementation different from the default
`NullLockingPolicy`. The `ReaderWriterLockingPolicy` that ships with the library
for example uses a `ReaderWriterLockSlim` that allows multiple parallel reads
or a single write at any given time.

In most setups, it can be used simply by registering `ReaderWriterLockingPolicy`
as the implementation to use for `ILockingPolicy`:

```csharp
services
    .AddKeyValueStore<int, string>()
    // Configuration for the store
    ;
// Use the ReaderWriterLockingPolicy for in-memory store locking.
services.AddTransient<ILockingPolicy, ReaderWriterLockingPolicy>();
```

By default, Key-Value stores are registered as singletons, which means that even
if your process embeds multiple different Key-Value stores, with a _transient_
registration of `ReaderWriterLockingPolicy` for the `ILockingPolicy` each
Key-Value store singleton gets its own instance of the locking policy. If it was
instead registered as a _singleton_, all embedded Key-Value stores would share
the same locking policy instance, and therefore reading would be allowed in
parallel on all stores, but only a single store could ever accept a write at any
given moment.

## More Examples

This repository contains more [examples](examples):

1. A simple [URL shortening service](examples/ShortUrl/).
2. A command line [tool to manage a people directory](examples/PeopleStore/).
