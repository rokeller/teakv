namespace PeopleStore;

partial class PeopleCommand
{
    private sealed class CompactCommand : IPeopleCommand
    {
        private readonly PeopleRepository repo;

        public CompactCommand(PeopleRepository repo)
        {
            this.repo = repo;
        }

        public Task RunAsync()
        {
            repo.Compact();

            return Task.CompletedTask;
        }
    }
}
