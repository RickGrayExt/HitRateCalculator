using Contracts;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ShelfLocationsConsumer>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["Rabbit:Host"] ?? "rabbitmq", "/", h => { });
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();
app.Run();

class ShelfLocationsConsumer : IConsumer<ShelfLocationsAssigned>
{
    public async Task Consume(ConsumeContext<ShelfLocationsAssigned> ctx)
    {
        var rackIds = ctx.Message.Locations.Select(l => l.RackId).Distinct().ToList();
        var racks = rackIds.Select(id => new Rack(id, LevelCount: 4, SlotPerLevel: 12, MaxWeightKg: 500)).ToList();
        await ctx.Publish(new RackLayoutCalculated(ctx.Message.RunId, ctx.Message.Mode, racks, ctx.Message.Locations, ctx.Message.Lines));
    }
}
