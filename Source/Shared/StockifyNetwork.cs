namespace Stockify;

/// <summary>
/// Represents a storage network with connected shelves and material inventory.
/// </summary>
public class StockifyNetwork
{
    /// <summary>
    /// Gets the unique identifier for this network.
    /// </summary>
    public int ID { get; }

    /// <summary>
    /// Gets or sets the network name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets the designated area for this network.
    /// </summary>
    public CellRect DesignatedArea { get; }

    /// <summary>
    /// Gets the registered shelves in this network.
    /// </summary>
    public List<StockifyShelf> RegisteredShelves { get; } = new List<StockifyShelf>();

    /// <summary>
    /// Gets or sets whether GET is enabled for this network.
    /// </summary>
    public bool GetEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether PULL is enabled for this network.
    /// </summary>
    public bool PullEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether INPUT is enabled for this network.
    /// </summary>
    public bool InputEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether OUTPUT is enabled for this network.
    /// </summary>
    public bool OutputEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the storage priority for this network.
    /// </summary>
    public int StoragePriority { get; set; } = 0;

    /// <summary>
    /// Gets the material inventory overview for this network.
    /// </summary>
    public Dictionary<string, NetworkMaterialInfo> MaterialInventory { get; } = new Dictionary<string, NetworkMaterialInfo>();

    /// <summary>
    /// Initializes a new instance of the <see cref="StockifyNetwork"/> class.
    /// </summary>
    /// <param name="name">The network name.</param>
    /// <param name="area">The designated area.</public>
    public StockifyNetwork(string name, CellRect area)
    {
        this.ID = UnityEngine.Random.Range(100000, 999999);
        this.Name = name;
        this.DesignatedArea = area;
    }

    /// <summary>
    /// Registers a shelf into this network.
    /// </summary>
    /// <param name="shelf">The shelf to register.</public>
    public void RegisterShelf(StockifyShelf shelf)
    {
        if (!this.RegisteredShelves.Contains(shelf))
        {
            this.RegisteredShelves.Add(shelf);
            this.UpdateMaterialInventory();
        }
    }

    /// <summary>
    /// Unregisters a shelf from this network.
    /// </summary>
    /// <param name="shelf">The shelf to unregister.</public>
    public void UnregisterShelf(StockifyShelf shelf)
    {
        this.RegisteredShelves.Remove(shelf);
        this.UpdateMaterialInventory();
    }

    /// <summary>
    /// Updates the material inventory for this network.
    /// Recalculates all material counts from registered shelves.
    /// </summary>
    public void UpdateMaterialInventory()
    {
        this.MaterialInventory.Clear();

        foreach (var shelf in this.RegisteredShelves)
        {
            foreach (var item in shelf.StoredItems)
            {
                var def = item.def;
                var label = def.label;

                if (!this.MaterialInventory.ContainsKey(label))
                {
                    this.MaterialInventory[label] = new NetworkMaterialInfo(label);
                }

                this.MaterialInventory[label].AddItem(item);
            }
        }
    }

    /// <summary>
    /// Gets the available quantity of a specific material.
    /// </summary>
    /// <param name="materialLabel">The material label.</public>
    /// <param name="reservedOnly">Whether to count only reserved items.</public>
    /// <returns>The available quantity.</public>
    public int GetAvailableQuantity(string materialLabel, bool reservedOnly = false)
    {
        if (!this.MaterialInventory.ContainsKey(materialLabel))
        {
            return 0;
        }

        return reservedOnly
            ? this.MaterialInventory[materialLabel].ReservedCount
            : this.MaterialInventory[materialLabel].AvailableCount;
    }

    /// <summary>
    /// Gets the total quantity of a specific material (available + reserved).
    /// </summary>
    /// <param name="materialLabel">The material label.</public>
    /// <returns>The total quantity.</public>
    public int GetTotalQuantity(string materialLabel)
    {
        if (!this.MaterialInventory.ContainsKey(materialLabel))
        {
            return 0;
        }

        return this.MaterialInventory[materialLabel].TotalCount;
    }

    /// <summary>
    /// Attempts to reserve materials for a transfer.
    /// </summary>
    /// <param name="materialLabel">The material label.</public>
    /// <param name="quantity">The quantity to reserve.</public>
    /// <returns>True if reservation was successful.</public>
    public bool TryReserveMaterials(string materialLabel, int quantity)
    {
        if (!this.MaterialInventory.ContainsKey(materialLabel))
        {
            return false;
        }

        var info = this.MaterialInventory[materialLabel];
        return info.TryReserve(quantity);
    }

    /// <summary>
    /// Releases a previous material reservation.
    /// </summary>
    /// <param name="materialLabel">The material label.</public>
    /// <param name="quantity">The quantity to release.</public>
    public void ReleaseReservation(string materialLabel, int quantity)
    {
        if (!this.MaterialInventory.ContainsKey(materialLabel))
        {
            return;
        }

        this.MaterialInventory[materialLabel].Release(quantity);
    }

    /// <summary>
    /// Performs a transfer of materials from this network.
    /// </summary>
    /// <param name="materialLabel">The material label.</public>
    /// <param name="quantity">The quantity to transfer.</public>
    /// <param name="sourceShelf">The source shelf.</public>
    /// <param name="destination">The destination for the transfer.</public>
    /// <returns>True if the transfer was successful.</public>
    public bool TryTransferMaterials(string materialLabel, int quantity, StockifyShelf sourceShelf, StockifyDestination destination)
    {
        if (!this.MaterialInventory.ContainsKey(materialLabel))
        {
            return false;
        }

        var info = this.MaterialInventory[materialLabel];

        // Check if we have enough available (non-reserved) materials
        if (info.AvailableCount < quantity)
        {
            return false;
        }

        // Reserve the materials
        if (!info.TryReserve(quantity))
        {
            return false;
        }

        // Execute the transfer
        return sourceShelf.TryTransferToDestination(info, quantity, destination);
    }
}