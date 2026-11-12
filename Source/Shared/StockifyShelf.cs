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

}