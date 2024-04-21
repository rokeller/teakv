namespace PeopleStore;

partial class PeopleCommand
{
    private sealed class ListCommand : IPeopleCommand
    {
        private readonly PeopleReadOnlyRepository repo;

        public ListCommand(PeopleReadOnlyRepository repo)
        {
            this.repo = repo;
        }

        public Task RunAsync()
        {
            using IEnumerator<KeyValuePair<string, Person>> enumerator = repo.GetAll();
            int n = 0;

            Console.WriteLine("ID\tFirstName\tLastName\tAge");

            while (enumerator.MoveNext())
            {
                n++;

                Person p = enumerator.Current.Value;
                Console.WriteLine("{0}\t{1}\t{2}\t{3}",
                    enumerator.Current.Key, p.FirstName, p.LastName, p.Age);
            }

            if (n == 1)
            {
                Console.Error.WriteLine("1 person found.");
            }
            else
            {
                Console.Error.WriteLine("{0} people found.", n);
            }

            return Task.CompletedTask;
        }
    }
}
