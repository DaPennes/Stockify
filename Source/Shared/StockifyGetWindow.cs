namespace Stockify;

public class StockifyGetWindow : Verse.Window
{
    private StockifyNetwork _network;
    private List<StockifyNetwork> _sourceNetworks = new();
    private CompStockifyShelf _destination;
    private List<CompStockifyShelf> _destinations = new();
    private List<StockifyInventoryManager.MaterialInventoryEntry> _allMaterials = new();
    private List<StockifyInventoryManager.MaterialInventoryEntry> _materials = new();
    private string _searchTerm = "";
    private int _materialIndex;
    private int _requestedQuantity = 1;
    private ThingDef _cachedRequestMaterial;
    private StockifyNetwork _cachedRequestNetwork;
    private CompStockifyShelf _cachedRequestDestination;
    private int _cachedRequestTick = -1;
    private int _cachedRequestableCount;

    public StockifyGetWindow(StockifyNetwork initialNetwork = null)
        : this(initialNetwork, null)
    {
    }

    public StockifyGetWindow(StockifyNetwork initialNetwork, CompStockifyShelf destination)
        : base(new Verse.DefaultWindowDrawing())
    {
        layer = Verse.WindowLayer.Super;
        optionalTitle = "Stockify - Request Materials";
        doCloseX = true;
        closeOnCancel = true;
        absorbInputAroundWindow = true;
        _network = initialNetwork;
        _destination = destination;
    }

    public override Vector2 InitialSize => new(600f, 400f);

    public override void PreOpen()
    {
        base.PreOpen();
        _sourceNetworks = Find.CurrentMap?.GetComponent<StockifyMapComponent>()?.Networks
            .Where(network => network.GetEnabled)
            .ToList() ?? new List<StockifyNetwork>();
        if (!_sourceNetworks.Contains(_network))
            _network = _sourceNetworks.FirstOrDefault();

        _destinations = Find.CurrentMap?.listerThings.AllThings
            .Select(thing => thing.TryGetComp<CompStockifyShelf>())
            .Where(comp => comp != null)
            .ToList() ?? new List<CompStockifyShelf>();
        if (_destination == null || !_destinations.Contains(_destination))
            _destination = _destinations.FirstOrDefault();
        LoadMaterials();
    }

