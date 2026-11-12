namespace Stockify;

public class StockifyMainWindow : Verse.Window
{
    private StockifyMapComponent _mapComponent;
    private StockifyNetwork _selectedNetwork;
    private int _tabIndex;
    private string _newNetworkName = "";

    public StockifyMainWindow() : base(new Verse.DefaultWindowDrawing())
    {
        layer = Verse.WindowLayer.Super;
        optionalTitle = "Stockify";
        doCloseX = true;
        closeOnCancel = true;
        absorbInputAroundWindow = true;
    }

    public override Vector2 InitialSize => new(800f, 600f);

    public override void PreOpen()
    {
        base.PreOpen();
        _mapComponent = Find.CurrentMap?.GetComponent<StockifyMapComponent>();
        _selectedNetwork = _mapComponent?.Networks.FirstOrDefault();
    }

    public override void DoWindowContents(Rect inRect)
    {
        _mapComponent = Find.CurrentMap?.GetComponent<StockifyMapComponent>();

        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.Label("Storage networks");

        if (_mapComponent == null || _mapComponent.Networks.Count == 0)
        {
            listing.Label(_mapComponent == null
                ? "Open a colony map to manage Stockify warehouses."
                : "No warehouse has been designated yet.");
            if (_mapComponent != null &&
                Widgets.ButtonText(listing.GetRect(30f), "Designate a new warehouse"))
            {
                StockifyMod.Instance.DesignationTool.StartNewNetwork();
                Find.DesignatorManager.Select(StockifyMod.Instance.DesignationTool);
            }
            listing.End();
            return;
        }

        if (_selectedNetwork == null || !_mapComponent.Networks.Contains(_selectedNetwork))
        {
            _selectedNetwork = _mapComponent.Networks[0];
        }

        if (Widgets.ButtonText(listing.GetRect(30f),
                $"Network: {_selectedNetwork.Name} ({_mapComponent.Networks.IndexOf(_selectedNetwork) + 1}/{_mapComponent.Networks.Count})"))
        {
            var index = _mapComponent.Networks.IndexOf(_selectedNetwork);
            _selectedNetwork = _mapComponent.Networks[(index + 1) % _mapComponent.Networks.Count];
        }

        var tabLabels = new[] { "Network", "GET", "PULL", "INPUT", "OUTPUT" };
        var tabRect = listing.GetRect(30f);
        var tabWidth = tabRect.width / tabLabels.Length;
        for (var i = 0; i < tabLabels.Length; i++)
        {
            var rect = new Rect(tabRect.x + tabWidth * i, tabRect.y, tabWidth, tabRect.height);
            if (Widgets.ButtonText(rect, tabLabels[i]))
            {
                _tabIndex = i;
            }
        }

        listing.Gap(10f);
        switch (_tabIndex)
        {
            case 0:
                DrawNetworkTab(listing);
                break;
            case 1:
                DrawToggle(listing, "GET", _selectedNetwork.GetEnabled,
                    enabled => _selectedNetwork.GetEnabled = enabled);
                if (Widgets.ButtonText(listing.GetRect(35f), "Request materials"))
                {
                    Find.WindowStack.Add(new StockifyGetWindow(_selectedNetwork));
                }
                break;
            case 2:
                DrawToggle(listing, "PULL", _selectedNetwork.PullEnabled,
                    enabled => _selectedNetwork.PullEnabled = enabled);
                break;
            case 3:
                DrawToggle(listing, "INPUT", _selectedNetwork.InputEnabled,
                    enabled => _selectedNetwork.InputEnabled = enabled);
                break;
            case 4:
                DrawToggle(listing, "OUTPUT", _selectedNetwork.OutputEnabled,
                    enabled => _selectedNetwork.OutputEnabled = enabled);
                break;
        }

        listing.End();
    }

