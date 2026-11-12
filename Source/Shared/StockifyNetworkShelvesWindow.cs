namespace Stockify;

public class StockifyNetworkShelvesWindow : Window
{
    private readonly StockifyNetwork _network;
    private string _search = "";
    private Vector2 _scrollPosition;

    public StockifyNetworkShelvesWindow(StockifyNetwork network)
        : base(new DefaultWindowDrawing())
    {
        _network = network;
        layer = WindowLayer.Super;
        optionalTitle = $"Stockify shelves: {network.Name}";
        doCloseX = true;
        closeOnCancel = true;
        absorbInputAroundWindow = true;
    }

    public override Vector2 InitialSize => new(560f, 520f);

    public override void DoWindowContents(Rect inRect)
    {
        var mapComponent = Find.CurrentMap?.GetComponent<StockifyMapComponent>();
        if (mapComponent == null)
            return;

        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.Label($"Connected shelves: {_network.RegisteredShelves.Count}");
        _search = Widgets.TextField(listing.GetRect(30f), _search);
        listing.End();

        var listRect = new Rect(
            inRect.x, inRect.y + 64f, inRect.width, inRect.height - 64f);
        var shelves = Find.CurrentMap.listerThings.AllThings
            .OfType<Building_Storage>()
            .Where(shelf => shelf.AllSlotCells()
                                .Any(cell => _network.ContainsCell(cell, Find.CurrentMap)) &&
                            shelf.TryGetComp<CompStockifyShelf>() == null &&
                            (string.IsNullOrWhiteSpace(_search) ||
                             shelf.LabelCap.ToString().IndexOf(
                                 _search, StringComparison.OrdinalIgnoreCase) >= 0))
            .OrderBy(shelf => shelf.LabelCap.ToString(), StringComparer.OrdinalIgnoreCase)
            .ToList();
        var viewRect = new Rect(
            0f, 0f, listRect.width - 16f, Math.Max(listRect.height, shelves.Count * 32f));
        Widgets.BeginScrollView(listRect, ref _scrollPosition, viewRect);
        var itemListing = new Listing_Standard();
        itemListing.Begin(viewRect);

        if (shelves.Count == 0)
            itemListing.Label("No vanilla storage buildings match this warehouse area.");

        foreach (var shelf in shelves)
        {
            var owner = mapComponent.Networks.FirstOrDefault(network =>
                network.RegisteredShelves.Any(registered => registered.Thing == shelf));
            var row = itemListing.GetRect(30f);
            if (owner != null && owner != _network)
            {
                Widgets.Label(row, $"{shelf.LabelCap}: assigned to {owner.Name}");
                continue;
            }

            var included = owner == _network;
            if (Widgets.ButtonText(
                    row, $"{(included ? "[x] " : "[ ] ")}{shelf.LabelCap}"))
            {
                if (included)
                    _network.ExcludedShelfThingIds.Add(shelf.thingIDNumber);
                else
                    _network.ExcludedShelfThingIds.Remove(shelf.thingIDNumber);
                mapComponent.RebuildShelfRegistration();
            }
        }

        itemListing.End();
        Widgets.EndScrollView();
    }
}
