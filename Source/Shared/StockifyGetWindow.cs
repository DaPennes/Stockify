namespace Stockify;

/// <summary>
/// Main window for GET requests - allows players to request materials
/// from connected storage networks.
/// </summary>
public class StockifyGetWindow : Verse.Window
{
    private StockifyNetwork _selectedNetwork;
    private string _searchTerm;
    private string _selectedMaterial;
    private int _requestedQuantity;
    private int _availableQuantity;
    private GetRequest _currentRequest;
    private List<string> _materialNames = new List<string>();
    private Vector2 _scrollPosition;
    private bool _showInventory = true;

    /// <summary>
    /// Creates a new GET request window.
    /// </summary>
    /// <param name="initialNetwork">The network to start with, or null for first network.</param>
    public StockifyGetWindow(StockifyNetwork initialNetwork = null) : base(
        new Rect(0f, 0f, 600f, 500f), "Stockify - Request Materials", WindowLayer.Super,
        doCloseOnCancel: true, closeButtonLimit: 1, absorbInputAroundWindow: true)
    {
        _selectedNetwork = initialNetwork;

        // Initialize with first available network if none specified
        if (_selectedNetwork == null)
        {
            var mapComponent = Find.WorldObjects.GetComponent<StockifyMapComponent>();
            if (mapComponent?.Networks.Count > 0)
            {
                _selectedNetwork = mapComponent.Networks[0];
            }
        }

        // Populate material names from the selected network
        UpdateMaterialList();
    }

    /// <summary>
    /// Updates the material list from the selected network.
    /// </summary>
    private void UpdateMaterialList()
    {
        _materialNames.Clear();

        if (_selectedNetwork != null)
        {
            var inventory = StockifyInventoryManager.GetAllMaterials(_selectedNetwork);
            foreach (var material in inventory)
            {
                _materialNames.Add(material.MaterialLabel);
            }
        }

        // If no materials in network, add placeholder
        if (_materialNames.Count == 0)
        {
            _materialNames.Add("No materials in network");
        }

        // Select first material if none selected
        if (string.IsNullOrEmpty(_selectedMaterial) && _materialNames.Count > 0)
        {
            _selectedMaterial = _materialNames[0];
        }

        // Update quantities
        UpdateQuantities();
    }

    /// <summary>
    /// Updates the available quantity display for the selected material.
    /// </summary>
    private void UpdateQuantities()
    {
        _availableQuantity = 0;

        if (_selectedNetwork != null && !string.IsNullOrEmpty(_selectedMaterial))
        {
            _availableQuantity = StockifyInventoryManager.GetAvailableQuantity(_selectedNetwork, _selectedMaterial);
        }

        // If requesting, ensure requested quantity doesn't exceed available
        if (_requestedQuantity > _availableQuantity)
        {
            _requestedQuantity = _availableQuantity;
        }
    }

    /// <summary>
    /// Called when the window is opened.
    /// </summary>
    public override void PreOpen()
    {
        base.PreOpen();
        _searchTerm = "";
    }

    /// <summary>
    /// Called to do the window GUI.
    /// </summary>
    public override void DoWindowContents(Rect inRect)
    {
        var listingStandard = new Verse.Listing_Standard();
        listingStandard.Begin(inRect);

        // Network selection
        DrawNetworkSelection(listingStandard);

        // Material search and selection
        DrawMaterialSelection(listingStandard);

        // Request details
        DrawRequestDetails(listingStandard);

        // Buttons
        DrawButtons(listingStandard);

        listingStandard.End();
    }

