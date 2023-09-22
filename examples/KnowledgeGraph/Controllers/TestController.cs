using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using TeaSuite.KV;

// using HeadRelationOrderKey = TeaSuite.KV.KeyTriplet<ulong, uint, long>;
// using HeadTailOrderKey = TeaSuite.KV.KeyTriplet<ulong, ulong, long>;
using HeadRelationOrderKey = TeaSuite.KV.KeyTuple<ulong, TeaSuite.KV.KeyTuple<uint, long>>;
using HeadTailOrderKey = TeaSuite.KV.KeyTuple<ulong, TeaSuite.KV.KeyTuple<ulong, long>>;

namespace KnowledgeGraph.Controllers;

[ApiController]
[Route("test")]
public sealed class TestController : ControllerBase
{
    // private readonly IKeyValueStore<TeaSuite.KV.KeyTriplet<ulong, uint, long>, ulong> hr2t;
    // private readonly IKeyValueStore<TeaSuite.KV.KeyTriplet<ulong, ulong, long>, ulong> ht2r;
    private readonly IReadOnlyKeyValueStore<HeadRelationOrderKey, ulong> hr2t;
    private readonly IReadOnlyKeyValueStore<HeadTailOrderKey, ulong> ht2r;

    public TestController(IReadOnlyKeyValueStore<HeadRelationOrderKey, ulong> hr2t, IReadOnlyKeyValueStore<HeadTailOrderKey, ulong> ht2r)
    {
        this.hr2t = hr2t;
        this.ht2r = ht2r;
    }

    [HttpGet("populate")]
    public IActionResult Populate()
    {
        Tuple<ulong, uint, ulong> hrt = new Tuple<ulong, uint, ulong>(1, 1, 2);

        Set(new Tuple<ulong, uint, ulong>(1, 1, 2), 0);
        Set(new Tuple<ulong, uint, ulong>(2, 2, 1), 0);

        Set(new Tuple<ulong, uint, ulong>(2, 3, 3), 1);
        Set(new Tuple<ulong, uint, ulong>(2, 3, 4), 2);

        hr2t.Close();
        ht2r.Close();

        return Ok();
    }

    [HttpGet("{head}/{relation}")]
    public IActionResult GetHeadRelation(ulong head, uint relation)
    {
        Range<HeadRelationOrderKey> range = new RangeBuilder<HeadRelationOrderKey>()
            .WithStart(new HeadRelationOrderKey()
            {
                Major = head,
                Minor = new KeyTuple<uint, long>()
                {
                    Major = relation,
                    Minor = Int64.MinValue,
                },
            })
            .WithEnd(new HeadRelationOrderKey()
            {
                Major = head + 1,
                Minor = new KeyTuple<uint, long>()
                {
                    Major = UInt32.MinValue,
                    Minor = Int64.MinValue,
                },
            })
            .Build();

        // hr2t
        return Ok(new
        {
            timestamp = DateTimeOffset.Now,
            data = ToEnumerable(hr2t.GetEnumerator(range)),
        });
        // hr2t.
    }

    private static IEnumerable<T> ToEnumerable<T>(IEnumerator<T> enumerator)
    {
        using (enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
    }

    private void Set(Tuple<ulong, uint, ulong> hrt, long order)
    {
        // hr2t.Set(MakeHeadRelationOrderKey(hrt, order), hrt.Item3);
        // ht2r.Set(MakeHeadTailOrderKey(hrt, order), hrt.Item3);
    }

    private static HeadRelationOrderKey MakeHeadRelationOrderKey(Tuple<ulong, uint, ulong> hrt, long order)
    {
        return new HeadRelationOrderKey()
        {
            Major = hrt.Item1,
            Minor = new KeyTuple<uint, long>()
            {
                Major = hrt.Item2,
                Minor = order,
            },
        };
    }

    private static HeadTailOrderKey MakeHeadTailOrderKey(Tuple<ulong, uint, ulong> hrt, long order)
    {
        return new HeadTailOrderKey()
        {
            Major = hrt.Item1,
            Minor = new KeyTuple<ulong, long>()
            {
                Major = hrt.Item3,
                Minor = order,
            },
        };
    }
}
