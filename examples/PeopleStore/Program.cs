using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TeaSuite.KV;

namespace PeopleStore;

internal class Program
{
    private static readonly HashSet<string> ValidCommands = new(
        ["get", "set", "delete", "list", "compact",],
        StringComparer.OrdinalIgnoreCase
    );

    private static void Main(string[] args)
    {
        IHostBuilder? builder = CreateHost(args);
        if (null == builder)
        {
            return;
        }

        IHost host = builder
            .ConfigureServices(ConfigureServices)
            .Build();

        host.Run();
    }

    private static void ConfigureServices(
        HostBuilderContext context,
        IServiceCollection services)
    {
        services
            .AddHostedService<PeopleService>()
            .AddSingleton<PeopleRepository>()
            .AddSingleton<PeopleReadOnlyRepository>()
            ;

        services
            .AddFormatter<Person, PersonFormatter>()

            .AddReadOnlyKeyValueStore<string, Person>()
            .AddFileStorage((options) => options.SegmentsDirectoryPath = "people")
            .Services

            .AddKeyValueStore<string, Person>()
            .AddFileStorage((options) => options.SegmentsDirectoryPath = "people")
            .AddWriteAheadLog((options) => { })
            ;
    }

    private static IHostBuilder? CreateHost(string[] args)
    {
        string commandName = String.Empty;
        if (args.Length >= 1)
        {
            commandName = args[0];
        }

        if (!ValidCommands.Contains(commandName))
        {
            Console.Error.WriteLine(
                "Missing command: valid commands are 'get', 'set', 'delete', 'list', 'compact'.");
            return null;
        }

        Func<IServiceProvider, IPeopleCommand> commandFactory =
            CreateCommandFactory(commandName);

        return Host
            .CreateDefaultBuilder(args)
            .ConfigureServices(
                (context, services) => services
                    .Configure<PersonCommandSettings>(context.Configuration.Bind)
                    .AddSingleton(commandFactory))
            ;
    }

    private static Func<IServiceProvider, IPeopleCommand> CreateCommandFactory(
        string commandName
    )
    {
        switch (commandName.ToLower())
        {
            case "get":
                return PeopleCommand.CreateGetCommand;
            case "set":
                return PeopleCommand.CreateSetCommand;
            case "delete":
                return PeopleCommand.CreateDeleteCommand;
            case "list":
                return PeopleCommand.CreateListCommand;
            case "compact":
                return PeopleCommand.CreateCompactCommand;

            default:
                throw new ArgumentException("unexpected command", nameof(commandName));
        }
    }
}
