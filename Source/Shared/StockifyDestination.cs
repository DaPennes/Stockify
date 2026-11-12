namespace Stockify;

/// <summary>
/// Represents a destination for material transfers.
/// Can be a shelf within a network or a workbench-accessible location.
/// </summary>
public class StockifyDestination
{
    /// <summary>
    /// The thing at the destination location.
    /// </summary>
    public Thing Thing { get; }

    public Map Map { get; }

    public IntVec3 Position { get; }

    /// <summary>
    /// The label/name of the destination.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// The network this destination belongs to (if any).
    /// </summary>
    public StockifyNetwork Network { get; set; }

    /// <summary>
    /// Creates a new destination from a thing.
    /// </summary>
    /// <param name="thing">The thing at the destination location.</param>
    public StockifyDestination(Thing thing)
    {
        this.Thing = thing;
        this.Map = thing?.Map;
        this.Position = thing?.Position ?? IntVec3.Invalid;
        this.Label = thing?.Label ?? "Unknown Destination";
        this.Network = null;
    }

    public StockifyDestination(Map map, IntVec3 position, string label = "Map location")
    {
        this.Map = map;
        this.Position = position;
        this.Label = label;
    }

    /// <summary>
    /// Creates a destination from a shelf.
    /// </summary>
    /// <param name="shelf">The shelf to use as destination.</param>
    public StockifyDestination(StockifyShelf shelf)
    {
        this.Thing = shelf.Thing;
        this.Map = shelf.Thing.Map;
        this.Position = shelf.Thing.Position;
        this.Label = shelf.Thing.Label;
        this.Network = shelf.Network;
    }

    /// <summary>
    /// Converts to string for display.
    /// </summary>
    /// <returns>Destination label.</returns>
    public override string ToString()
    {
        return Label;
    }

    /// <summary>
    /// Checks if this destination is valid for transfers.
    /// </summary>
    /// <returns>True if the destination is valid.</returns>
    public bool IsValid()
    {
        return Map != null && Position.InBounds(Map) && (Thing == null || !Thing.Destroyed);
    }
}