using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace PeopleStore;

partial class PeopleCommand
{
    private sealed class GetCommand : IPeopleCommand
    {
        private readonly PersonCommandSettings settings;
        private readonly PeopleRepository repo;

        public GetCommand(
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
            Debug.Assert(null != settings.ID, "The ID of the person to get must not be null.");

            Person? person = repo.Get(settings.ID);
            if (person.HasValue)
            {
                Console.WriteLine("Found person '{0}':\n\t{1}", settings.ID, person);
            }
            else
            {
                Console.WriteLine("Could not find person '{0}'.", settings.ID);
            }

            return Task.CompletedTask;
        }
    }
}
