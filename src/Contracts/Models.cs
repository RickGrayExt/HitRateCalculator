namespace Contracts;

public record SalesRecord(DateOnly OrderDate, TimeOnly Time, string CustomerId, string ProductCategory,
                          string Product, decimal Sales, int Qty, string Priority);

public record Sku(string Id, string Name, string Category);
public record SkuDemand(string SkuId, int TotalUnits, int OrderCount, double Velocity, bool Seasonal);

public record SkuGroup(string GroupId, List<string> SkuIds);

public record ShelfLocation(string SkuId, string RackId, string SlotId, int Rank);

public record Rack(string RackId, int LevelCount, int SlotPerLevel, double MaxWeightKg);

public record OrderLine(string OrderId, string SkuId, int Qty);
public record Batch(string BatchId, string StationId, string Mode /* PTO|PTL */, List<OrderLine> Lines);

public record Station(string StationId, int Capacity /* orders concurrently */);
public record StationAssignment(string StationId, List<string> BatchIds);

public record HitRateResult(string Mode, double HitRate, int TotalItemsPicked, int TotalRackPresentations,
                            Dictionary<string, double> ByRack /* rackId->avg items */);
