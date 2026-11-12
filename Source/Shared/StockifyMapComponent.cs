namespace Stockify;

/// <summary>
/// Map-specific component for managing storage networks.
/// Handles network registration, persistence, and map-specific management.
/// </summary>
public class StockifyMapComponent : Verse.MapComponent
{
    private static readonly Color[] AreaColors =
    {
        new(0.35f, 0.8f, 1f, 0.9f),
        new(1f, 0.75f, 0.25f, 0.9f),
        new(0.7f, 0.45f, 1f, 0.9f),
        new(0.45f, 1f, 0.55f, 0.9f)
    };

    private List<List<IntVec3>> _overlayCellGroups = new();
    private bool _overlayCacheDirty = true;

    /// <summary>
    /// Gets or sets the collection of storage networks on this map.
    /// </summary>
    public List<StockifyNetwork> Networks { get; private set; } = new List<StockifyNetwork>();
    public List<CompStockifyShelf> LocalShelves { get; private set; } = new List<CompStockifyShelf>();

    /// <summary>
    /// Initializes the map component.
    /// </summary>
    /// <param name="map">The map to initialize for.</param>
    public StockifyMapComponent(Verse.Map map) : base(map)
    {
    }

    public override void ExposeData()
    {
        base.ExposeData();

        var networks = Networks;
        Scribe_Collections.Look(ref networks, "stockifyNetworks", LookMode.Deep);
        Networks = networks ?? new List<StockifyNetwork>();
    }

    /// <summary>
    /// Called when the map is created.
    /// </summary>
    public override void FinalizeInit()
    {
        base.FinalizeInit();
        foreach (var network in Networks)
            network.AttachToMap(map);
        RebuildLocalShelfRegistration();
        this.RebuildShelfRegistration();
    }

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        if (Find.TickManager.TicksGame % 250 != 0) return;

        RebuildLocalShelfRegistration();
        foreach (var network in Networks)
        {
            network.AttachToMap(map);
            network.UpdateShelfRegistration(map);
            network.UpdateMaterialInventory(map);
        }
    }

    public override void MapComponentDraw()
    {
        base.MapComponentDraw();
        if (_overlayCacheDirty)
        {
            _overlayCellGroups = Networks
                .Select((network, index) => network.DesignatedCells
                    .Where(cell => cell.InBounds(map) && network.ContainsCell(cell, map))
                    .ToList())
                .ToList();
            _overlayCacheDirty = false;
        }

        for (var index = 0; index < _overlayCellGroups.Count; index++)
        {
            var cells = _overlayCellGroups[index];
            if (cells.Count > 0)
                GenDraw.DrawFieldEdges(cells, AreaColors[index % AreaColors.Length]);
        }
    }

    public void RebuildLocalShelfRegistration()
    {
        LocalShelves = map.listerThings.AllThings
            .Select(thing => thing.TryGetComp<CompStockifyShelf>())
            .Where(comp => comp != null && !comp.parent.Destroyed)
            .ToList();
    }

    /// <summary>
    /// Rebuilds shelf registration for all networks.
    /// </summary>
    public void RebuildShelfRegistration()
    {
        _overlayCacheDirty = true;
        foreach (var network in this.Networks.ToList())
        {
            network.AttachToMap(map);
            network.UpdateShelfRegistration(this.map);
            network.UpdateMaterialInventory(this.map);
        }
    }

    /// <summary>
    /// Creates a new storage network with the given name and area.
    /// </summary>
    /// <param name="name">The network name.</param>
    /// <param name="area">The designated area.</param>
    public StockifyNetwork CreateNetwork(string name, CellRect area)
    {
        return CreateNetwork(name, area.Cells);
    }

    public StockifyNetwork CreateNetwork(string name, IEnumerable<IntVec3> designatedCells)
    {
        var network = new StockifyNetwork(name, designatedCells);
        while (Networks.Any(existing => existing.ID == network.ID))
        {
            network = new StockifyNetwork(name, designatedCells);
        }

        network.AttachToMap(map);
        this.Networks.Add(network);
        RebuildShelfRegistration();
        return network;
    }

    /// <summary>
    /// Removes a network from the map.
    /// </summary>
    /// <param name="network">The network to remove.</param>
    public void RemoveNetwork(StockifyNetwork network)
    {
        if (this.Networks.Remove(network))
        {
            foreach (var shelf in network.RegisteredShelves.ToList())
            {
                network.UnregisterShelf(shelf);
            }

            foreach (var localShelf in LocalShelves)
            {
                localShelf.SourceNetworkIds.Remove(network.ID);
                foreach (var request in localShelf.ManualRequests)
                    request.SourceNetworkIds.Remove(network.ID);
                localShelf.ManualRequests.RemoveAll(request => request.SourceNetworkIds.Count == 0);
            }

            RebuildShelfRegistration();
        }
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