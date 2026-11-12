namespace Stockify;

public class StockifyDesignationTool : Verse.Designator
{
    private StockifyNetwork _currentNetwork;

    public StockifyDesignationTool()
    {
        defaultLabel = "Stockify Designate Storage Area";
        defaultDesc = "Drag to select an area. Storage buildings inside will join a network.";
    }

    public CellRect SelectionArea { get; set; } = CellRect.Empty;

    public override bool DragDrawMeasurements => true;

    public void SetNetwork(StockifyNetwork network)
    {
        _currentNetwork = network;
    }

    public void StartNewNetwork()
    {
        _currentNetwork = null;
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 cell)
    {
        return Find.CurrentMap != null && cell.InBounds(Find.CurrentMap);
    }

    public override void DesignateSingleCell(IntVec3 cell)
    {
        FinalizeNetworkCreation(CellRect.SingleCell(cell), Find.CurrentMap);
    }

    public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
    {
        var area = CellRect.FromCellList(cells);
        if (!area.IsEmpty)
        {
            FinalizeNetworkCreation(area, Find.CurrentMap);
        }
    }

    private void FinalizeNetworkCreation(CellRect area, Map map)
    {
        var mapComponent = map?.GetComponent<StockifyMapComponent>();
        if (mapComponent == null) return;

        var overlapsExisting = mapComponent.Networks.Any(network =>
            network != _currentNetwork && AreasOverlap(area, network.DesignatedArea));
        if (_currentNetwork != null)
        {
            _currentNetwork.DesignatedArea = area;
            mapComponent.RebuildShelfRegistration();
            Messages.Message($"Updated network {_currentNetwork.Name} ({area.Area} cells).",
                MessageTypeDefOf.PositiveEvent);
            ShowOverlapWarning(overlapsExisting);
            return;
        }

        var network = mapComponent.CreateNetwork(
            $"Storage Area {mapComponent.Networks.Count + 1}", area);
        _currentNetwork = network;

        Messages.Message($"Created network {network.Name} with {network.RegisteredShelves.Count} shelves.",
            MessageTypeDefOf.PositiveEvent);
        ShowOverlapWarning(overlapsExisting);
    }

    private static bool AreasOverlap(CellRect first, CellRect second)
    {
        return first.minX <= second.maxX && first.maxX >= second.minX &&
               first.minZ <= second.maxZ && first.maxZ >= second.minZ;
    }

    private static void ShowOverlapWarning(bool overlapsExisting)
    {
        if (overlapsExisting)
        {
            Messages.Message(
                "Stockify storage areas overlap. Buildings in the overlap belong to the earlier network.",
                MessageTypeDefOf.NeutralEvent);
        }
    }
}
