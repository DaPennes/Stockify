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

        if (_currentNetwork != null)
        {
            _currentNetwork.DesignatedArea = area;
            _currentNetwork.UpdateShelfRegistration(map);
            Messages.Message($"Updated network {_currentNetwork.Name} ({area.Area} cells).",
                MessageTypeDefOf.PositiveEvent);
            return;
        }

        var network = mapComponent.CreateNetwork(
            $"Storage Area {mapComponent.Networks.Count + 1}", area);
        network.UpdateShelfRegistration(map);
        _currentNetwork = network;

        Messages.Message($"Created network {network.Name} with {network.RegisteredShelves.Count} shelves.",
            MessageTypeDefOf.PositiveEvent);
    }
}
