using System.Diagnostics;
using System;
using Microsoft.AspNetCore.Mvc;
using TeaSuite.KV;

namespace ShortUrl.Controllers;

[ApiController]
[Route("/")]
public class RedirController : ControllerBase
{
    private readonly ILogger<RedirController> logger;
    private readonly IKeyValueStore<ulong, string> store;

    public RedirController(ILogger<RedirController> logger, IKeyValueStore<ulong, string> store)
    {
        this.logger = logger;
        this.store = store;
    }

    [HttpGet("{shortId}")]
    public IActionResult LookupAndRedirect(string shortId)
    {
        ulong? id = Codec.Decode(shortId);
        if (!id.HasValue)
        {
            // The short ID is invalid/malformed. Pretend we just didn't find anything.
            return NotFound();
        }

        logger.LogTrace("Decoded short ID '{shortId}' to {id}.", shortId, id);
        Stopwatch watch = Stopwatch.StartNew();
        if (!store.TryGet(id.Value, out string? longUrl))
        {
            watch.Stop();
            // We couldn't find a matching ID.
            return NotFound();
        }

        watch.Stop();
        logger.LogTrace("Lookup took {timeMs}ms.", watch.ElapsedMilliseconds);

        return Redirect(longUrl!);
    }
}
