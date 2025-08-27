namespace Contracts;

public record StartRunCommand(Guid RunId, string DatasetPath, string Mode /* PTO|PTL */);

public record SalesContextReady(Guid RunId, string Mode, List<SkuDemand> Demand, List<OrderLine> Lines);

public record SkuGroupsCreated(Guid RunId, string Mode, List<SkuGroup> Groups, List<SkuDemand> Demand, List<OrderLine> Lines);

public record ShelfLocationsAssigned(Guid RunId, string Mode, List<ShelfLocation> Locations, List<SkuDemand> Demand, List<OrderLine> Lines);

public record RackLayoutCalculated(Guid RunId, string Mode, List<Rack> Racks, List<ShelfLocation> Locations, List<OrderLine> Lines);

public record BatchesCreated(Guid RunId, string Mode, List<Batch> Batches, List<ShelfLocation> Locations);

public record StationsAllocated(Guid RunId, List<StationAssignment> Assignments, List<Batch> Batches, List<ShelfLocation> Locations);

public record HitRateCalculated(Guid RunId, HitRateResult Result);
