using Contracts;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<StationsAllocatedConsumer>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["Rabbit:Host"] ?? "rabbitmq", "/", h => { });
        cfg.ConfigureEndpoints(ctx);
    });
});
builder.Services.AddSingleton(new Dictionary<Guid, HitRateResult>());

var app = builder.Build();

app.MapGet("/results/{id:guid}", (Guid id, Dictionary<Guid,HitRateResult> store) =>
{
    return store.TryGetValue(id, out var res) ? Results.Ok(res) : Results.NotFound();
});

app.Run();

class StationsAllocatedConsumer : IConsumer<StationsAllocated>
{
    private readonly Dictionary<Guid, HitRateResult> _store;
    public StationsAllocatedConsumer(Dictionary<Guid, HitRateResult> store) => _store = store;

    public Task Consume(ConsumeContext<StationsAllocated> ctx)
    {
        // Build rack presentations from batches + locations
        var skuToRack = ctx.Message.Locations.ToDictionary(l => l.SkuId, l => l.RackId);
        var rackPresentations = new Dictionary<string, (int items, int presentations)>();

        foreach (var batch in ctx.Message.Batches)
        {
            var racksInBatch = new HashSet<string>();
            var itemsByRack = new Dictionary<string,int>();

            foreach (var line in batch.Lines)
            {
                if (!skuToRack.TryGetValue(line.SkuId, out var rackId)) continue;
                racksInBatch.Add(rackId);
                itemsByRack[rackId] = itemsByRack.GetValueOrDefault(rackId) + line.Qty;
            }

            foreach (var rackId in racksInBatch)
            {
                var items = itemsByRack.GetValueOrDefault(rackId);
                var current = rackPresentations.GetValueOrDefault(rackId, (0,0));
                current.items += items;
                current.presentations += 1;
                rackPresentations[rackId] = current;
            }
        }

        int totalPresentations = rackPresentations.Values.Sum(v => v.presentations);
        int totalItems = rackPresentations.Values.Sum(v => v.items);

        double hitRatePto = rackPresentations.Count > 0
            ? rackPresentations.Values.Average(v => v.presentations==0 ? 0 : (double)v.items / v.presentations)
            : 0;

        // Approximate PTL: sum over batches of (units / 5 capacity) / racks touched
        double totalBatchEff = ctx.Message.Batches.Sum(b => (double)b.Lines.Sum(l=>l.Qty) / 5.0);
        double hitRatePtl = rackPresentations.Count>0 ? totalBatchEff / rackPresentations.Count : 0;

        // Use batch mode from first batch (they're homogeneous per run)
        var mode = ctx.Message.Batches.FirstOrDefault()?.Mode ?? "PTO";
        double hitRate = mode=="PTL" ? hitRatePtl : hitRatePto;

        var byRack = rackPresentations.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.presentations==0 ? 0.0 : (double)kvp.Value.items/kvp.Value.presentations);

        var result = new HitRateResult(mode, hitRate, totalItems, totalPresentations, byRack);
        _store[ctx.Message.RunId] = result;

        return ctx.Publish(new HitRateCalculated(ctx.Message.RunId, result));
    }
}