    /// <summary>
    /// Draws the network selection area.
    /// </summary>
    /// <param name="listingStandard">The listing standard.</param>
    private void DrawNetworkSelection(Verse.Listing_Standard listingStandard)
    {
        listingStandard.GapLine(6);

        // Network selector
        var networkLabel = _selectedNetwork != null
            ? $"Network: {_selectedNetwork.Name} (ID: {_selectedNetwork.ID})"
            : "No network selected";

        Verse.Text.Anchor = Verse.TextAnchor.MiddleLeft;
        Verse.Text.Font = Verse.Font.Small;
        Verse.Text.WordWrap = false;
        GUI.Label(new Rect(10f, listingStandard.CurHeight, 200f, 30f), networkLabel);

        // Network dropdown logic would go here
        // For now, just display the selected network

        listingStandard.GapLine(6);
    }

    /// <summary>
    /// Draws the material selection area.
    /// </summary>
    /// <param name="listingStandard">The listing standard.</param>
    private void DrawMaterialSelection(Verse.Listing_Standard listingStandard)
    {
        var rect = listingStandard.GetRect(250f);
        rect.xMin += 10f;
        rect.yMin += 10f;

        // Material search box
        var searchRect = new Rect(rect.xMin, rect.yMin, 200f, 30f);
        _searchTerm = Verse.Widgets.TextField(searchRect, _searchTerm);

        // Filter materials based on search term
        var filteredMaterials = FilterMaterials(_searchTerm);

        // Material dropdown
        if (filteredMaterials.Count > 0)
        {
            var labelRect = new Rect(rect.xMin, rect.yMin + 35f, 200f, 25f);
            Verse.Text.Anchor = Verse.TextAnchor.MiddleLeft;
            GUI.Label(labelRect, $"Material:");

            // Simple selection - show first filtered material or search results
            if (_materialNames.Count > 0)
            {
                var materialRect = new Rect(rect.xMin, rect.yMin + 60f, 200f, 25f);

                // If we have a selection, display it
                if (!string.IsNullOrEmpty(_selectedMaterial))
                {
                    GUI.Label(materialRect, $"Selected: {_selectedMaterial}");
                    _availableQuantity = StockifyInventoryManager.GetAvailableQuantity(_selectedNetwork, _selectedMaterial);
                }
                else
                {
                    // Show first available material
                    GUI.Label(materialRect, $"First: {_materialNames[0]}");
                    _selectedMaterial = _materialNames[0];
                    _availableQuantity = StockifyInventoryManager.GetAvailableQuantity(_selectedNetwork, _selectedMaterial);
                }
            }
        }

        listingStandard.GapLine(4);
    }

    /// <summary>
    /// Filters materials by search term.
    /// </summary>
    /// <param name="searchTerm">The search term.</param>
    /// <returns>Filtered list of material names.</returns>
    private List<string> FilterMaterials(string searchTerm)
    {
        var filtered = new List<string>();

        if (string.IsNullOrEmpty(searchTerm))
        {
            return _materialNames;
        }

        var searchLower = searchTerm.ToLowerInvariant();

        foreach (var material in _materialNames)
        {
            if (material.ToLowerInvariant().Contains(searchLower))
            {
                filtered.Add(material);
            }
        }

        return filtered;
    }

    /// <summary>
    /// Draws the request details area.
    /// </summary>
    /// <param name="listingStandard">The listing standard.</param>
    private void DrawRequestDetails(Verse.Listing_Standard listingStandard)
    {
        var rect = listingStandard.GetRect(150f);
        rect.xMin += 10f;
        rect.yMin += 10f;

        // Requested quantity
        var quantityLabel = $"Request: {_requestedQuantity}";
        GUI.Label(new Rect(rect.xMin, rect.yMin, 150f, 30f), quantityLabel);

        // Quantity slider/drag
        var quantityRect = new Rect(rect.xMin, rect.yMin + 30f, 150f, 30f);
        _requestedQuantity = Verse.Widgets.HorizontalSlider(quantityRect, _requestedQuantity, 0, _availableQuantity, true, 1f);

        // Available quantity display
        var availableLabel = $"Available: {_availableQuantity}";
        GUI.Label(new Rect(rect.xMin, rect.yMin + 65f, 150f, 30f), availableLabel);

        // Request status
        if (_currentRequest != null)
        {
            var statusLabel = $"Status: {_currentRequest.GetStatusDescription()}";
            GUI.color = Color.green;
            GUI.Label(new Rect(rect.xMin, rect.yMin + 100f, 150f, 30f), statusLabel);
            GUI.color = Color.white;
        }
    }

