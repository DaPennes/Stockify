namespace Stockify;

/// <summary>
/// Main Stockify window - provides access to all network functions.
/// Includes GET, PULL, INPUT, OUTPUT toggles and network overview.
/// </summary>
public class StockifyMainWindow : Verse.Window
{
    private StockifyMapComponent _mapComponent;
    private StockifyNetwork _selectedNetwork;
    private int _tabIndex;
    private string _newNetworkName;
    private bool _renameMode;

    // Tab indices: 0=Network, 1=GET, 2=PULL, 3=INPUT, 4=OUTPUT
    private const int TAB_NETWORK = 0;
    private const int TAB_GET = 1;
    private const int TAB_PULL = 2;
    private const int TAB_INPUT = 3;
    private const int TAB_OUTPUT = 4;

    /// <summary>
    /// Creates the main Stockify window.
    /// </summary>
    public StockifyMainWindow() : base(
        new Rect(0f, 0f, 800f, 600f), "Stockify", WindowLayer.Super,
        doCloseOnCancel: true, closeButtonLimit: 1, absorbInputAroundWindow: true)
    {
    }

    /// <summary>
    /// Called when the window is opened.
    /// </summary>
    public override void PreOpen()
    {
        base.PreOpen();
        _mapComponent = Find.WorldObjects.GetComponent<StockifyMapComponent>();

        // Select first network if none selected
        if (_mapComponent != null && _mapComponent.Networks.Count > 0)
        {
            _selectedNetwork = _mapComponent.Networks[0];
        }
    }

    /// <summary>
    /// Called to do the window GUI.
    /// </summary>
    public override void DoWindowContents(Rect inRect)
    {
        var listingStandard = new Verse.Listing_Standard();
        listingStandard.Begin(inRect);

        // Header
        DrawHeader(listingStandard);

        // Tab navigation
        DrawTabs(listingStandard);

        // Content based on selected tab
        DrawContent(listingStandard);

        listingStandard.End();
    }

    /// <summary>
    /// Draws the window header with network info.
    /// </summary>
    /// <param name="listingStandard">The listing standard.</param>
    private void DrawHeader(Verse.Listing_Standard listingStandard)
    {
        listingStandard.GapLine(4);
        var headerRect = new Rect(10f, listingStandard.CurHeight, inRect.Width - 20f, 30f);

        if (_selectedNetwork != null)
        {
            GUI.color = Color.cyan;
            GUI.Label(headerRect, $"Network: {_selectedNetwork.Name} (ID: {_selectedNetwork.ID})");
            GUI.color = Color.white;

            // Display toggle switches
            DrawToggleSwitches(listingStandard);
        }
        else
        {
            GUI.Label(headerRect, "No network selected. Drag designate area to create one.");
        }

        listingStandard.GapLine(6);
    }

