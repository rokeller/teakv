using TeaSuite.KV;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers().Services

    .AddKeyValueStore<ulong, string>()
    .AddFileStorage((options) => options.SegmentsDirectoryPath = "../kv/shorturl")
    ;

var app = builder.Build();

app.MapControllers();
app.Run();
