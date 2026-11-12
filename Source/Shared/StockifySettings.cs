namespace Stockify;

/// <summary>
/// Settings class for Stockify mod.
/// Contains all configurable settings including workbench PULL integration.
/// </summary>
public class StockifySettings : Verse.ModSettings
{
    /// <summary>
    /// Gets or sets whether PULL is enabled globally.
    /// When disabled, workbenches will not attempt to draw from Stockify networks.
    /// </summary>
    public bool PullEnabledGlobally { get; set; } = true;

    /// <summary>
    /// Gets or sets the default source priority for workbench PULL.
    /// Networks with higher priority are preferred when multiple networks have materials.
    /// </summary>
    public int DefaultSourcePriority { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether PULL automatically respects ingredient filters from bills.
    /// When enabled, workbenches will only pull ingredients that match the bill's filters.
    /// </summary>
    public bool RespectIngredientFilters { get; set; } = true;

    /// <summary>
    /// Gets or sets whether PULL respects quality requirements from bills.
    /// When enabled, workbenches will only pull ingredients meeting the quality requirement.
    /// </summary>
    public bool RespectQualityRequirements { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum distance (tiles) for automatic source shelf selection.
    /// Beyond this distance, the player will be prompted to select a source.
    /// </summary>
    public int MaxAutomaticTransferDistance { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to use balanced source selection (considering distance, priority, capacity).
    /// When disabled, uses nearest-first selection.
    /// </summary>
    public bool UseBalancedSourceSelection { get; set; } = true;

    /// <summary>
    /// Called to load settings from Scribe.
    /// </summary>
    public override void ExposeData()
    {
        base.ExposeData();
        var pullEnabledGlobally = PullEnabledGlobally;
        var defaultSourcePriority = DefaultSourcePriority;
        var respectIngredientFilters = RespectIngredientFilters;
        var respectQualityRequirements = RespectQualityRequirements;
        var maxAutomaticTransferDistance = MaxAutomaticTransferDistance;
        var useBalancedSourceSelection = UseBalancedSourceSelection;

        Scribe_Values.Look(ref pullEnabledGlobally, "pull_enabled_globally", true);
        Scribe_Values.Look(ref defaultSourcePriority, "default_source_priority", 0);
        Scribe_Values.Look(ref respectIngredientFilters, "respect_ingredient_filters", true);
        Scribe_Values.Look(ref respectQualityRequirements, "respect_quality_requirements", true);
        Scribe_Values.Look(ref maxAutomaticTransferDistance, "max_automatic_transfer_distance", 30);
        Scribe_Values.Look(ref useBalancedSourceSelection, "use_balanced_source_selection", true);

        PullEnabledGlobally = pullEnabledGlobally;
        DefaultSourcePriority = defaultSourcePriority;
        RespectIngredientFilters = respectIngredientFilters;
        RespectQualityRequirements = respectQualityRequirements;
        MaxAutomaticTransferDistance = maxAutomaticTransferDistance;
        UseBalancedSourceSelection = useBalancedSourceSelection;
    }
}