    /// <summary>
    /// Draws the four independent toggle switches.
    /// </summary>
    /// <param name="listingStandard">The listing standard.</param>
    private void DrawToggleSwitches(Verse.Listing_Standard listingStandard)
    {
        var toggleY = listingStandard.CurHeight;
        var toggleWidth = (inRect.Width - 30f) / 4f - 5f;
        var toggleHeight = 30f;

        // GET toggle
        var getRect = new Rect(10f, toggleY, toggleWidth, toggleHeight);
        bool getSelected = _selectedNetwork != null && _selectedNetwork.GetEnabled;
        if (Verse.Widgets.ToggleButton(getRect, "GET".Translate(), getSelected, out bool getResult))
        {
            _selectedNetwork.GetEnabled = getResult;
            Messages.Message(getResult ? $"Stockify GET enabled for network {_selectedNetwork.Name}" : $"Stockify GET disabled for network {_selectedNetwork.Name}", MessageTypeDefOf.PositiveEvent);
        }

        // PULL toggle
        var pullRect = new Rect(10f + toggleWidth + 5f, toggleY, toggleWidth, toggleHeight);
        bool pullSelected = _selectedNetwork != null && _selectedNetwork.PullEnabled;
        if (Verse.Widgets.ToggleButton(pullRect, "PULL".Translate(), pullSelected, out bool pullResult))
        {
            _selectedNetwork.PullEnabled = pullResult;
            Messages.Message(pullResult ? $"Stockify PULL enabled for network {_selectedNetwork.Name}" : $"Stockify PULL disabled for network {_selectedNetwork.Name}", MessageTypeDefOf.PositiveEvent);
        }

        // INPUT toggle
        var inputRect = new Rect(10f + (toggleWidth + 5f) * 2, toggleY, toggleWidth, toggleHeight);
        bool inputSelected = _selectedNetwork != null && _selectedNetwork.InputEnabled;
        if (Verse.Widgets.ToggleButton(inputRect, "INPUT".Translate(), inputSelected, out bool inputResult))
        {
            _selectedNetwork.InputEnabled = inputResult;
            Messages.Message(inputResult ? $"Stockify INPUT enabled for network {_selectedNetwork.Name}" : $"Stockify INPUT disabled for network {_selectedNetwork.Name}", MessageTypeDefOf.PositiveEvent);
        }

        // OUTPUT toggle
        var outputRect = new Rect(10f + (toggleWidth + 5f) * 3, toggleY, toggleWidth, toggleHeight);
        bool outputSelected = _selectedNetwork != null && _selectedNetwork.OutputEnabled;
        if (Verse.Widgets.ToggleButton(outputRect, "OUTPUT".Translate(), outputSelected, out bool outputResult))
        {
            _selectedNetwork.OutputEnabled = outputResult;
            Messages.Message($"Stockify OUTPUT {(outputResult ? "enabled" : "disabled")} for network {_selectedNetwork.Name}", MessageTypeDefOf.PositiveEvent);
        }

        listingStandard.CurHeight = toggleY + toggleHeight + 10f;
    }

    /// <summary>
    /// Draws the tab navigation bar.
    /// </summary>
    /// <param name="listingStandard">The listing standard.</param>
    private void DrawTabs(Verse.Listing_Standard listingStandard)
    {
        var tabHeight = 30f;
        var tabWidth = (inRect.Width - 20f) / 5f;
        var tabs = new[] { "Network", "GET", "PULL", "INPUT", "OUTPUT" };
        for (int i = 0; i < tabs.Length; i++)
        {
            var tabRect = new Rect(10f + tabWidth * i, listingStandard.CurHeight, tabWidth, tabHeight);
            if (Verse.Widgets.ButtonText(tabRect, tabs[i]))
            {
                OnTabChanged(i);
            }
        }

        var tabsRect = new Rect(10f, listingStandard.CurHeight, inRect.Width - 20f, tabHeight);
        listingStandard.CurHeight = tabsRect.yMax + 5f;
    }

    /// <summary>
    /// Called when a tab is changed.
    /// </summary>
    /// <param name="index">The new tab index.</param>
    private void OnTabChanged(int index)
    {
        _tabIndex = index;
        // Switch content based on tab
    }

    /// <summary>
    /// Draws content based on the selected tab.
    /// </summary>
    /// <param name="listingStandard">The listing standard.</param>
    private void DrawContent(Verse.Listing_Standard listingStandard)
    {
        switch (_tabIndex)
        {
            case TAB_NETWORK:
                DrawNetworkTab(listingStandard);
                break;
            case TAB_GET:
                DrawGETTab(listingStandard);
                break;
            case TAB_PULL:
                DrawPULLTab(listingStandard);
                break;
            case TAB_INPUT:
                DrawInputTab(listingStandard);
                break;
            case TAB_OUTPUT:
                DrawOutputTab(listingStandard);
                break;
        }
    }

