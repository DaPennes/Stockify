namespace Stockify;

/// <summary>
/// CompBillGiver that provides Stockify PULL functionality to workbenches.
/// This component is added to vanilla workbenches to enable them to draw
/// ingredients from connected storage networks.
/// </summary>
public class StockifyBillComp : Verse.ThingComp
{
    /// <summary>
    /// Gets whether Stockify PULL is enabled for this workbench.
    /// </summary>
    public bool PullEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the source network IDs that this workbench can draw from.
    /// </summary>
    public List<int> SourceNetworks { get; set; } = new List<int>();

    /// <summary>
    /// Gets or sets the ingredient category filters.
    /// Only ingredients matching these categories will be accepted.
    /// </summary>
    public List<string> IngredientFilters { get; set; } = new List<string>();

    /// <summary>
    /// Gets the cached ingredient requirements for the current bill.
    /// </summary>
    public List<StockifyBillIntegration.IngredientRequirement> CachedRequirements { get; set; } = new List<StockifyBillIntegration.IngredientRequirement>();

    /// <summary>
    /// Called when the comp is spawned. Initializes default values.
    /// </summary>
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        // Initialize with default values - PULL enabled by default
        // Source networks will be populated from the map's networks
    }

    /// <summary>
    /// Called when the comp is loaded from save. Rebuilds cached data.
    /// </summary>
    /// <summary>
    /// Gets the ingredient requirements for the current bill.
    /// This is called by the bill processing system.
    /// </summary>
    /// <paramname="bill">The bill being processed.</param>
    /// <returns>List of ingredient requirements.</returns>
    public List<StockifyBillIntegration.IngredientRequirement> GetIngredientRequirements(Bill bill)
    {
        // Detect requirements from the bill's def
        return StockifyBillIntegration.DetectIngredientRequirements(bill);
    }

    /// <summary>
    /// Called each tick to try to satisfy ingredient requirements.
    /// This integrates with the vanilla bill processing pipeline.
    /// </summary>
    public override void CompTick()
    {
        base.CompTick();

        if (!PullEnabled) return;

        // Try to satisfy ingredients for the current bill
        var billGiver = parent as IBillGiver;
        var bill = billGiver?.BillStack?.Bills?.FirstOrDefault();
        if (bill == null) return;

        var workbench = parent as Thing;
        if (workbench == null) return;

        var map = workbench.Map;
        var mapComponent = workbench.Map?.GetComponent<StockifyMapComponent>();

        if (mapComponent == null) return;

        // Check if this workbench should try to pull ingredients
        var requirements = StockifyBillIntegration.DetectIngredientRequirements(bill);
        if (requirements.Count == 0) return; // No Stockify-dependent ingredients

        // For each requirement, try to reserve from eligible networks
        foreach (var req in requirements)
        {
            // Find eligible networks (those with PULL enabled and having materials)
            foreach (var network in mapComponent.Networks)
            {
                if (!network.PullEnabled) continue;

                var available = StockifyInventoryManager.GetAvailableQuantity(network, req.thingDef.label);
                if (available > 0)
                {
                    // Try to reserve the required count
                    var reserved = StockifyInventoryManager.TryReserveMaterials(network, req.thingDef.label, req.count);
                    if (reserved)
                    {
                        // Transfer to workbench
                        TransferToWorkbench(network, req.count, req.thingDef, workbench);
                        break; // Satisfied this requirement
                    }
                }
            }
        }
    }

    /// <summary>
    /// Transfers materials from a network to the workbench.
    /// </summary>
    /// <paramname="network">The source network.</param>
    /// <paramname="count">The quantity to transfer.</param>
    /// <paramname="thingDef">The thing definition.</param>
    /// <paramname="workbench">The workbench requiring ingredients.</param>
    private static void TransferToWorkbench(StockifyNetwork network, int count, ThingDef thingDef, Thing workbench)
    {
        // Find a source shelf within the network
        var sourceShelf = network.RegisteredShelves.FirstOrDefault();

        if (sourceShelf == null) return;

        // Create destination at the workbench position
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
    /// Expose data for save/load.
    /// </summary>
    public override void PostExposeData()
    {
        base.PostExposeData();

        var pullEnabled = PullEnabled;
        var sourceNetworks = SourceNetworks;
        var ingredientFilters = IngredientFilters;
        Scribe_Values.Look(ref pullEnabled, "pull_enabled", true);
        Scribe_Collections.Look(ref sourceNetworks, "source_networks", LookMode.Value);
        Scribe_Collections.Look(ref ingredientFilters, "ingredient_filters", LookMode.Value);
        PullEnabled = pullEnabled;
        SourceNetworks = sourceNetworks ?? new List<int>();
        IngredientFilters = ingredientFilters ?? new List<string>();
        CachedRequirements = new List<StockifyBillIntegration.IngredientRequirement>();
    }
}