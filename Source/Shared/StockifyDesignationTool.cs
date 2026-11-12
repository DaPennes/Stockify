namespace Stockify;

/// <summary>
/// Designation tool for selecting storage areas and creating/managing storage networks.
/// Works similarly to the vanilla Home Area designation tool.
/// </summary>
public class StockifyDesignationTool : Verse.Designator
{
    private CellRect _selectionArea;
    private StockifyNetwork _currentNetwork;

    /// <summary>
    /// Creates a new designation tool.
    /// </summary>
    public StockifyDesignationTool()
    {
        this.defaultLabel = "Stockify Designate Storage Area";
        this.defaultDesc = "Drag to select an area. Shelves inside will be registered to a storage network.";
        this.soundSelect = Verse.SoundDefOf.Designate_Zap;
    }

    /// <summary>
    /// Gets or sets the current selection area.
    /// </summary>
    public CellRect SelectionArea
    {
        get => _selectionArea ?? CellRect.Invalid;
        set => _selectionArea = value;
    }

    /// <summary>
    /// Allows selection of cells on the current map.
    /// </summary>
    public override AcceptanceReport CanDesignateCell(IntVec3 cell)
    {
        return true;
    }

    /// <summary>
    /// Creates a network from a single selected cell.
    /// </summary>
    public override void DesignateSingleCell(IntVec3 cell)
    {
        var area = new CellRect(cell, cell);
        this.FinalizeNetworkCreation(area, Find.CurrentMap);
    }

    /// <summary>
    /// Creates a network from all cells selected with the designator.
    /// </summary>
    public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
    {
        var selectedCells = cells.ToList();
        if (selectedCells.Count == 0)
        {
            return;
        }

        var area = CellRect.MinMaxRect(
            selectedCells.Min(cell => cell.x),
            selectedCells.Min(cell => cell.z),
            selectedCells.Max(cell => cell.x),
            selectedCells.Max(cell => cell.z));

        this.FinalizeNetworkCreation(area, Find.CurrentMap);
    }

    /// <summary>
    /// Highlights shelves within the selection area.
    /// </summary>
    /// <param name="area">The selection area.</param>
    private void HighlightShelvesInArea(CellRect area)
    {
        var map = Find.CurrentMap;
        var shelves = this.DetectShelvesInArea(area, map);

        foreach (var shelf in shelves)
        {
            // Draw a temporary highlight around the shelf
            GenDraw.DrawOutlinedString(shelf.Thing.Position.ToString(),
                "StockifyHighlight", shelf.Thing.Position,
                new Color(0.0f, 1.0f, 0.0f, 0.8f));
        }
    }

    /// <summary>
    /// Detects eligible shelves within an area.
    /// </summary>
    /// <param name="area">The area to scan.</param>
    /// <param name="map">The map to scan on.</param>
    /// <returns>List of eligible shelves.</returns>
    private List<StockifyShelf> DetectShelvesInArea(CellRect area, Verse.Map map)
    {
        var shelves = new List<StockifyShelf>();

        // Find all things in the area
        var thingsInArea = map.listerThings.ThingsInArea(area, ThingRequest.ForDescribed(Lister.ThingsInRole(ThingRequest.Group.Building)));

        foreach (var thing in thingsInArea)
        {
            var shelf = thing.TryGetComp<StockifyShelf>();
            if (shelf != null && shelf.IsCompatibleShelf)
            {
                shelves.Add(shelf);
            }
        }

        return shelves;
    }

    /// <summary>
    /// Finalizes network creation after area selection.
    /// </summary>
    /// <param name="area">The selected area.</param>
    /// <param name="map">The current map.</param>
    private void FinalizeNetworkCreation(CellRect area, Verse.Map map)
    {
        // Check if there's already a network being edited, or create a new one
        var mapComponent = Find.WorldObjects.GetComponent<StockifyMapComponent>();

        if (_currentNetwork != null)
        {
            // Update existing network's area
            _currentNetwork.DesignatedArea = area;
            _currentNetwork.UpdateShelfRegistration();
            Messages.Message("StockifyNetworkAreaUpdated".Translate(_currentNetwork.Name, area.Area), MessageTypeDefOf.PositiveEvent);
        }
        else
        {
            // Create a new network
            var networkName = "Storage Area " + (mapComponent?.Networks.Count + 1 ?? 1);
            var newNetwork = mapComponent?.CreateNetwork(networkName, area);

            if (newNetwork != null)
            {
                // Register eligible shelves
                var shelves = this.DetectShelvesInArea(area, map);
                foreach (var shelf in shelves)
                {
                    newNetwork.RegisterShelf(shelf);
                }

                if (shelves.Count > 0)
                {
                    Messages.Message("StockifyNetworkCreated".Translate(newNetwork.Name, shelves.Count), MessageTypeDefOf.PositiveEvent);
                }
                else
                {
                    Messages.Message("StockifyNoShelvesFound".Translate(), MessageTypeDefOf.NegativeEvent);
                }
            }
        }
    }

}