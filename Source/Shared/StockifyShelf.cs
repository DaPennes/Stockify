namespace Stockify;

/// <summary>
/// Represents a vanilla shelf registered in a Stockify network.
/// Uses RimWorld's storage APIs to determine compatibility and read storage data.
/// </summary>
public class StockifyShelf
{
    /// <summary>
    /// Gets the Thing representing the shelf building.
    /// </summary>
    public Thing Thing { get; }

    /// <summary>
    /// Gets the unique identifier for this shelf.
    /// </summary>
    public int ShelfID { get; }

    public StockifyNetwork Network { get; internal set; }

    /// <summary>
    /// Gets the storage priority of this shelf.
    /// </summary>
    public int StoragePriority { get; set; }

    /// <summary>
    /// Gets the Filter accepting things classes for this shelf.
    /// </summary>
    public ThingFilter Filter { get; set; }

    /// <summary>
    /// Gets the stored items on this shelf.
    /// </summary>
    public List<Thing> StoredItems { get; } = new List<Thing>();

    /// <summary>
    /// Gets the cell rect that this shelf occupies.
    /// </summary>
    public CellRect OccupiedRect => this.Thing is Building_Storage storage
        ? CellRect.FromCellList(storage.AllSlotCells())
        : CellRect.SingleCell(this.Thing.Position);

    public bool IsCompatibleShelf => this.Thing is Building_Storage;

    /// <summary>
    /// Initializes a new instance of the <see cref="StockifyShelf"/> class.
    /// </summary>
    /// <param name="thing">The Thing representing the shelf building.</param>
    public StockifyShelf(Thing thing)
    {
        this.Thing = thing;
        this.ShelfID = thing.thingIDNumber;
        this.Filter = (thing as Building_Storage)?.GetStoreSettings()?.filter;
        this.StoragePriority = 0;
        this.UpdateStoredItems();
    }

    /// <summary>
    /// Updates the stored items list from the shelf.
    /// Should be called when the shelf's contents may have changed.
    /// </summary>
    public void UpdateStoredItems()
    {
        this.StoredItems.Clear();

        if (this.Thing is Building_Storage storage)
        {
            var slotGroup = storage.GetSlotGroup();
            if (slotGroup != null)
            {
                foreach (var thing in slotGroup.HeldThings)
                {
                    if (thing != null && !thing.Destroyed)
                    {
                        this.StoredItems.Add(thing);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if this shelf has available capacity for more items.
    /// </summary>
    /// <param name="count">The number of items to check capacity for.</param>
    /// <returns>True if there is available capacity.</returns>
    public bool HasCapacity(int count = 1)
    {
        if (count < 0 || this.Thing is not Building_Storage storage) return false;
        var slotGroup = storage.GetSlotGroup();
        if (slotGroup == null) return false;

        var maxItems = Math.Max(1, storage.def.building.maxItemsInCell) * slotGroup.CellsList.Count;
        return slotGroup.HeldThingsCount + count <= maxItems;
    }

    /// <summary>
    /// Gets the available count of a specific thing on this shelf.
    /// </summary>
    /// <param name="thingDef">The thing definition to count.</param>
    /// <param name="qualityFilter">Optional quality filter.</param>
    /// <returns>The available count.</returns>
    public int GetAvailableCount(ThingDef thingDef, QualityCategory? qualityFilter = null)
    {
        int count = 0;

        foreach (var thing in this.StoredItems)
        {
            if (thing.def == thingDef)
            {
                // Apply quality filter if specified
                var quality = thing.TryGetComp<CompQuality>()?.Quality;
                if (qualityFilter.HasValue && quality != qualityFilter.Value)
                {
                    continue;
                }

                count += thing.stackCount;
            }
        }

        return count;
    }

    /// <summary>
    /// Gets the reserved count of a specific thing on this shelf.
    /// </summary>
    /// <param name="thingDef">The thing definition to count.</param>
    /// <param name="qualityFilter">Optional quality filter.</param>
    /// <returns>The reserved count.</returns>
    public int GetReservedCount(ThingDef thingDef, QualityCategory? qualityFilter = null)
    {
        // This would track reservations made by the transfer system
        // For now, return 0 (reservations are tracked at the network level)
        return 0;
    }

    /// <summary>
    /// Tries to transfer items from this shelf to a destination.
    /// </summary>
    /// <param name="transferInfo">The transfer information (material, quantity, source network).</param>
    /// <param name="destination">The destination for the transfer.</param>
    /// <returns>True if the transfer was successful.</returns>
    public bool TryTransferToDestination(StockifyTransferInfo transferInfo, StockifyDestination destination)
    {
        // Verify destination is valid
        if (!destination.IsValid())
        {
            return false;
        }

        // Get the material definition from the label
        var thingDef = DefDatabase<ThingDef>.AllDefsListForReading
            .FirstOrDefault(def => def.label == transferInfo.MaterialLabel);
        if (thingDef == null)
        {
            return false;
        }

        // Find available items of this type on the shelf (non-reserved)
        var availableItems = new List<Thing>();
        int totalAvailable = 0;

        foreach (var thing in this.StoredItems)
        {
            if (thing.def == thingDef && thing.stackCount > 0)
            {
                // Items without bill reservations are available
                // In a full implementation, we'd check bill reservations here
                availableItems.Add(thing);
                totalAvailable += thing.stackCount;
            }
        }

        // Check if we have enough available
        if (totalAvailable < transferInfo.Quantity)
        {
            // Not enough materials, transfer what we can and return partial
            transferInfo.Quantity = totalAvailable;
        }

        if (transferInfo.Quantity <= 0) return false;

        var transferred = 0;
        foreach (var thing in availableItems)
        {
            var toTransfer = Math.Min(thing.stackCount, transferInfo.Quantity - transferred);
            var split = thing.SplitOff(toTransfer);
            var placed = GenPlace.TryPlaceThing(
                split, destination.Position, destination.Map, ThingPlaceMode.Near);
            if (!placed)
            {
                GenPlace.TryPlaceThing(split, this.Thing.Position, this.Thing.Map, ThingPlaceMode.Near);
                break;
            }

            transferred += toTransfer;
            if (transferred >= transferInfo.Quantity) break;
        }

        this.UpdateStoredItems();
        this.Network?.UpdateMaterialInventory();
        return transferred == transferInfo.Quantity;
    }
}