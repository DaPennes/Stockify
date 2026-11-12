namespace Stockify;

public class StockifyDesignationTool : Verse.Designator
{
    private StockifyNetwork _currentNetwork;

    public StockifyDesignationTool()
    {
        defaultLabel = "Stockify Designate Storage Area";
        defaultDesc = "Drag to add or remove storage tiles. Only valid stored items on selected tiles are counted.";
    }

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
        FinalizeNetworkCreation(new[] { cell }, Find.CurrentMap);
    }

    public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
    {
        FinalizeNetworkCreation(cells, Find.CurrentMap);
    }

    private void FinalizeNetworkCreation(IEnumerable<IntVec3> selectedCells, Map map)
    {
        var mapComponent = map?.GetComponent<StockifyMapComponent>();
        if (mapComponent == null)
            return;

        var cells = selectedCells?
            .Where(cell => cell.InBounds(map))
            .Distinct()
            .ToList() ?? new List<IntVec3>();
        if (cells.Count == 0)
            return;

        if (_currentNetwork != null && mapComponent.Networks.Contains(_currentNetwork))
        {
            var updatedCells = new HashSet<IntVec3>(_currentNetwork.DesignatedCells);
            foreach (var cell in cells)
            {
                if (!updatedCells.Add(cell))
                    updatedCells.Remove(cell);
            }

            _currentNetwork.SetDesignatedCells(updatedCells);
            mapComponent.RebuildShelfRegistration();
            Messages.Message(
                $"Updated {_currentNetwork.Name}: {_currentNetwork.DesignatedCells.Count} designated tiles.",
                MessageTypeDefOf.PositiveEvent);
            ShowOverlapWarning(_currentNetwork.DesignatedCells.Any(cell => mapComponent.Networks
                .TakeWhile(network => network != _currentNetwork)
                .Any(network => network.DesignatedCells.Contains(cell))));
            return;
        }

        var network = mapComponent.CreateNetwork(
            $"Storage Area {mapComponent.Networks.Count + 1}", cells);
        _currentNetwork = network;
        Messages.Message(
            $"Created {network.Name} with {network.DesignatedCells.Count} designated tiles.",
            MessageTypeDefOf.PositiveEvent);
        ShowOverlapWarning(cells.Any(cell => mapComponent.Networks
            .TakeWhile(candidate => candidate != network)
            .Any(candidate => candidate.DesignatedCells.Contains(cell))));
    }

    private static void ShowOverlapWarning(bool overlapsExisting)
    {
        if (overlapsExisting)
        {
            Messages.Message(
                "Stockify storage areas overlap. Items in overlapping tiles belong to the earlier area.",
                MessageTypeDefOf.NeutralEvent);
        }
    }
}