    private void DrawNetworkTab(Listing_Standard listing)
    {
        listing.Label($"Shelves: {_selectedNetwork.RegisteredShelves.Count}");
        listing.Label($"Designated tiles: {_selectedNetwork.DesignatedCells.Count}");
        var incoming = CountIncomingItems(_selectedNetwork);
        var outgoingDemand = _mapComponent.LocalShelves
            .Sum(shelf => shelf.GetOutstandingDemandFrom(_selectedNetwork));
        listing.Label($"Incoming: {incoming} items in Stockify hauling jobs");
        listing.Label($"Outgoing demand: {outgoingDemand} items requested by linked shelves");
        if (Widgets.ButtonText(listing.GetRect(30f),
                $"Warehouse transfer priority: {_selectedNetwork.StoragePriority} (increase)"))
            _selectedNetwork.StoragePriority = (_selectedNetwork.StoragePriority + 1) % 4;
        if (Widgets.ButtonText(listing.GetRect(30f), "Configure warehouse material filter"))
            Find.WindowStack.Add(new StockifyNetworkFilterWindow(_selectedNetwork));
        if (Widgets.ButtonText(listing.GetRect(30f), "Manage warehouse shelves"))
            Find.WindowStack.Add(new StockifyNetworkShelvesWindow(_selectedNetwork));
        foreach (var material in StockifyInventoryManager.GetCachedMaterials(_selectedNetwork))
        {
            listing.Label($"{material.MaterialLabel}: {material.Stored} stored");
        }

        listing.Gap(8f);
        if (Widgets.ButtonText(listing.GetRect(30f), "Designate a new warehouse"))
        {
            StockifyMod.Instance.DesignationTool.StartNewNetwork();
            Find.DesignatorManager.Select(StockifyMod.Instance.DesignationTool);
        }
        if (Widgets.ButtonText(listing.GetRect(30f), "Edit selected storage area"))
        {
            StockifyMod.Instance.DesignationTool.SetNetwork(_selectedNetwork);
            Find.DesignatorManager.Select(StockifyMod.Instance.DesignationTool);
        }

        listing.Gap(8f);
        _newNetworkName = Widgets.TextField(listing.GetRect(30f), _newNetworkName);
        if (Widgets.ButtonText(listing.GetRect(30f), "Rename network") &&
            !string.IsNullOrWhiteSpace(_newNetworkName))
        {
            _selectedNetwork.Name = _newNetworkName.Trim();
            _newNetworkName = "";
        }

        if (Widgets.ButtonText(listing.GetRect(30f), "Delete selected storage area"))
        {
            var network = _selectedNetwork;
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                $"Delete storage area '{network.Name}'? Shelf assignments to it will be removed.",
                () =>
                {
                    _mapComponent.RemoveNetwork(network);
                    StockifyMod.Instance.DesignationTool.StartNewNetwork();
                    _selectedNetwork = _mapComponent.Networks.FirstOrDefault();
                }));
        }
    }

    private static int CountIncomingItems(StockifyNetwork network)
    {
        var map = Find.CurrentMap;
        if (map == null)
            return 0;

        var storageCells = new HashSet<IntVec3>(network.DesignatedCells
            .Where(cell => network.ContainsCell(cell, map)));
        var workGivers = new HashSet<WorkGiverDef>
        {
            DefDatabase<WorkGiverDef>.GetNamed("StockifyReplenishShelf"),
            DefDatabase<WorkGiverDef>.GetNamed("StockifyGetShelf")
        };
        var count = 0;
        foreach (var pawn in map.mapPawns.AllPawnsSpawned)
        {
            var job = pawn.CurJob;
            if (job?.def != JobDefOf.HaulToCell ||
                !workGivers.Contains(job.workGiverDef) ||
                !storageCells.Contains(job.targetB.Cell))
                continue;

            count += job.count > 0 ? job.count : job.targetA.Thing?.stackCount ?? 0;
        }

        return count;
    }

    private static void DrawToggle(Listing_Standard listing, string label, bool enabled, Action<bool> setEnabled)
    {
        if (Widgets.ButtonText(listing.GetRect(35f), $"{label}: {(enabled ? "Enabled" : "Disabled")}"))
        {
            setEnabled(!enabled);
        }
    }
}
