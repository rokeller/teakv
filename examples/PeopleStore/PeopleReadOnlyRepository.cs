using TeaSuite.KV;

namespace PeopleStore;

internal sealed class PeopleReadOnlyRepository
{
    private readonly IReadOnlyKeyValueStore<string, Person> store;

    public PeopleReadOnlyRepository(IReadOnlyKeyValueStore<string, Person> store)
    {
        this.store = store;
    }

    public IEnumerator<KeyValuePair<string, Person>> GetAll()
    {
        return store.GetEnumerator();
    }
}
