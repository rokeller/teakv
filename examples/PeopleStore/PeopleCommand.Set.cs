using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace PeopleStore;

partial class PeopleCommand
{
    private sealed class SetCommand : IPeopleCommand
    {
        private readonly PersonCommandSettings settings;
        private readonly PeopleRepository repo;

        public SetCommand(
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
            else if (String.IsNullOrWhiteSpace(this.settings.FirstName))
            {
                throw new ArgumentException("The FirstName parameter must be passed.",
                    nameof(PersonCommandSettings.FirstName));
            }
            else if (String.IsNullOrWhiteSpace(this.settings.LastName))
            {
                throw new ArgumentException("The LastName parameter must be passed.",
                    nameof(PersonCommandSettings.LastName));
            }
            else if (!this.settings.Age.HasValue)
            {
                throw new ArgumentException("The Age parameter must be passed.",
                    nameof(PersonCommandSettings.Age));
            }

            this.repo = repo;
        }

        public Task RunAsync()
        {
            Debug.Assert(null != settings.ID,
                "The ID of the person to set must not be null.");
            Debug.Assert(null != settings.FirstName,
                "The FirstName of the person to set must not be null.");
            Debug.Assert(null != settings.LastName,
                "The LastName of the person to set must not be null.");
            Debug.Assert(settings.Age.HasValue,
                "The Age of the person to set must not be null.");

            Person person = new(
                settings.FirstName,
                settings.LastName,
                settings.Age.Value);
            repo.Write(settings.ID, person);

            Console.WriteLine("Wrote entry for person '{0}':\n\t{1}",
                settings.ID, person);

            return Task.CompletedTask;
        }
    }
}
