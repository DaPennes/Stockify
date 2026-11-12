namespace Stockify;

public class StockifyNetworkFilterWindow : Window
{
    private readonly StockifyNetwork _network;
    private string _search = "";
    private Vector2 _scrollPosition;

    public StockifyNetworkFilterWindow(StockifyNetwork network)
        : base(new DefaultWindowDrawing())
    {
        _network = network;
        layer = WindowLayer.Super;
        optionalTitle = $"Stockify filter: {network.Name}";
        doCloseX = true;
        closeOnCancel = true;
        absorbInputAroundWindow = true;
    }

    public override Vector2 InitialSize => new(520f, 600f);

    public override void DoWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.Label("Items are allowed by default. Toggle items to exclude them from this warehouse.");
        _search = Widgets.TextField(listing.GetRect(30f), _search);
        var items = DefDatabase<ThingDef>.AllDefsListForReading
            .Where(def => def.category == ThingCategory.Item &&
                          def.EverHaulable &&
                          !string.IsNullOrEmpty(def.label))
            .Where(def => string.IsNullOrWhiteSpace(_search) ||
                          def.label.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(def => def.label)
            .ToList();
        listing.End();

        var listRect = new Rect(
            inRect.x, inRect.y + 64f, inRect.width, inRect.height - 64f);
        var viewRect = new Rect(
            0f, 0f, listRect.width - 16f, Math.Max(listRect.height, items.Count * 28f));
        Widgets.BeginScrollView(listRect, ref _scrollPosition, viewRect);
        var itemListing = new Listing_Standard();
        itemListing.Begin(viewRect);
        foreach (var item in items)
        {
            var excluded = _network.IngredientFilters.Contains(item.defName);
            if (!Widgets.ButtonText(
                    itemListing.GetRect(26f),
                    $"{(excluded ? "[ ] " : "[x] ")}{item.LabelCap}"))
                continue;

            if (excluded)
                _network.IngredientFilters.Remove(item.defName);
            else
                _network.IngredientFilters.Add(item.defName);
            _network.UpdateMaterialInventory();
        }
        itemListing.End();
        Widgets.EndScrollView();
    }
}
