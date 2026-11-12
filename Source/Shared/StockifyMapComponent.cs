namespace Stockify;

/// <summary>
/// Map-specific component for managing storage networks.
/// Handles network registration, persistence, and map-specific management.
/// </summary>
public class StockifyMapComponent : Verse.MapComponent
{
    /// <summary>
    /// Gets or sets the collection of storage networks on this map.
    /// </summary>
    public List<StockifyNetwork> Networks { get; set; } = new List<StockifyNetwork>();

    /// <summary>
    /// Initializes the map component.
    /// </summary>
    /// <param name="map">The map to initialize for.</param>
    public StockifyMapComponent(Verse.Map map = null) : base(map)
    {
        // Networks will be loaded from save or created fresh
    }

    /// <summary>
    /// Called when the map is created.
    /// </summary>
    public override void FinalizeInit()
    {
        base.FinalizeInit();
        this.RebuildShelfRegistration();
    }

    /// <summary>
    /// Rebuilds shelf registration for all networks.
    /// </summary>
    public void RebuildShelfRegistration()
    {
        foreach (var network in this.Networks.ToList())
        {
            network.UpdateShelfRegistration(this.map);
        }
    }

    /// <summary>
    /// Creates a new storage network with the given name and area.
    /// </summary>
    /// <param name="name">The network name.</param>
    /// <param name="area">The designated area.</param>
    public StockifyNetwork CreateNetwork(string name, CellRect area)
    {
        var network = new StockifyNetwork(name, area);
        this.Networks.Add(network);
        return network;
    }

    /// <summary>
    /// Removes a network from the map.
    /// </summary>
    /// <param name="network">The network to remove.</param>
    public void RemoveNetwork(StockifyNetwork network)
    {
        this.Networks.Remove(network);
    }

    /// <summary>
    /// Gets a network by its unique ID.
    /// </summary>
    /// <param name="id">The network unique identifier.</param>
    /// <returns>The network, or null if not found.</returns>
    public StockifyNetwork GetNetworkByID(int id)
    {
        return this.Networks.FirstOrDefault(n => n.ID == id);
    }

    /// <summary>
    /// Gets all networks that have input enabled.
    /// </summary>
    /// <returns>List of networks with input enabled.</returns>
    public List<StockifyNetwork> GetNetworksWithInputEnabled()
    {
        return this.Networks.Where(n => n.InputEnabled).ToList();
    }

    /// <summary>
    /// Gets all networks that have output enabled.
    /// </summary>
    /// <returns>List of networks with output enabled.</returns>
    public List<StockifyNetwork> GetNetworksWithOutputEnabled()
    {
        return this.Networks.Where(n => n.OutputEnabled).ToList();
    }
}