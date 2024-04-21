using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PeopleStore;

internal sealed class PeopleService : BackgroundService
{
    private readonly ILogger<PeopleService> logger;
    private readonly IPeopleCommand command;
    private readonly IHostApplicationLifetime lifetime;

    public PeopleService(
        ILogger<PeopleService> logger,
        IPeopleCommand command,
        IHostApplicationLifetime lifetime)
    {
        this.logger = logger;
        this.command = command;
        this.lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(1)).ConfigureAwait(false);
        logger.LogDebug("Run command '{command}' ...", command);

        try
        {
            await command.RunAsync().ConfigureAwait(false);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine("Missing argument:\n\t{0} - {1}",
                ex.ParamName, ex.Message);
        }

        logger.LogDebug("Finished command '{command}'.", command);
        lifetime.StopApplication();
    }
}