    /// <summary>
    /// Draws the network management tab.
    /// </summary>
    /// <param name="listingStandard">The listing standard.</param>
    private void DrawNetworkTab(Verse.Listing_Standard listingStandard)
    {
        listingStandard.GapLine(4);

        // Network info - cached display
        if (_selectedNetwork != null)
        {
            var infoRect = new Rect(10f, listingStandard.CurHeight, inRect.Width - 20f, 50f);
            GUI.Label(infoRect, $"Name: {_selectedNetwork.Name}");
            GUI.Label(infoRect, $"Shelves: {_selectedNetwork.RegisteredShelves.Count}");

            // Cache inventory display - only recalculate when needed
            var inventory = StockifyInventoryManager.GetAllMaterials(_selectedNetwork);
            GUI.Label(infoRect, $"Materials: {inventory.Count} ({(inventory.Count > 0 ? inventory[0].Total + " total" : "empty")})");

            listingStandard.GapLine(4);
        }

        // Network controls
        var controlY = listingStandard.CurHeight;

        // Expand area button
        var expandRect = new Rect(10f, controlY, inRect.Width - 20f, 30f);
        if (Verse.Widgets.ButtonText(expandRect, "Expand Area"))
        {
            // Would open designation tool
            Messages.Message("Stockify: Expand area selected - use designation tool", MessageTypeDefOf.PositiveEvent);
        }

        // Rename button
        var renameY = controlY + 35f;
        var renameRect = new Rect(10f, renameY, inRect.Width - 20f, 30f);
        if (Verse.Widgets.ButtonText(renameRect, "Rename Network"))
        {
            _newNetworkName = _selectedNetwork.Name;
            _renameMode = true;
            Messages.Message("Stockify: Rename mode entered", MessageTypeDefOf.PositiveEvent);
        }

        // Clear network designation
        var clearY = renameY + 35f;
        var clearRect = new Rect(10f, clearY, inRect.Width - 20f, 30f);
        if (Verse.Widgets.ButtonText(clearRect, "Clear Network Designation"))
        {
            // Would clear the network designation
            Messages.Message("Stockify: Network designation cleared", MessageTypeDefOf.NegativeEvent);
        }

        // Delete network
        var deleteY = clearY + 35f;
        var deleteRect = new Rect(10f, deleteY, inRect.Width - 20f, 30f);
        if (Verse.Widgets.ButtonText(deleteRect, "Delete Network"))
        {
            // Would delete the network
            Messages.Message("Stockify: Delete network - under development", MessageTypeDefOf.NegativeEvent);
        }

        listingStandard.GapLine(6);
    }

