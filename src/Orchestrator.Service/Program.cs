using Contracts;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

var runStatus = new Dictionary<Guid, string>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<HitRateConsumer>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["Rabbit:Host"] ?? "rabbitmq", "/", h => { });
        cfg.ConfigureEndpoints(ctx);
    });
});
builder.Services.AddSingleton(runStatus);

var app = builder.Build();

app.MapPost("/runs", async (IPublishEndpoint bus, StartRequest req, Dictionary<Guid,string> status) =>
{
    var runId = Guid.NewGuid();
    status[runId] = "Started";
    await bus.Publish(new StartRunCommand(runId, req.DatasetPath, req.Mode));
    return Results.Accepted($"/runs/{runId}", new { runId, status = "Started" });
});

app.MapGet("/runs/{id:guid}", (Guid id, Dictionary<Guid,string> status) =>
{
    if (status.TryGetValue(id, out var s))
        return Results.Ok(new { runId = id, status = s });
    return Results.NotFound();
});

app.Run();

record StartRequest(string DatasetPath, string Mode);

class HitRateConsumer : IConsumer<HitRateCalculated>
{
    private readonly Dictionary<Guid,string> _status;
    public HitRateConsumer(Dictionary<Guid,string> status) => _status = status;
    public Task Consume(ConsumeContext<HitRateCalculated> ctx)
    {
        _status[ctx.Message.RunId] = "Completed";
        return Task.CompletedTask;
    }
}
