using TeaSuite.KV;

namespace PeopleStore;

internal sealed class PeopleRepository
{
    private readonly IKeyValueStore<string, Person> store;

    public PeopleRepository(IKeyValueStore<string, Person> store)
    {
        this.store = store;
    }

    public Person? Get(string key)
    {
        if (store.TryGet(key, out Person person))
        {
            return person;
        }

        return default;
    }

    public void Write(string key, Person person)
    {
        store.Set(key, person);
    }

    public void Delete(string key)
    {
        store.Delete(key);
    }

    public void Compact()
    {
        if (store is DefaultKeyValueStore<string, Person> dkvs)
        {
            dkvs.Merge();
        }
    }
}
