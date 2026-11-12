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
    public ThingFilter Filter { get; set; }

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
    /// <param name="thing">The Thing representing the shelf building.</param>
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
    /// <param name="count">The number of items to check capacity for.</param>
    /// <returns>True if there is available capacity.</returns>
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
        var thingDef = Verse.DefDatabase<ThingDef>.GetDefByName(transferInfo.MaterialLabel);
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

        // Transfer the required quantity
        int transferred = 0;
        var itemsToTransfer = availableItems.TakeWhile((_) => transferred < transferInfo.Quantity).ToList();

        foreach (var thing in itemsToTransfer)
        {
            var toTransfer = Math.Min(thing.stackCount, transferInfo.Quantity - transferred);

            thing.stackCount -= toTransfer;
            transferred += toTransfer;

            // If we've transferred all stacks, remove them
            if (thing.stackCount <= 0)
            {
                this.StoredItems.Remove(thing);
            }
            else
            {
                // Partial stack remaining
                // thing.stackCount already updated above
            }

            // TODO: Actually move the thing to the destination using vanilla hauling
            // For now, we just reduce the shelf stock and consider the transfer complete
            // A full implementation would find a valid hauling destination or workbench slot
        }

        // If we transferred the full requested quantity, success
        if (transferred >= transferInfo.Quantity)
        {
            return true;
        }

        // Partial transfer - restore what we couldn't transfer
        // In a full implementation, we'd need to restore the items properly
        // For now, mark as partial success
        return transferred > 0;
    }
}