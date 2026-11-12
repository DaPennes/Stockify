namespace Stockify;

/// <summary>
/// Manages network inventory calculations and display.
/// Calculates available/reserved/total quantities from registered shelves.
/// </summary>
public static class StockifyInventoryManager
{
    /// <summary>
    /// Calculates the complete inventory for a network from all registered shelves.
    /// </summary>
    /// <param name="network">The network to calculate inventory for.</param>
    /// <returns>Dictionary of material labels to their inventory info.</returns>
    public static Dictionary<string, NetworkMaterialInfo> CalculateNetworkInventory(StockifyNetwork network)
    {
        var inventory = new Dictionary<string, NetworkMaterialInfo>();

        foreach (var shelf in network.RegisteredShelves)
        {
            shelf.UpdateStoredItems();
            foreach (var thing in shelf.StoredItems)
            {
                var thingDef = thing.def;
                var stackCount = thing.stackCount;

                if (!inventory.ContainsKey(thingDef.label))
                {
                    inventory[thingDef.label] = new NetworkMaterialInfo(thingDef.label);
                }

                var materialInfo = inventory[thingDef.label];
                materialInfo.AddItem(thingDef, stackCount);
            }
        }

        return inventory;
    }

    /// <summary>
    /// Gets available quantity of a material in a network (non-reserved).
    /// </summary>
    /// <param name="network">The network.</param>
    /// <param name="materialLabel">The material label.</param>
    /// <returns>Available quantity.</returns>
    public static int GetAvailableQuantity(StockifyNetwork network, string materialLabel)
    {
        var inventory = CalculateNetworkInventory(network);

        if (inventory.TryGetValue(materialLabel, out var info))
        {
            return info.AvailableCount;
        }

        return 0;
    }

    /// <summary>
    /// Gets total quantity of a material in a network (available + reserved).
    /// </summary>
    /// <param name="network">The network.</param>
    /// <param name="materialLabel">The material label.</param>
    /// <returns>Total quantity.</returns>
    public static int GetTotalQuantity(StockifyNetwork network, string materialLabel)
    {
        var inventory = CalculateNetworkInventory(network);

        if (inventory.TryGetValue(materialLabel, out var info))
        {
            return info.TotalCount;
        }

        return 0;
    }

    /// <summary>
    /// Gets reserved quantity of a material in a network.
    /// </summary>
    /// <param name="network">The network.</param>
    /// <param name="materialLabel">The material label.</param>
    /// <returns>Reserved quantity.</returns>
    public static int GetReservedQuantity(StockifyNetwork network, string materialLabel)
    {
        var inventory = CalculateNetworkInventory(network);

        if (inventory.TryGetValue(materialLabel, out var info))
        {
            return info.ReservedCount;
        }

        return 0;
    }

    /// <summary>
    /// Tries to reserve materials in a network.
    /// </summary>
    /// <param name="network">The network.</param>
    /// <param name="materialLabel">The material label.</param>
    /// <param name="quantity">The quantity to reserve.</param>
    /// <returns>True if reservation was successful.</returns>
    public static bool TryReserveMaterials(StockifyNetwork network, string materialLabel, int quantity)
    {
        var inventory = CalculateNetworkInventory(network);

        if (inventory.TryGetValue(materialLabel, out var info))
        {
            return info.TryReserve(quantity);
        }

        return false;
    }

    /// <summary>
    /// Releases a material reservation in a network.
    /// </summary>
    /// <param name="network">The network.</param>
    /// <param name="materialLabel">The material label.</param>
    /// <param name="quantity">The quantity to release.</param>
    public static void ReleaseReservation(StockifyNetwork network, string materialLabel, int quantity)
    {
        var inventory = CalculateNetworkInventory(network);

        if (inventory.TryGetValue(materialLabel, out var info))
        {
            info.Release(quantity);
        }
    }

    /// <summary>
    /// Gets all materials in a network with their quantities.
    /// </summary>
    /// <param name="network">The network.</param>
    /// <returns>List of material info.</returns>
    public static List<MaterialInventoryEntry> GetAllMaterials(StockifyNetwork network)
    {
        var inventory = CalculateNetworkInventory(network);
        var entries = new List<MaterialInventoryEntry>();

        foreach (var entry in inventory)
        {
            entries.Add(new MaterialInventoryEntry
            {
                MaterialLabel = entry.Key,
                Available = entry.Value.AvailableCount,
                Reserved = entry.Value.ReservedCount,
                Total = entry.Value.TotalCount
            });
        }

        // Sort by total count descending
        entries.Sort((a, b) => b.Total.CompareTo(a.Total));

        return entries;
    }

    /// <summary>
    /// Entry for material inventory display.
    /// </summary>
    public class MaterialInventoryEntry
    {
        /// <summary>
        /// The material label.
        /// </summary>
        public string MaterialLabel { get; set; }

        /// <summary>
        /// Available (non-reserved) count.
        /// </summary>
        public int Available { get; set; }

        /// <summary>
        /// Reserved count.
        /// </summary>
        public int Reserved { get; set; }

        /// <summary>
        /// Total count (available + reserved).
        /// </summary>
        public int Total { get; set; }
    }
}