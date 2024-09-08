using TeaSuite.KV;
using TeaSuite.KV.Policies;

var builder = WebApplication.CreateBuilder(args);

const string SegmentsDirPath = "../kv/shorturl";

builder.Services
    .AddControllers().Services

    .AddKeyValueStore<ulong, string>()
    .AddMemoryMappedFileStorage((options) => options.SegmentsDirectoryPath = SegmentsDirPath)
    .AddWriteAheadLog((settings) =>
    {
        settings.LogDirectoryPath = SegmentsDirPath + ".wal";
    }).Services
    .AddTransient<ILockingPolicy, ReaderWriterLockingPolicy>()
    ;

var app = builder.Build();

app.MapControllers();
app.Run();
