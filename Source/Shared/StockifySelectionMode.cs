namespace Stockify;

/// <summary>
/// Selection mode for shelf selection.
/// </summary>
public enum StockifySelectionMode
{
    /// <summary>
    /// Prefer the nearest eligible shelf.
    /// </summary>
    NearestFirst,

    /// <summary>
    /// Prefer the shelf with the highest priority.
    /// </summary>
    PriorityFirst,

    /// <summary>
    /// Consider distance, priority, and available capacity.
    /// </summary>
    Balanced
}