using Contracts;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SkuGroupsConsumer>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["Rabbit:Host"] ?? "rabbitmq", "/", h => { });
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();
app.Run();

class SkuGroupsConsumer : IConsumer<SkuGroupsCreated>
{
    public async Task Consume(ConsumeContext<SkuGroupsCreated> ctx)
    {
        var demandBySku = ctx.Message.Demand.ToDictionary(d=>d.SkuId, d=>d);
        var ordered = ctx.Message.Demand.OrderByDescending(d => d.Velocity).ToList();
        var locations = new List<ShelfLocation>();
        for (int i=0;i<ordered.Count;i++)
        {
            var rank = i+1;
            string zone = i < ordered.Count*0.2 ? "FRONT" : i < ordered.Count*0.6 ? "MID" : "BACK";
            string rackId = $"{(i/12)+1}";
            string slotId = $"{(i%12)+1}";
            locations.Add(new ShelfLocation(ordered[i].SkuId, rackId, $"{zone}_S{slotId}", rank));
        }
        await ctx.Publish(new ShelfLocationsAssigned(ctx.Message.RunId, ctx.Message.Mode, locations, ctx.Message.Demand, ctx.Message.Lines));
    }
}