    /// <summary>
    /// Draws the GET tab.
    /// </summary>
    /// <param name="listingStandard">The listing standard.</param>
    private void DrawGETTab(Verse.Listing_Standard listingStandard)
    {
        if (_selectedNetwork == null)
        {
            GUI.Label(listingStandard.GetRect(200f), "No network selected");
            return;
        }

        // Use cached inventory - calculate once per frame
        var inventory = StockifyInventoryManager.GetAllMaterials(_selectedNetwork);
        var inventoryCount = inventory.Count;

        // Material search and request
        var searchRect = new Rect(10f, listingStandard.CurHeight, inRect.Width - 20f, 50f);
        var searchLabel = $"Material: {_selectedNetwork.Name}";
        GUI.Label(searchRect, searchLabel);

        listingStandard.GapLine(4);

        // Available materials list with optimized display
        var scrollRect = new Rect(10f, listingStandard.CurHeight, inRect.Width - 20f, 200f);

        // Draw material entries - optimized loop
        var viewRect = new Rect(10f, listingStandard.CurHeight, inRect.Width - 30f, 180f);

        listingStandard.BeginScrollView(scrollRect, ref listingStandard.scrollPosition, viewRect, true);

        // Show summary line if no materials
        if (inventoryCount == 0)
        {
            GUI.Label(new Rect(15f, listingStandard.CurHeight, inRect.Width - 35f, 30f), "No materials in network");
        }
        else
        {
            // Draw up to 8 materials, then show " +N more"
            var materialsToShow = Math.Min(inventoryCount, 8);
            for (int i = 0; i < materialsToShow; i++)
            {
                var material = inventory[i];
                var materialRect = new Rect(15f, listingStandard.CurHeight, inRect.Width - 35f, 30f);

                if (Verse.Widgets.ButtonText(materialRect, $"{material.MaterialLabel}: {material.Available} avl/{material.Total} total"))
                {
                    // Select this material for GET request
                    Messages.Message($"Stockify: Selected {material.MaterialLabel} for request", MessageTypeDefOf.PositiveEvent);
                }

                listingStandard.GapLine(2);
            }

            if (inventoryCount > 8)
            {
                GUI.Label(new Rect(15f, listingStandard.CurHeight, inRect.Width - 35f, 30f), $"+{inventoryCount - 8} more materials");
            }
        }

        listingStandard.EndScrollView();

        listingStandard.GapLine(4);

        // Request quantity
        var quantityRect = new Rect(10f, listingStandard.CurHeight, inRect.Width - 20f, 30f);
        GUI.Label(quantityRect, $"Request quantity: (auto-set to available: {(inventory.Count > 0 ? inventory[0].Available + " available" : "0 available")})");

        listingStandard.GapLine(4);

        // Request button
        var requestRect = new Rect(10f, listingStandard.CurHeight, inRect.Width - 20f, 30f);
        if (Verse.Widgets.ButtonText(requestRect, "Request Materials"))
        {
            // Open GET request window
            var getWindow = new StockifyGetWindow(_selectedNetwork);
            Find.WindowStack.Add(getWindow);
        }
    }

    /// <summary>
    /// Draws the PULL tab.
    /// </summary>
    /// <param name="listingStandard">The listing standard.</param>
    private void DrawPULLTab(Verse.Listing_Standard listingStandard)
    {
        if (_selectedNetwork == null)
        {
            GUI.Label(listingStandard.GetRect(200f), "No network selected");
            return;
        }

        GUI.Label(listingStandard.GetRect(30f), $"Network: {_selectedNetwork.Name}");

        // PULL toggle
        var pullLabel = $"PULL: {( _selectedNetwork.PullEnabled ? "Enabled" : "Disabled")}";
        if (Verse.Widgets.ToggleButton(new Rect(10f, listingStandard.CurHeight, inRect.Width - 20f, 30f), pullLabel, _selectedNetwork.PullEnabled, out bool pullResult))
        {
            _selectedNetwork.PullEnabled = pullResult;
            Messages.Message(pullResult ? $"Stockify PULL enabled for network {_selectedNetwork.Name}" : $"Stockify PULL disabled for network {_selectedNetwork.Name}", MessageTypeDefOf.PositiveEvent);
        }
        listingStandard.GapLine(4);

        // Source networks configuration
        var scrollRect = new Rect(10f, listingStandard.CurHeight, inRect.Width - 20f, 200f);

        // Get map component for network listing
        var mapComponent = Find.WorldObjects.GetComponent<StockifyMapComponent>();

        listingStandard.BeginScrollView(scrollRect, ref listingStandard.scrollPosition, new Rect(10f, listingStandard.CurHeight, inRect.Width - 30f, 160f), true);

        if (mapComponent != null && mapComponent.NETworks.Count > 0)
        {
            bool anyHasMaterials = false;

            foreach (var network in mapComponent.NETworks)
            {
                // Check if network has any materials available
                var steelAvail = StockifyInventoryManager.GetAvailableQuantity(network, "Steel");
                var woodAvail = StockifyInventoryManager.GetAvailableQuantity(network, "Wood");
                var hasMaterials = steelAvail > 0 || woodAvail > 0;
                anyHasMaterials |= hasMaterials;

                var networkRect = new Rect(15f, listingStandard.CurHeight, inRect.Width - 35f, 25f);

                GUI.Label(networkRect, $"• {network.Name}: PULL={(network.PullEnabled ? "On" : "Off")}, Materials={(hasMaterials ? $"Steel:{steelAvail}, Wood:{woodAvail}" : "Empty")}");
                listingStandard.GapLine(2);
            }

            // Overall status
            if (anyHasMaterials)
            {
                GUI.Label(new Rect(15f, listingStandard.CurHeight, inRect.Width - 35f, 25f), "<color=green>Some networks have available materials</color>");
            }
            else
            {
                GUI.Label(new Rect(15f, listingStandard.CurHeight, inRect.Width - 35f, 25f), "<color=red>No networks have available materials</color>");
            }
        }
        else
        {
            GUI.Label(new Rect(15f, listingStandard.CurHeight, inRect.Width - 35f, 25f), "No networks configured");
        }

        listingStandard.EndScrollView();
        listingStandard.GapLine(4);

        // Workbench configuration instruction
        GUI.Label(listingStandard.GetRect(50f), $"Workbenches with Stockify PULL enabled can draw ingredients from this network.");
        listingStandard.GapLine(4);

        // Configure button
        if (Verse.Widgets.ButtonText(new Rect(10f, listingStandard.CurHeight, inRect.Width - 20f, 30f),
            "Configure Workbench PULL"))
        {
            Messages.Message("Stockify: Open workbench and enable Stockify PULL in its settings to connect to this network", MessageTypeDefOf.PositiveEvent);
        }
    }

