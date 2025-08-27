using Contracts;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<RackLayoutConsumer>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["Rabbit:Host"] ?? "rabbitmq", "/", h => { });
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();
app.Run();

class RackLayoutConsumer : IConsumer<RackLayoutCalculated>
{
    public async Task Consume(ConsumeContext<RackLayoutCalculated> ctx)
    {
        var mode = ctx.Message.Mode; // PTO or PTL
        var batches = new List<Batch>();

        if (mode == "PTL")
        {
            // group lines by SKU
            var bySku = ctx.Message.Lines.GroupBy(l => l.SkuId).OrderByDescending(g => g.Sum(x=>x.Qty));
            int station = 1;
            foreach (var g in bySku)
            {
                var lines = g.ToList();
                var batchId = Guid.NewGuid().ToString("N");
                batches.Add(new Batch(batchId, station.ToString(), mode, lines));
                station = station % 5 + 1;
            }
        }
        else
        {
            // PTO: group by OrderId up to 10 orders per batch
            var orders = ctx.Message.Lines.GroupBy(l => l.OrderId).ToList();
            int i=0; int batchNum=1; var current = new List<OrderLine>();
            foreach (var o in orders)
            {
                current.AddRange(o);
                i++;
                if (i>=10)
                {
                    batches.Add(new Batch($"B{batchNum++}", ((batchNum-1)%5+1).ToString(), mode, new List<OrderLine>(current)));
                    current.Clear(); i=0;
                }
            }
            if (current.Count>0)
                batches.Add(new Batch($"B{batchNum++}", ((batchNum-1)%5+1).ToString(), mode, current));
        }

        await ctx.Publish(new BatchesCreated(ctx.Message.RunId, mode, batches, ctx.Message.Locations));
    }
}
