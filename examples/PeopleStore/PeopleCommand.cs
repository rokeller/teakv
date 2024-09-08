using Microsoft.Extensions.DependencyInjection;

namespace PeopleStore;

internal static partial class PeopleCommand
{
    internal static IPeopleCommand CreateGetCommand(IServiceProvider services)
    {
        return CreateCommand<GetCommand>(services);
    }

    internal static IPeopleCommand CreateSetCommand(IServiceProvider services)
    {
        return CreateCommand<SetCommand>(services);
    }

    internal static IPeopleCommand CreateDeleteCommand(IServiceProvider services)
    {
        return CreateCommand<DeleteCommand>(services);
    }

    internal static IPeopleCommand CreateListCommand(IServiceProvider services)
    {
        return CreateCommand<ListCommand>(services);
    }

    internal static IPeopleCommand CreateCompactCommand(IServiceProvider services)
    {
        return CreateCommand<CompactCommand>(services);
    }

    private static IPeopleCommand CreateCommand<TCommand>(IServiceProvider services)
        where TCommand : IPeopleCommand
    {
        try
        {
            return ActivatorUtilities.CreateInstance<TCommand>(services);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine("Missing argument:\n\t{0} - {1}",
                ex.ParamName, ex.Message);
            return NopCommand.Default;
        }
    }

    private sealed class NopCommand : IPeopleCommand
    {
        internal static readonly NopCommand Default = new();

        public Task RunAsync()
        {
            return Task.CompletedTask;
        }
    }
}