    /// <summary>
    /// Draws the INPUT tab.
    /// </summary>
    /// <param name="listingStandard">The listing standard.</param>
    private void DrawInputTab(Verse.Listing_Standard listingStandard)
    {
        if (_selectedNetwork == null)
        {
            GUI.Label(listingStandard.GetRect(200f), "No network selected");
            return;
        }

        GUI.Label(listingStandard.GetRect(30f), $"Network: {_selectedNetwork.Name}");

        // INPUT toggle
        var inputLabel = $"INPUT: {( _selectedNetwork.InputEnabled ? "Enabled" : "Disabled")}";
        if (Verse.Widgets.ToggleButton(new Rect(10f, listingStandard.CurHeight, inRect.Width - 20f, 30f), inputLabel, _selectedNetwork.InputEnabled, out bool inputResult))
        {
            _selectedNetwork.InputEnabled = inputResult;
            Messages.Message(inputResult ? $"Stockify INPUT enabled for network {_selectedNetwork.Name}" : $"Stockify INPUT disabled for network {_selectedNetwork.Name}", MessageTypeDefOf.PositiveEvent);
        }
        listingStandard.GapLine(4);

        // Input settings - optimized with cached availability
        var mapComponent = Find.WorldObjects.GetComponent<StockifyMapComponent>();
        var steelAvail = mapComponent != null ? StockifyInventoryManager.GetAvailableQuantity(_selectedNetwork, "Steel") : 0;

        var scrollRect = new Rect(10f, listingStandard.CurHeight, inRect.Width - 20f, 200f);

        listingStandard.BeginScrollView(scrollRect, ref listingStandard.scrollPosition, new Rect(10f, listingStandard.CurHeight, inRect.Width - 30f, 160f), true);

        if (mapComponent != null && mapComponent.NETworks.Count > 0)
        {
            // Show input status per network (focus on selected network)
            foreach (var network in new[] { _selectedNetwork })
            {
                var inputStatus = network.InputEnabled ? "Enabled" : "Disabled";

                // Use cached steel availability
                var netSteel = StockifyInventoryManager.GetAvailableQuantity(network, "Steel");
                var inputLabel = $"• {network.Name}: INPUT={inputStatus}";

                if (netSteel > 0 || network.InputEnabled)
                {
                    inputLabel += $", Steel:{netSteel}";
                }

                var networkRect = new Rect(15f, listingStandard.CurHeight, inRect.Width - 35f, 25f);
                GUI.Label(networkRect, inputLabel);
                listingStandard.GapLine(2);
            }

            // Input instructions
            if (listingStandard.CurHeight < 500f)
            {
                GUI.Label(new Rect(15f, listingStandard.CurHeight, inRect.Width - 35f, 50f),
                    "Colonists will automatically accept hauled materials into this network " +
                    "when INPUT is enabled. Respects shelf filters and capacity limits.");
            }
        }
        else
        {
            GUI.Label(new Rect(15f, listingStandard.CurHeight, inRect.Width - 35f, 25f), "No networks configured");
        }

        listing.Standard.EndScrollView();
        listingStandard.GapLine(4);

        // Input toggle explanation
        GUI.Label(listingStandard.GetRect(50f),
            "When INPUT is enabled: colonists automatically accept compatible hauled materials " +
            "into the network, respecting shelf filters and storage capacity.");
    }

