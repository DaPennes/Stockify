namespace Stockify;

public static class StockifyInventoryManager
{
    public static Dictionary<string, NetworkMaterialInfo> CalculateNetworkInventory(
        StockifyNetwork network)
    {
        var inventory = new Dictionary<string, NetworkMaterialInfo>();
        if (network == null)
            return inventory;

        foreach (var shelf in network.RegisteredShelves)
        {
            shelf.UpdateStoredItems();
            foreach (var thing in shelf.StoredItems)
            {
                if (thing == null || thing.Destroyed || thing.IsForbidden(Faction.OfPlayer) ||
                    !network.AllowsMaterial(thing) ||
                    shelf.Thing is Building_Storage storage &&
                    storage.GetStoreSettings()?.AllowedToAccept(thing) != true)
                    continue;

                if (!inventory.TryGetValue(thing.def.defName, out var info))
                {
                    info = new NetworkMaterialInfo(thing.def);
                    inventory.Add(thing.def.defName, info);
                }

                info.AddItem(thing.def, thing.stackCount);
            }
        }

        return inventory;
    }

    public static int GetStoredQuantity(StockifyNetwork network, string materialDefName)
    {
        return CalculateNetworkInventory(network).TryGetValue(materialDefName, out var info)
            ? info.StoredCount
            : 0;
    }

    public static int GetTotalQuantity(StockifyNetwork network, string materialLabel)
    {
        return CalculateNetworkInventory(network).TryGetValue(materialLabel, out var info)
            ? info.TotalCount
            : 0;
    }

    public static int GetUnreservedQuantity(StockifyNetwork network, ThingDef material)
    {
        if (network == null || material == null)
            return 0;

        var map = network.RegisteredShelves
            .Select(shelf => shelf.Thing?.Map)
            .FirstOrDefault(candidate => candidate != null);
        if (map == null)
            return 0;

        var reservedCounts = new Dictionary<Thing, int>();
        foreach (var reservation in map.reservationManager.ReservationsReadOnly)
        {
            var thing = reservation.Target.Thing;
            if (thing?.def != material)
                continue;

            var reserved = reservation.StackCount > 0
                ? reservation.StackCount
                : thing.stackCount;
            reservedCounts.TryGetValue(thing, out var current);
            reservedCounts[thing] = current + reserved;
        }

        var haulingCounts = new Dictionary<Thing, int>();
        foreach (var pawn in map.mapPawns.AllPawnsSpawned)
        {
            var job = pawn.CurJob;
            var thing = job?.targetA.Thing;
            if (job?.def != JobDefOf.HaulToCell || thing?.def != material)
                continue;

            var hauling = job.count > 0 ? job.count : thing.stackCount;
            haulingCounts.TryGetValue(thing, out var current);
            haulingCounts[thing] = current + hauling;
        }

        var available = 0;
        foreach (var shelf in network.RegisteredShelves)
        {
            if (shelf.Thing is not Building_Storage storage)
                continue;

            foreach (var thing in storage.GetSlotGroup()?.HeldThings ?? Enumerable.Empty<Thing>())
            {
                if (thing.def != material || thing.IsForbidden(Faction.OfPlayer) ||
                    !network.AllowsMaterial(thing) ||
                    storage.GetStoreSettings()?.AllowedToAccept(thing) != true)
                    continue;

                reservedCounts.TryGetValue(thing, out var reserved);
                haulingCounts.TryGetValue(thing, out var hauling);
                available += Math.Max(
                    0, thing.stackCount - Math.Min(thing.stackCount, Math.Max(reserved, hauling)));
            }
        }

        return available;
    }

    public static List<MaterialInventoryEntry> GetAllMaterials(StockifyNetwork network)
    {
        return CreateEntries(CalculateNetworkInventory(network));
    }

    public static List<MaterialInventoryEntry> GetCachedMaterials(StockifyNetwork network)
    {
        return CreateEntries(network?.MaterialInventory);
    }

    private static List<MaterialInventoryEntry> CreateEntries(
        Dictionary<string, NetworkMaterialInfo> inventory)
    {
        if (inventory == null)
            return new List<MaterialInventoryEntry>();

        return inventory
            .Select(entry => new MaterialInventoryEntry
            {
                MaterialDef = entry.Value.MaterialDef,
                MaterialLabel = entry.Value.Label,
                Stored = entry.Value.StoredCount,
                Total = entry.Value.TotalCount
            })
            .OrderByDescending(entry => entry.Total)
            .ToList();
    }

    public class MaterialInventoryEntry
    {
        public ThingDef MaterialDef { get; set; }
        public string MaterialLabel { get; set; }
        public int Stored { get; set; }
        public int Total { get; set; }
    }
}
