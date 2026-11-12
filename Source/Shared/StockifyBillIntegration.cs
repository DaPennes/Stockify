namespace Stockify;

/// <summary>
/// Handles workbench integration for Stockify PULL functionality.
/// Detects ingredient requirements from production bills and manages
/// reservations and transfers from connected storage networks.
/// </summary>
public class StockifyBillIntegration
{
    /// <summary>
    /// Checks if a workbench has Stockify PULL enabled and configured.
    /// </summary>
    /// <paramname="thing">The workbench thing.</param>
    /// <returns>True if Stockify PULL is enabled for this workbench.</returns>
    public static bool HasStockifyPULL(Thing thing)
    {
        var comp = thing.TryGetComp<StockifyBillComp>();
        if (comp != null)
        {
            return comp.PullEnabled;
        }
        return false;
    }

    /// <summary>
    /// Gets the source networks configured for a workbench's Stockify PULL.
    /// </summary>
    /// <paramname="thing">The workbench thing.</param>
    /// <returns>List of source network IDs, or null if Stockify PULL not enabled.</returns>
    public static List<int> GetSourceNetworks(Thing thing)
    {
        var comp = thing.TryGetComp<StockifyBillComp>();
        if (comp != null && comp.SourceNetworks != null)
        {
            return comp.SourceNetworks;
        }
        return null;
    }

    /// <summary>
    /// Gets the ingredient filters configured for a workbench's Stockify PULL.
    /// </summary>
    /// <paramname="thing">The workbench thing.</param>
    /// <returns>List of allowed ingredient categories, or null if none.</returns>
    public static List<string> GetIngredientFilters(Thing thing)
    {
        var comp = thing.TryGetComp<StockifyBillComp>();
        if (comp != null && comp.IngredientFilters != null)
        {
            return comp.IngredientFilters;
        }
        return null;
    }

    /// <summary>
    /// Detects ingredient requirements from a production bill.
    /// </summary>
    /// <paramname="bill">The production bill.</param>
    /// <returns>List of ingredient requirements (thingDef, count, quality required).</returns>
    public static List<IngredientRequirement> DetectIngredientRequirements(Bill bill)
    {
        var requirements = new List<IngredientRequirement>();

        if (bill.def == null || bill.def.ingredients == null) return requirements;

        foreach (var ingredient in bill.def.ingredients)
        {
            var count = ingredientCount(ingredient);
            if (count > 0)
            {
                requirements.Add(new IngredientRequirement
                {
                    thingDef = ingredient.thingDef,
                    count = count,
                    qualityRequired = ingredient.filter?.quality ?? QualityCategory.Normal
                });
            }
        }

        return requirements;
    }

    /// <summary>
    /// Gets the total ingredient count from a bill ingredient list.
    /// </summary>
    /// <paramname="ingredients">The ingredient list from a bill.</param>
    /// <returns>Total count.</returns>
    private static int ingredientCount(IngredientCount ingredient)
    {
        if (ingredient.Count >= 1)
        {
            return ingredient.Count;
        }
        // For bill-level settings, return a reasonable default
        return 1;
    }

    /// <summary>
    /// Attempts to satisfy ingredient requirements from Stockify networks.
    /// </summary>
    /// <paramname="bill">The production bill being processed.</param>
    /// <paramname="workbench">The workbench thing.</param>
    /// <returns>True if ingredients were successfully reserved/transferred.</returns>
    public static bool TrySatisfyIngredients(Bill bill, Thing workbench)
    {
        // Check if Stockify PULL is enabled for this workbench
        if (!HasStockifyPULL(workbench))
        {
            return false;
        }

        // Detect ingredient requirements
        var requirements = DetectIngredientRequirements(bill);
        if (requirements.Count == 0) return true; // No Stockify-dependent ingredients

        var map = workbench.Map;
        var mapComponent = Find.WorldObjects.GetComponent<StockifyMapComponent>();

        if (mapComponent == null) return false;

        // For each ingredient requirement, try to find and reserve from networks
        foreach (var req in requirements)
        {
            // Find eligible networks (those with PULL enabled and input enabled)
            var eligibleNetworks = new List<StockifyNetwork>();

            foreach (var network in mapComponent.NETworks)
            {
                if (!network.PullEnabled) continue;

                // Check if network has this material type
                var available = StockifyInventoryManager.GetAvailableQuantity(network, req.thingDef.label);
                if (available > 0)
                {
                    eligibleNetworks.Add(network);
                }
            }

            // Try to reserve from each eligible network
            foreach (var network in eligibleNetworks)
            {
                var reserved = StockifyInventoryManager.TryReserveMaterials(network, req.thingDef.label, req.count);
                if (reserved)
                {
                    // Transfer materials to workbench-accessible location
                    TransferToWorkbench(network, req.count, req.thingDef, workbench);
                    break; // Successfully satisfied this ingredient
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Transfers materials from a network to a workbench-accessible location.
    /// </summary>
    /// <paramname="network">The source network.</param>
    /// <paramname="count">The quantity to transfer.</param>
    /// <paramname="thingDef">The thing definition.</param>
    /// <paramname="workbench">The workbench requiring the ingredients.</param>
    private static void TransferToWorkbench(StockifyNetwork network, int count, ThingDef thingDef, Thing workbench)
    {
        // Find a source shelf within the network
        var sourceShelf = network.RegisteredShelves.FirstOrDefault();

        if (sourceShelf == null) return;

        // Create destination at the workbench position
        var destCell = workbench.Position;
        var destination = new StockifyDestination(workbench);

        // Attempt the transfer
        var transferInfo = new StockifyTransferInfo
        {
            MaterialLabel = thingDef.label,
            Quantity = count,
            SourceNetwork = network
        };

        sourceShelf.TryTransferToDestination(transferInfo, destination);
    }

    /// <summary>
    /// Information about ingredient requirements for display.
    /// </summary>
    public class IngredientRequirement
    {
        /// <summary>
        /// The thing definition required.
        /// </summary>
        public ThingDef thingDef;

        /// <summary>
        /// The count required.
        /// </summary>
        public int count;

        /// <summary>
        /// The quality level required.
        /// </summary>
        public QualityCategory qualityRequired;
    }
}