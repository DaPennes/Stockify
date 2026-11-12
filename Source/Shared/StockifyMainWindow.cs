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
            listing.Label("No network selected. Use the Stockify designation tool to create one.");
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
        foreach (var material in StockifyInventoryManager.GetAllMaterials(_selectedNetwork))
        {
            listing.Label($"{material.MaterialLabel}: {material.Available} available, {material.Reserved} reserved");
        }

        listing.Gap(8f);
        _newNetworkName = Widgets.TextField(listing.GetRect(30f), _newNetworkName);
        if (Widgets.ButtonText(listing.GetRect(30f), "Rename network") &&
            !string.IsNullOrWhiteSpace(_newNetworkName))
        {
            _selectedNetwork.Name = _newNetworkName.Trim();
            _newNetworkName = "";
        }
    }

    private static void DrawToggle(Listing_Standard listing, string label, bool enabled, Action<bool> setEnabled)
    {
        if (Widgets.ButtonText(listing.GetRect(35f), $"{label}: {(enabled ? "Enabled" : "Disabled")}"))
        {
            setEnabled(!enabled);
        }
    }
}
