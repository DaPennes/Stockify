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

    /// <summary>
    /// Gets the storage priority of this shelf.
    /// </summary>
    public int StoragePriority { get; set; }

    /// <summary>
    /// Gets the Filter accepting things classes for this shelf.
    /// </summary>
    public Verse.FilterLayer? Filter { get; set; }

    /// <summary>
    /// Gets the stored items on this shelf.
    /// </summary>
    public List<Thing> StoredItems { get; } = new List<Thing>();

    /// <summary>
    /// Gets the cell rect that this shelf occupies.
    /// </summary>
    public CellRect OccupiedRect => this.Thing.Position.GetThingList(this.Thing.Map).OfType<Thing>().Select(t => t.Position).Aggregate((a, b) => CellRect.MinMax(a, b));

    /// <summary>
    /// Initializes a new instance of the <see cref="StockifyShelf"/> class.
    /// </summary>
    /// <param name="thing">The Thing representing the shelf building.</public>
    public StockifyShelf(Thing thing)
    {
        this.Thing = thing;
        this.ShelfID = thing.thingID;

        // Try to detect storage compatibility using vanilla APIs
        this.DetectStorageCompatibility();
    }

    /// <summary>
    /// Detects storage compatibility using RimWorld's storage APIs.
    /// </summary>
    private void DetectStorageCompatibility()
    {
        // Check if this thing has a storage compartment
        var comp = this.Thing.TryGetComp<Verse.CompStoraging>();
        if (comp != null)
        {
            // This is a storage-enabled building
            // Read the filter if available
            var filterComp = this.Thing.TryGetComp<Verse.CompForcedWin>(); // placeholder

            // Try to get slot group for storage priority
            var slotGroup = this.Thing.TryGetComp<Verse.CompSlotGroup>(); // placeholder

            // Store compatibility info
            this.StoragePriority = 0; // default
        }

        // Also check for ThingOwner which contains stored items
        var thingOwner = this.Thing.TryGetComp<Verse.CompThingOwner>(); // placeholder
        if (thingOwner != null && thingOwner.HasStoredThings)
        {
            // This building stores things
        }
    }

    /// <summary>
    /// Updates the stored items list from the shelf.
    /// Should be called when the shelf's contents may have changed.
    /// </summary>
    public void UpdateStoredItems()
    {
        this.StoredItems.Clear();

        // Get stored things from the building's comp
        var storageComp = this.Thing.TryGetComp<Verse.CompStoraging>();
        if (storageComp != null && storageComp.StoredThings != null)
        {
            foreach (var thing in storageComp.StoredThings)
            {
                // Only add if still valid and on the same map
                if (thing != null && thing.Map == this.Thing.Map)
                {
                    this.StoredItems.Add(thing);
                }
            }
        }

        // Also check direct thingOwner
        var thingOwner = this.Thing.TryGetComp<Verse.CompThingOwner>();
        if (thingOwner != null && thingOwner.InnerList != null)
        {
            foreach (var thing in thingOwner.InnerList)
            {
                if (thing != null && thing.Map == this.Thing.Map)
                {
                    this.StoredItems.Add(thing);
                }
            }
        }
    }

    /// <summary>
    /// Checks if this shelf has available capacity for more items.
    /// </summary>
    /// <param name="count">The number of items to check capacity for.</public>
    /// <returns>True if there is available capacity.</public>
    public bool HasCapacity(int count = 1)
    {
        // Vanilla shelves typically have a capacity based on their def
        var storageComp = this.Thing.TryGetComp<Verse.CompStoraging>();
        if (storageComp != null && storageComp.HasCapacity)
        {
            // Return based on available slots vs stored count
            return storageComp.StoredThings.Count < storageComp.MaximumStoredThings;
        }

        // Default: assume shelf has capacity if it's a valid storage building
        return this.StoredItems.Count < 50; // reasonable default
    }

    /// <summary>
    /// Gets the available count of a specific thing on this shelf.
    /// </summary>
    /// <param name="thingDef">The thing definition to count.</public>
    /// <param name="qualityFilter">Optional quality filter.</public>
    /// <returns>The available count.</public>
    public int GetAvailableCount(ThingDef thingDef, Verse.QualityLayer? qualityFilter = null)
    {
        int count = 0;

        foreach (var thing in this.StoredItems)
        {
            if (thing.def == thingDef)
            {
                // Apply quality filter if specified
                if (qualityFilter.HasValue && thing.quality != qualityFilter.Value)
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
    /// <param name="thingDef">The thing definition to count.</public>
    /// <param name="qualityFilter">Optional quality filter.</public>
    /// <returns>The reserved count.</public>
    public int GetReservedCount(ThingDef thingDef, Verse.QualityLayer? qualityFilter = null)
    {
        // This would track reservations made by the transfer system
        // For now, return 0 (reservations are tracked at the network level)
        return 0;
    }

    /// <summary>
    /// Tries to transfer items from this shelf to a destination.
    /// </summary>
    /// <param name="destination">The destination for the transfer.</public>
    /// <returns>True if the transfer was successful.</public>
    public bool TryTransferToDestination(StockifyNetwork.TransferInfo transferInfo, StockifyDestination destination)
    {
        // Implementation will handle the actual transfer logic
        // using vanilla hauling and storage mechanics
        return false; // placeholder
    }
}