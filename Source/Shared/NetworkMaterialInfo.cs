namespace Stockify;

/// <summary>
/// Represents the material inventory information for a network.
/// Calculated from real physical item stacks on registered shelves.
/// </summary>
public class NetworkMaterialInfo
{
    /// <summary>
    /// The material label/def name.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Available (non-reserved) count.
    /// </summary>
    public int AvailableCount { get; private set; }

    /// <summary>
    /// Reserved count (by transfers/bills).
    /// </summary>
    public int ReservedCount { get; private set; }

    /// <summary>
    /// Total count (available + reserved).
    /// </summary>
    public int TotalCount { get; private set; }

    /// <summary>
    /// Creates a new NetworkMaterialInfo.
    /// </summary>
    /// <param name="label">The material label.</param>
    public NetworkMaterialInfo(string label)
    {
        this.Label = label;
        this.AvailableCount = 0;
        this.ReservedCount = 0;
        this.TotalCount = 0;
    }

    /// <summary>
    /// Adds a thing to the inventory count.
    /// </summary>
    /// <param name="thing">The thing to add.</param>
    public void AddItem(Thing thing)
    {
        if (thing.def == null) return;

        var label = thing.def.label;

        // Ensure we have an entry
        if (!string.Equals(this.Label, label, StringComparison.OrdinalIgnoreCase))
        {
            // This is a different material, update label
            this.Label = label;
        }

        // Increment counts
        this.AvailableCount += thing.stackCount;
        this.TotalCount += thing.stackCount;
    }

    /// <summary>
    /// Tries to reserve a quantity of this material.
    /// </summary>
    /// <param name="quantity">The quantity to reserve.</param>
    /// <returns>True if reservation was successful.</returns>
    public bool TryReserve(int quantity)
    {
        if (quantity > this.AvailableCount)
        {
            return false;
        }

        this.ReservedCount += quantity;
        this.AvailableCount -= quantity;
        return true;
    }

    /// <summary>
    /// Releases a previous reservation.
    /// </summary>
    /// <param name="quantity">The quantity to release.</param>
    public void Release(int quantity)
    {
        this.ReservedCount = Math.Max(0, this.ReservedCount - quantity);
        this.AvailableCount += quantity;
    }

    /// <summary>
    /// Gets the available quantity for display.
    /// </summary>
    /// <returns>The available count.</returns>
    public int GetAvailable() => this.AvailableCount;

    /// <summary>
    /// Gets the reserved quantity for display.
    /// </summary>
    /// <returns>The reserved count.</returns>
    public int GetReserved() => this.ReservedCount;

    /// <summary>
    /// Gets the total quantity for display.
    /// </summary>
    /// <returns>The total count.</returns>
    public int GetTotal() => this.TotalCount;
}