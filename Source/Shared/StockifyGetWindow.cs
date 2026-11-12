namespace Stockify;

public class StockifyGetWindow : Verse.Window
{
    private readonly StockifyNetwork _network;
    private List<StockifyInventoryManager.MaterialInventoryEntry> _materials = new();
    private int _materialIndex;
    private int _requestedQuantity = 1;

    public StockifyGetWindow(StockifyNetwork initialNetwork = null)
        : base(new Verse.DefaultWindowDrawing())
    {
        layer = Verse.WindowLayer.Super;
        optionalTitle = "Stockify - Request Materials";
        doCloseX = true;
        closeOnCancel = true;
        absorbInputAroundWindow = true;
        _network = initialNetwork;
    }

    public override Vector2 InitialSize => new(600f, 400f);

    public override void PreOpen()
    {
        base.PreOpen();
        _materials = StockifyInventoryManager.GetAllMaterials(_network)
            .Where(material => material.Available > 0)
            .ToList();
        _materialIndex = 0;
        _requestedQuantity = _materials.Count == 0 ? 0 : 1;
    }

    public override void DoWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);

        if (_network == null)
        {
            listing.Label("No network selected.");
            listing.End();
            return;
        }

        listing.Label($"Network: {_network.Name}");
        if (_materials.Count == 0)
        {
            listing.Label("No available materials in this network.");
            listing.End();
            return;
        }

        var material = _materials[_materialIndex];
        if (Widgets.ButtonText(listing.GetRect(35f),
                $"{material.MaterialLabel}: {material.Available} available"))
        {
            _materialIndex = (_materialIndex + 1) % _materials.Count;
            material = _materials[_materialIndex];
            _requestedQuantity = Math.Min(_requestedQuantity, material.Available);
        }

        listing.Label($"Quantity (1-{material.Available}):");
        var quantityRect = listing.GetRect(30f);
        var quantityText = Widgets.TextField(quantityRect, _requestedQuantity.ToString());
        if (int.TryParse(quantityText, out var quantity))
        {
            _requestedQuantity = Math.Max(1, Math.Min(quantity, material.Available));
        }

        if (Widgets.ButtonText(listing.GetRect(35f), "Request"))
        {
            MakeRequest(material);
        }

        if (Widgets.ButtonText(listing.GetRect(35f), "Close"))
        {
            Close();
        }

        listing.End();
    }

    private void MakeRequest(StockifyInventoryManager.MaterialInventoryEntry material)
    {
        var map = Find.CurrentMap;
        var pawn = map?.mapPawns.FreeColonists.FirstOrDefault();
        if (pawn == null)
        {
            Messages.Message("A colonist is required to choose a delivery location.",
                MessageTypeDefOf.RejectInput);
            return;
        }

        var destination = new StockifyDestination(map, pawn.Position, pawn.LabelShort);
        var request = new GetRequest(material.MaterialLabel, _requestedQuantity, _network, destination);
        var completed = request.Process();
        if (!completed)
        {
            Messages.Message(request.GetStatusDescription(), MessageTypeDefOf.RejectInput);
            return;
        }

        Messages.Message(request.GetStatusDescription(), MessageTypeDefOf.PositiveEvent);
        Close();
    }
}
