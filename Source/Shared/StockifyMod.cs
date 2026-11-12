namespace Stockify;

/// <summary>
/// Main mod class for Stockify.
/// Handles mod initialization, Harmony patches, and settings management.
/// </summary>
public class StockifyMod : Verse.Mod
{
    /// <summary>
    /// Gets the mod instance.
    /// </summary>
    public static StockifyMod Instance { get; private set; }

    /// <summary>
    /// Gets or sets the storage network manager.
    /// </summary>
    public StockifyStorageManager StorageManager { get; private set; }

    /// <summary>
    /// Gets or sets the transfer manager.
    /// </summary>
    public StockifyTransferManager TransferManager { get; private set; }

    /// <summary>
    /// Gets or sets the designation tool.
    /// </summary>
    public StockifyDesignationTool DesignationTool { get; private set; }

    /// <summary>
    /// Gets or sets the main window.
    /// </summary>
    public StockifyMainWindow MainWindow { get; private set; }

    /// <summary>
    /// Gets or sets the network component for the current map.
    /// </summary>
    public StockifyMapComponent MapComponent { get; private set; }

    /// <summary>
    /// Initializes the mod.
    /// </summary>
    /// <param name="content">The mod content.</param>
    public override void Init()
    {
        StockifyMod.Instance = this;
        this.StorageManager = new StockifyStorageManager();
        this.TransferManager = new StockifyTransferManager();
        this.DesignationTool = new StockifyDesignationTool();
        this.MapComponent = Find.WorldObjects.TryGetComponent<StockifyMapComponent>();

        if (this.MapComponent == null)
        {
            this.MapComponent = new StockifyMapComponent();
            Find.WorldObjects.components.Add(this.MapComponent);
        }

        this.MainWindow = new StockifyMainWindow();
        base.Init();
    }

    /// <summary>
    /// Called when the world is loading.
    /// </summary>
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe.Look(this.StorageManager, "storage_manager");
        Scribe.Look(this.TransferManager, "transfer_manager");
        Scribe.Look(this.DesignationTool, "designation_tool");
        Scribe.Look(this.MainWindow, "main_window");
    }
}