using TeaSuite.KV;

var builder = WebApplication.CreateBuilder(args);

const string SegmentsDirPath = "../kv/shorturl";

builder.Services
    .AddControllers().Services

    // Use a read-only store for range queries.
    .AddReadOnlyKeyValueStore<ulong, string>()
    .AddMemoryMappedFileStorage((options) => options.SegmentsDirectoryPath = SegmentsDirPath)
    .Services

    .AddKeyValueStore<ulong, string>()
    .AddFileStorage((options) => options.SegmentsDirectoryPath = SegmentsDirPath)
    ;

var app = builder.Build();

app.MapControllers();
app.Run();
