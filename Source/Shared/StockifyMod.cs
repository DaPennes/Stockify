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
    private StockifyDesignationTool _designationTool;

    public StockifyDesignationTool DesignationTool =>
        _designationTool ??= new StockifyDesignationTool();

    /// <summary>
    /// Gets or sets the main window.
    /// </summary>
    public StockifyMainWindow MainWindow { get; private set; }

    /// <summary>
    /// Gets or sets the network component for the current map.
    /// </summary>
    public StockifyMapComponent MapComponent { get; private set; }

    public StockifyMod(Verse.ModContentPack content) : base(content)
    {
        StockifyMod.Instance = this;
        this.StorageManager = new StockifyStorageManager();
        this.TransferManager = new StockifyTransferManager();
        this.MainWindow = new StockifyMainWindow();
    }
}