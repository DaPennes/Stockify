namespace Stockify;

/// <summary>
/// Manages OUTPUT operations - controls which materials can leave a Stockify network.
/// Handles workbench output destinations, priority-based shelf selection, and transfer validation.
/// </summary>
public static class StockifyOutputManager
{
    /// <summary>
    /// Called when a workbench completes production of a thing.
    /// Checks if the output should be sent to a Stockify network.
    /// </summary>
    /// <param name="thing">The finished product thing.</param>
    /// <param name="sourceShelf">The shelf the product should come from, if specified.</param>
    public static void OnWorkbenchProductionCompleted(Thing thing, StockifyShelf sourceShelf)
    {
        // If no specific shelf, find the best output shelf
        if (sourceShelf == null)
        {
            var map = thing.Map;
            var mapComponent = Find.WorldObjects.GetComponent<StockifyMapComponent>();

            if (mapComponent != null)
            {
                // Try each network's output-enabled shelves
                foreach (var network in mapComponent.NETworks.Where(n => n.OutputEnabled))
                {
                    var shelf = FindBestOutputShelf(network, thing.def);
                    if (shelf != null)
                    {
                        // Transfer thing from shelf to the workbench/product destination
                        TransferThingFromShelf(shelf, thing);
                        break; // First matching network wins
                    }
                }
            }
            return;
        }

        // If we have a specific shelf, check if it belongs to an output-enabled network
        var network = sourceShelf.Network;
        if (network != null && network.OutputEnabled)
        {
            // Check if the shelf has the thing
            if (sourceShelf.StoredItems.Contains(thing) || HasThingOnShelf(sourceShelf, thing.def))
            {
                // Transfer the thing from shelf
                TransferThingFromShelf(sourceShelf, thing);
            }
        }
    }

    /// <summary>
    /// Finds the best output shelf for a given thing across a network.
    /// </summary>
    /// <param name="network">The network to search.</param>
    /// <param name="thingDef">The thing definition.</param>
    /// <returns>The best output shelf, or null.</returns>
    private static StockifyShelf FindBestOutputShelf(StockifyNetwork network, ThingDef thingDef)
    {
        // Check each shelf in the network for eligibility and capacity as destination
        foreach (var shelf in network.RegisteredShelves)
        {
            // Check if shelf has the thing or available space
            if (shelf.HasCapacity(1))
            {
                // Shelf can accept output - return it
                // In a full implementation, we'd check if the shelf already has this thing type
                // and prefer shelves that already contain this material type
                return shelf;
            }
        }
        return null;
    }

    /// <summary>
    /// Checks if a shelf has a specific thing type stored.
    /// </summary>
    /// <param name="shelf">The shelf.</param>
    /// <param name="thingDef">The thing definition.</param>
    /// <returns>True if the shelf has this thing type.</returns>
    private static bool HasThingOnShelf(StockifyShelf shelf, ThingDef thingDef)
    {
        foreach (var thing in shelf.StoredItems)
        {
            if (thing.def == thingDef)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Transfers a thing from a shelf, simulating vanilla production output placement.
    /// </summary>
    /// <param name="shelf">The source shelf.</param>
    /// <param name="thing">The thing to transfer.</param>
    private static void TransferThingFromShelf(StockifyShelf shelf, Thing thing)
    {
        // Remove the thing from the shelf's stored items
        shelf.StoredItems.Remove(thing);

        // Note: In a full implementation, we'd need to actually place the thing
        // at a valid location on the map (e.g., near the workbench, or in the world).
        // The vanilla game's production completion logic handles item placement.
        // Here we just remove it from the shelf, and the game will place it appropriately.
    }
}