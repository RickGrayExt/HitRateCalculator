using Contracts;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<BatchesCreatedConsumer>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["Rabbit:Host"] ?? "rabbitmq", "/", h => { });
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();
app.Run();

class BatchesCreatedConsumer : IConsumer<BatchesCreated>
{
    public async Task Consume(ConsumeContext<BatchesCreated> ctx)
    {
        // allocate batches to stations (already have a StationId; just group them)
        var assignments = ctx.Message.Batches
            .GroupBy(b => b.StationId)
            .Select(g => new StationAssignment(g.Key, g.Select(b=>b.BatchId).ToList()))
            .ToList();

        await ctx.Publish(new StationsAllocated(ctx.Message.RunId, assignments, ctx.Message.Batches, ctx.Message.Locations));
    }
}
