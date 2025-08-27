using System.Globalization;
using Contracts;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<StartRunConsumer>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["Rabbit:Host"] ?? "rabbitmq", "/", h => { });
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();
app.Run();

class StartRunConsumer : IConsumer<StartRunCommand>
{
    public async Task Consume(ConsumeContext<StartRunCommand> ctx)
    {
        var path = ctx.Message.DatasetPath;
        var lines = await File.ReadAllLinesAsync(path);
        var data = new List<SalesRecord>();
        for (int i=1;i<lines.Length;i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            try
            {
                var rec = new SalesRecord(
                    DateOnly.ParseExact(parts[0].Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture),
                    TimeOnly.Parse(parts[1].Trim()),
                    parts[2].Trim(),
                    parts[3].Trim(),
                    parts[4].Trim(),
                    decimal.Parse(parts[5].Trim(), CultureInfo.InvariantCulture),
                    int.Parse(parts[6].Trim()),
                    parts[7].Trim()
                );
                data.Add(rec);
            }
            catch { /* skip bad rows */ }
        }

        // Demand & velocity
        var demand = data.GroupBy(r => r.Product)
            .Select(g => new SkuDemand(
                g.Key, // SkuId = Product
                g.Sum(x => x.Qty),
                g.Count(),
                g.Sum(x => (double)x.Qty) / Math.Max(1, (DateOnly.FromDateTime(DateTime.Today) - g.Min(x=>x.OrderDate)).DayNumber),
                IsSeasonal(g.Select(x=>x.OrderDate))
            )).ToList();

        // Order lines with synthetic order id (customer + date)
        var linesOut = data.Select(r => new OrderLine($"{r.CustomerId}_{r.OrderDate:yyyyMMdd}", r.Product, r.Qty)).ToList();

        await ctx.Publish(new SalesContextReady(ctx.Message.RunId, ctx.Message.Mode, demand, linesOut));
    }

    private bool IsSeasonal(IEnumerable<DateOnly> dates)
    {
        var byMonth = dates.GroupBy(d => d.Month).Select(g => g.Count()).ToList();
        if (byMonth.Count==0) return false;
        var avg = byMonth.Average();
        return byMonth.Any(c => c > 1.5 * avg);
    }
}
