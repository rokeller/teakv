using Microsoft.Extensions.DependencyInjection.Extensions;
using TeaSuite.KV;
using TeaSuite.KV.IO.Formatters;

// using HeadRelationOrderKey = TeaSuite.KV.KeyTriplet<ulong, uint, long>;
// using HeadTailOrderKey = TeaSuite.KV.KeyTriplet<ulong, ulong, long>;
using HeadRelationOrderKey = TeaSuite.KV.KeyTuple<ulong, TeaSuite.KV.KeyTuple<uint, long>>;
using HeadTailOrderKey = TeaSuite.KV.KeyTuple<ulong, TeaSuite.KV.KeyTuple<ulong, long>>;

var builder = WebApplication.CreateBuilder(args);

const string SegmentsDirPath = "../kv/knowledge-graph";

builder.Services
    // Use a read-only store for range queries.
    // .AddReadOnlyKeyValueStore<ulong, string>()
    // .AddMemoryMappedFileStorage((options) => options.SegmentsDirectoryPath = SegmentsDirPath)
    // .Services

    // Mapping head node ID (major) / relation ID (major of minor) / order (minor of minor) to tail node ID.
    .AddKeyValueStore<HeadRelationOrderKey, ulong>()
    .AddFileStorage((options) =>
    {
        // Console.WriteLine("Setting options for HeadRelationOrder (hr2t)");
        options.SegmentsDirectoryPath = SegmentsDirPath + "/hr2t";
    })
    .Services

    .AddReadOnlyKeyValueStore<HeadRelationOrderKey, ulong>()
    .AddMemoryMappedFileStorage((options) => options.SegmentsDirectoryPath = SegmentsDirPath + "/hr2t")
    .Services

    // Mapping head node ID (major) / tail node ID (major of minor) / order (minor of minor) to relation ID.
    .AddKeyValueStore<HeadTailOrderKey, ulong>()
    .AddFileStorage((options) =>
    {
        // Console.WriteLine("Setting options for HeadTailOrder (ht2r)");
        options.SegmentsDirectoryPath = SegmentsDirPath + "/ht2r";
    })
    .Services

    .AddReadOnlyKeyValueStore<HeadTailOrderKey, ulong>()
    .AddMemoryMappedFileStorage((options) => options.SegmentsDirectoryPath = SegmentsDirPath + "/ht2r")
    .Services

    // .AddTransient<IFormatter<HeadRelationOrderKey>>(
    //     services => services.GetRequiredService<IKeyTripletFormatter<ulong, uint, long>>())
    // .AddTransient<IFormatter<HeadTailOrderKey>>(
    //     services => services.GetRequiredService<IKeyTripletFormatter<ulong, ulong, long>>())
    .AddKeyTupleFormatter<ulong, KeyTuple<uint, long>>()
    .AddKeyTupleFormatter<ulong, KeyTuple<ulong, long>>()
    .AddKeyTupleFormatter<uint, long>()
    .AddKeyTupleFormatter<ulong, long>()
    // .AddTransient<IFormatter<HeadRelationOrderKey>>(
    //     services => services.GetRequiredService<IKeyTupleFormatter<ulong, KeyTuple<uint, long>>>())
    // .AddTransient<IFormatter<KeyTuple<uint, long>>>(
    //     services => services.GetRequiredService<IKeyTupleFormatter<uint, long>>())
    // .AddTransient<IFormatter<HeadTailOrderKey>>(
    //     services => services.GetRequiredService<IKeyTupleFormatter<ulong, KeyTuple<ulong, long>>>())
    // .AddTransient<IFormatter<KeyTuple<ulong, long>>>(
    //     services => services.GetRequiredService<IKeyTupleFormatter<ulong, long>>())

    .AddControllers()
    ;



var app = builder.Build();

app.MapControllers();
app.Run();
