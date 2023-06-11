using System.Threading;
using System;
using Microsoft.AspNetCore.Mvc;
using TeaSuite.KV;

namespace ShortUrl.Controllers;

[ApiController]
[Route("api")]
public class ShortenController : ControllerBase
{
    private static readonly DateTime ServiceEpoch = new DateTime(2023, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc);
    private static ulong seqId = 0;
    private readonly IKeyValueStore<ulong, string> store;

    public ShortenController(IKeyValueStore<ulong, string> store)
    {
        this.store = store;
    }

    [HttpPost("shorten")]
    public IActionResult Shorten([FromForm] string url)
    {
        ulong id = GenerateId();
        string shortId = Codec.Encode(id);

        store.Set(id, url);

        return Ok(new { @short = shortId, url = url, });
    }

    [HttpGet("details/{shortId}")]
    public IActionResult GetDetails(string shortId)
    {
        ulong? id = Codec.Decode(shortId);
        if (!id.HasValue)
        {
            // The short ID is invalid/malformed. Pretend we just didn't find anything.
            return NotFound();
        }

        if (!store.TryGet(id.Value, out string? longUrl))
        {
            // We couldn't find a matching ID.
            return NotFound();
        }

        ulong msSinceServiceEpoch = id.Value / 1000;
        DateTime timestamp = ServiceEpoch.AddMilliseconds(msSinceServiceEpoch);

        return Ok(new
        {
            shortId,
            longUrl,
            id,
            timestamp,
        });
    }

    private static ulong GenerateId()
    {
        TimeSpan epochTime = DateTime.UtcNow.Subtract(ServiceEpoch);
        ulong seq = Interlocked.Increment(ref seqId) % 1000;
        ulong id = Convert.ToUInt64(epochTime.TotalMilliseconds) * 1000 + seq;

        return id;
    }
}