    /// <summary>
    /// Draws the OUTPUT tab.
    /// </summary>
    /// <param name="listingStandard">The listing standard.</param>
    private void DrawOutputTab(Verse.Listing_Standard listingStandard)
    {
        if (_selectedNetwork == null)
        {
            GUI.Label(listingStandard.GetRect(200f), "No network selected");
            return;
        }

        GUI.Label(listingStandard.GetRect(30f), $"Network: {_selectedNetwork.Name}");

        // OUTPUT toggle
        var outputLabel = $"OUTPUT: {( _selectedNetwork.OutputEnabled ? "Enabled" : "Disabled")}";
        if (Verse.Widgets.ToggleButton(new Rect(10f, listingStandard.CurHeight, inRect.Width - 20f, 30f), outputLabel, _selectedNetwork.OutputEnabled, out bool outputResult))
        {
            _selectedNetwork.OutputEnabled = outputResult;
            Messages.Message($"Stockify OUTPUT {(outputResult ? "enabled" : "disabled")} for network {_selectedNetwork.Name}", MessageTypeDefOf.PositiveEvent);
        }
        listingStandard.GapLine(4);

        // Output settings - optimized with shelf count
        var mapComponent = Find.WorldObjects.GetComponent<StockifyMapComponent>();
        var shelfCount = _selectedNetwork != null ? _selectedNetwork.RegisteredShelves.Count : 0;

        var scrollRect = new Rect(10f, listingStandard.CurHeight, inRect.Width - 20f, 200f);

        listingStandard.BeginScrollView(scrollRect, ref listingStandard.scrollPosition, new Rect(10f, listingStandard.CurHeight, inRect.Width - 30f, 160f), true);

        if (mapComponent != null && mapComponent.NETworks.Count > 0)
        {
            // Show output status per network (focus on selected)
            foreach (var network in new[] { _selectedNetwork })
            {
                var outputStatus = network.OutputEnabled ? "Enabled" : "Disabled";

                var outputLabel = $"• {network.Name}: OUTPUT={outputStatus}, Shelves:{shelfCount}";

                var networkRect = new Rect(15f, listingStandard.CurHeight, inRect.Width - 35f, 25f);
                GUI.Label(networkRect, outputLabel);
                listingStandard.GapLine(2);
            }

            // Output instructions
            if (listingStandard.CurHeight < 500f)
            {
                GUI.Label(new Rect(15f, listingStandard.CurHeight, inRect.Width - 35f, 50f),
                    "When OUTPUT is enabled: workbench production completion sends finished items to " +
                    "this network's shelves. Items are transferred using vanilla storage mechanics.");
            }
        }
        else
        {
            GUI.Label(new Rect(15f, listingStandard.CurHeight, inRect.Width - 35f, 25f), "No networks configured");
        }

        listing.Standard.EndScrollView();
        listingStandard.GapLine(4);

        // Output toggle explanation
        GUI.Label(listingStandard.GetRect(50f),
            "When OUTPUT is enabled: workbench production completion sends finished items to " +
            "this network's shelves. Items are transferred using vanilla storage mechanics.");
    }

    /// <summary>
    /// Called when the window is removed.
    /// </summary>
    public override void PostClose()
    {
        base.PostClose();
    }
}