    /// <summary>
    /// Draws the action buttons.
    /// </summary>
    /// <param name="listingStandard">The listing standard.</param>
    private void DrawButtons(Verse.Listing_Standard listingStandard)
    {
        var buttonWidth = 120f;
        var buttonHeight = 30f;
        var startX = listingStandard.CurRect.xMax - buttonWidth - 10f;
        var startY = listingStandard.CurRect.yMax - buttonHeight * 3 - 15f;

        // Request button
        var requestRect = new Rect(startX, startY, buttonWidth, buttonHeight);
        if (Verse.Widgets.ButtonText(requestRect, "Request"))
        {
            MakeRequest();
        }

        // Cancel button
        var cancelRect = new Rect(startX, startY + buttonHeight + 5f, buttonWidth, buttonHeight);
        if (Verse.Widgets.ButtonText(cancelRect, "Cancel"))
        {
            Close();
        }

        // Complete button (if request in progress)
        var completeRect = new Rect(startX, startY + (buttonHeight + 5f) * 2, buttonWidth, buttonHeight);
        if (_currentRequest != null && _currentRequest.Status == GetRequestStatus.InProgress)
        {
            if (Verse.Widgets.ButtonText(completeRect, "Complete"))
            {
                _currentRequest.Status = GetRequestStatus.Complete;
                Messages.Message("StockifyRequestCompleted".Translate(_currentRequest.MaterialLabel, _currentRequest.ActualQuantity), MessageTypeDefOf.PositiveEvent);
                Close();
            }
        }
    }

    /// <summary>
    /// Creates a new GET request with the current settings.
    /// </summary>
    private void MakeRequest()
    {
        if (_selectedNetwork == null || string.IsNullOrEmpty(_selectedMaterial))
        {
            Messages.Message("StockifyNoNetworkOrMaterial".Translate(), MessageTypeDefOf.NegativeEvent);
            return;
        }

        if (_availableQuantity <= 0)
        {
            Messages.Message("StockifyNoAvailableMaterials".Translate(_selectedMaterial), MessageTypeDefOf.NegativeEvent);
            return;
        }

        // Create destination - use a default shelf or the workbench location
        var destination = new StockifyDestination(Find.CurrentMap.gameObjects.FirstOrDefault() ?? ThingRequest.MakeThingRequest("StockifyRequestDest", ThingCategory.ThingCount.Anything, null).Accepted ? null : Find.CurrentMap.listerThings.ThingsInCell(Find.PlayerFaction.Leader.Position));

        // Create the GET request
        _currentRequest = new GetRequest(_selectedMaterial, _requestedQuantity, _selectedNetwork, destination);

        // Process the request immediately
        _currentRequest.Process();

        // Show status message
        var statusMsg = _currentRequest.Status switch
        {
            GetRequestStatus.Complete => $"Stockify: Requested {_currentRequest.ActualQuantity} {_currentRequest.MaterialLabel} - Transfer completed",
            GetRequestStatus.PendingPartial => $"Stockify: Partial request - {_currentRequest.ActualQuantity}/{_currentRequest.RequestedQuantity} {_currentRequest.MaterialLabel} reserved",
            GetRequestStatus.Reserved => $"Stockify: {_currentRequest.ActualQuantity} {_currentRequest.MaterialLabel} reserved, awaiting transfer",
            GetRequestStatus.Failed => $"Stockify: Failed to reserve {_currentRequest.MaterialLabel} - insufficient materials",
            GetRequestStatus.Cancelled => "Stockify: Request cancelled",
            _ => "Stockify: Request status unknown"
        };

        Messages.Message(statusMsg, MessageTypeDefOf.PositiveEvent);

        // Close window after request is made
        Close();
    }
}