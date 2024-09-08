using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using TeaSuite.KV;

namespace ShortUrl.Controllers;

[ApiController]
[Route("api")]
public sealed class ApiController : ControllerBase
{
    private static readonly DateTime ServiceEpoch = new(
        2023, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc);
    private static ulong seqId = 0;
    private readonly IKeyValueStore<ulong, string> store;

    public ApiController(IKeyValueStore<ulong, string> store)
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

    [HttpGet("all")]
    public IActionResult GetAll(
        [FromServices] ILogger<ApiController> logger,
        [FromQuery] int size = 100,
        [FromQuery] string? from = null,
        [FromQuery] string? to = null
        )
    {
        IEnumerator<KeyValuePair<ulong, string>> enumerator;

        if (from != null || to != null)
        {
            ulong? fromId = from != null ? Codec.Decode(from) : null;
            ulong? toId = to != null ? Codec.Decode(to) : null;
            Range<ulong> range = new Range<ulong>
            {
                HasStart = fromId.HasValue,
                Start = fromId.HasValue ? fromId.Value : 0,
                HasEnd = toId.HasValue,
                End = toId.HasValue ? toId.Value : 0,
            };

            logger.LogDebug(
                "From: '{fromStr}' ({from}), To: '{toStr}' ({to}), Range: {range}.",
                from, fromId, to, toId, range);

            enumerator = store.GetEnumerator(range);
        }
        else
        {
            enumerator = store.GetEnumerator();
        }

        // Ask for one more item than necessary to see what the next item's key would be.
        List<KeyValuePair<string, string>> results = ConvertUint64ToString(enumerator)
            .Take(size + 1).ToList();
        string? token = results.Count == size + 1 ? results[results.Count - 1].Key : null;

        return Ok(new
        {
            data = new Dictionary<string, string>(results.Take(size)),
            continuation_token = token,
        });
    }

    private static ulong GenerateId()
    {
        TimeSpan epochTime = DateTime.UtcNow.Subtract(ServiceEpoch);
        ulong seq = Interlocked.Increment(ref seqId) % 1000;
        ulong id = Convert.ToUInt64(epochTime.TotalMilliseconds) * 1000 + seq;

        return id;
    }

    private static IEnumerable<KeyValuePair<string, string>> ConvertUint64ToString(
        IEnumerator<KeyValuePair<ulong, string>> enumerator)
    {
        using (enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return new(
                    Codec.Encode(enumerator.Current.Key),
                    enumerator.Current.Value
                );
            }
        }
    }
}
