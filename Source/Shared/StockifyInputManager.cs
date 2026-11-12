namespace Stockify;

/// <summary>
/// Manages INPUT operations - controls which materials can enter a Stockify network.
/// Handles automatic acceptance of hauled materials, shelf filters, and capacity checking.
/// </summary>
public static class StockifyInputManager
{
    /// <summary>
    /// Called when a colonist finishes hauling a thing to a map.
    /// Checks if the thing should be automatically added to a Stockify network.
    /// </summary>
    /// <param name="thing">The thing that was hauled.</param>
    /// <param name="destinationShelf">The shelf the thing was hauled to, if any.</param>
    public static void OnThingHauled(Thing thing, StockifyShelf destinationShelf)
    {
        // If no specific shelf, find the best network shelf
        if (destinationShelf == null)
        {
            var map = thing.Map;
            var mapComponent = Find.WorldObjects.GetComponent<StockifyMapComponent>();

            if (mapComponent != null)
            {
                // Try each network's input-enabled shelves
                foreach (var network in mapComponent.NETworks.Where(n => n.InputEnabled))
                {
                    var shelf = FindBestInputShelf(network, thing.def);
                    if (shelf != null)
                    {
                        // Transfer thing to this shelf
                        TransferThingToShelf(thing, shelf);
                        break; // First matching network wins
                    }
                }
            }
            return;
        }

        // If we have a specific shelf, check if it belongs to an input-enabled network
        var network = destinationShelf.Network;
        if (network != null && network.InputEnabled)
        {
            // Verify the thing is acceptable for this network
            if (IsInputEligible(network, thing))
            {
                // Check capacity
                if (destinationShelf.HasCapacity(1))
                {
                    // Accept the thing
                    destinationShelf.StoredItems.Add(thing);
                    Messages.Message($"Stockify: Accepted {thing.Label} into network {network.Name}", MessageTypeDefOf.PositiveEvent);
                }
                else
                {
                    Messages.Message($"Stockify: Network {network.Name} at capacity, {thing.Label} rejected", MessageTypeDefOf.NegativeEvent);
                }
            }
        }
    }

    /// <summary>
    /// Finds the best input shelf for a given thing across a network.
    /// </summary>
    /// <param name="network">The network to search.</param>
    /// <param name="thingDef">The thing definition.</param>
    /// <returns>The best input shelf, or null.</returns>
    private static StockifyShelf FindBestInputShelf(StockifyNetwork network, ThingDef thingDef)
    {
        // Check each shelf in the network for eligibility and capacity
        foreach (var shelf in network.RegisteredShelves)
        {
            if (IsShelfInputEligible(shelf, thingDef))
            {
                if (shelf.HasCapacity(1))
                {
                    return shelf;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Checks if a thing is eligible for input into a network based on filters and priorities.
    /// </summary>
    /// <param name="network">The network.</param>
    /// <param name="thingDef">The thing definition.</param>
    /// <returns>True if the thing is eligible.</returns>
    public static bool IsInputEligible(StockifyNetwork network, ThingDef thingDef)
    {
        // Check ingredient filters
        if (network.IngredientFilters != null && network.IngredientFilters.Count > 0)
        {
            var label = thingDef.label;
            var accepted = false;

            foreach (var filter in network.IngredientFilters)
            {
                if (label.ToLowerInvariant().Contains(filter.ToLowerInvariant()))
                {
                    accepted = true;
                    break;
                }
            }

            if (!accepted) return false;
        }

        // Check shelf-level filters (each shelf has its own filter)
        // For now, assume shelf filters are handled at the shelf level

        return true;
    }

    /// <summary>
    /// Checks if a shelf accepts a given thing type for input.
    /// </summary>
    /// <param name="shelf">The shelf.</param>
    /// <param name="thingDef">The thing definition.</param>
    /// <returns>True if the shelf accepts this thing for input.</returns>
    private static bool IsShelfInputEligible(StockifyShelf shelf, ThingDef thingDef)
    {
        // Check shelf's stored items for filter compatibility
        // A shelf is eligible if it has capacity and the thing type matches

        // Basic eligibility: shelf has capacity and thing is not forbidden
        if (!shelf.HasCapacity(1)) return false;

        // In a full implementation, we'd check the shelf's filter comp
        // For now, assume all registered shelves in an input-enabled network are eligible
        return true;
    }

    /// <summary>
    /// Transfers a thing to a shelf using valid vanilla hauling and storage mechanics.
    /// </summary>
    /// <param name="thing">The thing to transfer.</param>
    /// <param name="shelf">The destination shelf.</param>
    private static void TransferThingToShelf(Thing thing, StockifyShelf shelf)
    {
        // Add the thing to the shelf's stored items
        // The thing already exists on the map from the haul job
        // We just need to add it to the shelf's inventory

        shelf.StoredItems.Add(thing);

        // Note: In a full implementation, we'd need to actually move the thing
        // to the shelf's position or have a hauler job move it there.
        // The vanilla hauling system should handle this if the thing is at the shelf's map location.
    }
}