    public override void DoWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);

        if (_network == null)
        {
            listing.Label("No GET-enabled warehouse is available.");
            listing.End();
            return;
        }

        if (_sourceNetworks.Count > 1 &&
            Widgets.ButtonText(listing.GetRect(30f), $"Source warehouse: {_network.Name} (click to change)"))
        {
            var index = _sourceNetworks.IndexOf(_network);
            _network = _sourceNetworks[(index + 1) % _sourceNetworks.Count];
            LoadMaterials();
        }

        listing.Label($"Network: {_network.Name}");
        if (!_network.GetEnabled)
        {
            listing.Label("GET is disabled for this warehouse.");
            listing.End();
            return;
        }

        if (_destination == null)
        {
            listing.Label("Build a Stockify Shelf before requesting materials.");
            listing.End();
            return;
        }

        listing.Label("Search materials:");
        var searchTerm = Widgets.TextField(listing.GetRect(30f), _searchTerm);
        if (searchTerm != _searchTerm)
        {
            _searchTerm = searchTerm;
            FilterMaterials();
        }

        if (_destinations.Count > 1 && Widgets.ButtonText(listing.GetRect(30f),
                $"Deliver to: {GetShelfLabel(_destination)} (click to change)"))
        {
            var destinationIndex = _destinations.IndexOf(_destination);
            _destination = _destinations[(destinationIndex + 1) % _destinations.Count];
            LoadMaterials();
        }

        if (_materials.Count == 0)
        {
            listing.Label(_allMaterials.Count == 0
                ? "No stored materials in this network."
                : "No stored materials match this search.");
            listing.End();
            return;
        }

        var material = _materials[_materialIndex];
        if (Widgets.ButtonText(listing.GetRect(35f),
                $"{material.MaterialLabel}: {material.Stored} stored, " +
                $"{GetRequestableCount(material.MaterialDef)} available"))
        {
            _materialIndex = (_materialIndex + 1) % _materials.Count;
            material = _materials[_materialIndex];
            _requestedQuantity = Math.Min(_requestedQuantity, material.Stored);
        }

        listing.Label("Quantity to request:");
        var quantityRect = listing.GetRect(30f);
        var quantityText = Widgets.TextField(quantityRect, _requestedQuantity.ToString());
        if (int.TryParse(quantityText, out var quantity))
        {
            _requestedQuantity = Math.Max(1, quantity);
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
        var mapComponent = Find.CurrentMap?.GetComponent<StockifyMapComponent>();
        if (mapComponent == null || !_sourceNetworks.Contains(_network) ||
            !mapComponent.Networks.Contains(_network) || !_network.GetEnabled)
        {
            Messages.Message("The selected Stockify warehouse is no longer available for GET.",
                MessageTypeDefOf.RejectInput);
            return;
        }

        if (_destination?.parent.Destroyed != false ||
            _destination.parent.Map != Find.CurrentMap)
        {
            Messages.Message("The selected Stockify Shelf is no longer available.",
                MessageTypeDefOf.RejectInput);
            return;
        }

        if (material.MaterialDef == null ||
            _destination.parent is not Building_Storage destinationStorage ||
            destinationStorage.GetStoreSettings()?.AllowedToAccept(material.MaterialDef) != true)
        {
            Messages.Message(
                $"The selected Stockify Shelf does not accept {material.MaterialLabel}.",
                MessageTypeDefOf.RejectInput);
            return;
        }

        _network.UpdateMaterialInventory();
        if (!_network.MaterialInventory.TryGetValue(material.MaterialDef.defName, out var storedInfo) ||
            storedInfo.StoredCount <= 0)
        {
            Messages.Message($"No {material.MaterialLabel} is available in this warehouse.",
                MessageTypeDefOf.RejectInput);
            return;
        }

        var available = GetRequestableCount(material.MaterialDef, true);
        if (available == 0)
        {
            Messages.Message(
                $"No unreserved {material.MaterialLabel} is available for an additional request.",
                MessageTypeDefOf.RejectInput);
            return;
        }

        var quantity = Math.Min(_requestedQuantity, available);
        if (!_destination.RequestMaterial(material.MaterialDef, quantity, _network.ID))
        {
            Messages.Message(
                $"A pending {material.MaterialLabel} request or delivery uses a different warehouse. Cancel it before requesting from another source.",
                MessageTypeDefOf.RejectInput);
            return;
        }

        var unavailable = Math.Max(0, _requestedQuantity - quantity);
        Messages.Message(
            unavailable > 0
                ? $"Requested {quantity} {material.MaterialLabel}. {unavailable} are unavailable or already requested."
                : $"Requested {quantity} {material.MaterialLabel}. Colonists will haul it to {GetShelfLabel(_destination)}.",
            unavailable > 0 ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.PositiveEvent);
        Close();
    }

    private int GetRequestableCount(ThingDef material, bool refresh = false)
    {
        if (material == null || _network == null || _destination == null)
            return 0;

        var tick = Find.TickManager?.TicksGame ?? 0;
        if (!refresh && _cachedRequestMaterial == material &&
            _cachedRequestNetwork == _network &&
            _cachedRequestDestination == _destination &&
            tick - _cachedRequestTick < 30)
            return _cachedRequestableCount;

        var outstanding = _destination.GetUnassignedManualCount(material, _network.ID);
        var unreserved = StockifyInventoryManager.GetUnreservedQuantity(_network, material);
        _cachedRequestMaterial = material;
        _cachedRequestNetwork = _network;
        _cachedRequestDestination = _destination;
        _cachedRequestTick = tick;
        _cachedRequestableCount = Math.Max(0, unreserved - outstanding);
        return _cachedRequestableCount;
    }

    private static string GetShelfLabel(CompStockifyShelf shelf)
    {
        return shelf.ShelfName ?? shelf.parent.LabelCap;
    }

    private void LoadMaterials()
    {
        _allMaterials = _network == null
            ? new List<StockifyInventoryManager.MaterialInventoryEntry>()
            : StockifyInventoryManager.GetAllMaterials(_network)
                .Where(material => material.Stored > 0 &&
                                   _destination?.parent is Building_Storage storage &&
                                   storage.GetStoreSettings()?.AllowedToAccept(material.MaterialDef) == true)
                .ToList();
        FilterMaterials();
    }

    private void FilterMaterials()
    {
        _materials = _allMaterials
            .Where(material => string.IsNullOrWhiteSpace(_searchTerm) ||
                               material.MaterialLabel.IndexOf(
                                   _searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();
        _materialIndex = 0;
        _requestedQuantity = _materials.Count == 0 ? 0 : 1;
    }
}
