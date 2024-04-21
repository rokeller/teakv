using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace PeopleStore;

partial class PeopleCommand
{
    private sealed class DeleteCommand : IPeopleCommand
    {
        private readonly PersonCommandSettings settings;
        private readonly PeopleRepository repo;

        public DeleteCommand(
            IOptions<PersonCommandSettings> settings,
            PeopleRepository repo
        )
        {
            this.settings = settings.Value;

            if (String.IsNullOrWhiteSpace(this.settings.ID))
            {
                throw new ArgumentException("The ID parameter must be passed.",
                    nameof(PersonCommandSettings.ID));
            }

            this.repo = repo;
        }

        public Task RunAsync()
        {
            Debug.Assert(null != settings.ID, "The ID of the person to delete must not be null.");

            repo.Delete(settings.ID);

            return Task.CompletedTask;
        }
    }
}
