using Contracts;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SalesContextConsumer>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["Rabbit:Host"] ?? "rabbitmq", "/", h => { });
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();
app.Run();

class SalesContextConsumer : IConsumer<SalesContextReady>
{
    public async Task Consume(ConsumeContext<SalesContextReady> ctx)
    {
        // Simple quantile groups based on velocity
        var ordered = ctx.Message.Demand.OrderByDescending(d => d.Velocity).ToList();
        int n = Math.Max(1, ordered.Count/5);
        var groups = new List<SkuGroup>();
        for (int i=0;i<5;i++)
        {
            var slice = ordered.Skip(i*n).Take(n).Select(d=>d.SkuId).ToList();
            if (slice.Count>0) groups.Add(new SkuGroup($"G{i+1}", slice));
        }
        await ctx.Publish(new SkuGroupsCreated(ctx.Message.RunId, ctx.Message.Mode, groups, ctx.Message.Demand, ctx.Message.Lines));
    }
}
