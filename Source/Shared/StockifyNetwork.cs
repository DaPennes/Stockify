namespace Stockify;

/// <summary>
/// Represents a storage network with connected shelves and material inventory.
/// </summary>
public class StockifyNetwork : IExposable
{
    /// <summary>
    /// Gets the unique identifier for this network.
    /// </summary>
    public int ID { get; private set; }

    /// <summary>
    /// Gets or sets the network name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets the designated area for this network.
    /// </summary>
    public CellRect DesignatedArea { get; set; }

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

    public List<string> IngredientFilters { get; } = new List<string>();
    public List<int> ExcludedShelfThingIds { get; } = new List<int>();

    /// <summary>
    /// Gets or sets the storage priority for this network.
    /// </summary>
    public int StoragePriority { get; set; } = 0;

    public bool AllowsMaterial(Thing thing)
    {
        return thing?.def != null &&
               !IngredientFilters.Contains(thing.def.defName);
    }

    /// <summary>
    /// Gets the material inventory overview for this network.
    /// </summary>
    public Dictionary<string, NetworkMaterialInfo> MaterialInventory { get; } = new Dictionary<string, NetworkMaterialInfo>();

    /// <summary>
    /// Initializes a new instance of the <see cref="StockifyNetwork"/> class.
    /// </summary>
    /// <param name="name">The network name.</param>
    /// <param name="area">The designated area.</param>
    public StockifyNetwork(string name, CellRect area)
    {
        this.ID = UnityEngine.Random.Range(100000, 999999);
        this.Name = name;
        this.DesignatedArea = area;
    }

    public StockifyNetwork() : this("", CellRect.Empty)
    {
    }

    public void ExposeData()
    {
        var id = ID;
        var name = Name;
        var minX = DesignatedArea.minX;
        var minZ = DesignatedArea.minZ;
        var maxX = DesignatedArea.maxX;
        var maxZ = DesignatedArea.maxZ;
        var getEnabled = GetEnabled;
        var pullEnabled = PullEnabled;
        var inputEnabled = InputEnabled;
        var outputEnabled = OutputEnabled;
        var storagePriority = StoragePriority;
        var ingredientFilters = IngredientFilters;
        var excludedShelfThingIds = ExcludedShelfThingIds;

        Scribe_Values.Look(ref id, "id");
        Scribe_Values.Look(ref name, "name");
        Scribe_Values.Look(ref minX, "areaMinX");
        Scribe_Values.Look(ref minZ, "areaMinZ");
        Scribe_Values.Look(ref maxX, "areaMaxX");
        Scribe_Values.Look(ref maxZ, "areaMaxZ");
        Scribe_Values.Look(ref getEnabled, "getEnabled", true);
        Scribe_Values.Look(ref pullEnabled, "pullEnabled", true);
        Scribe_Values.Look(ref inputEnabled, "inputEnabled", true);
        Scribe_Values.Look(ref outputEnabled, "outputEnabled", true);
        Scribe_Values.Look(ref storagePriority, "storagePriority");
        Scribe_Collections.Look(ref ingredientFilters, "ingredientFilters", LookMode.Value);
        Scribe_Collections.Look(ref excludedShelfThingIds, "excludedShelfThingIds", LookMode.Value);

        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            ID = id;
            Name = name ?? "";
            DesignatedArea = CellRect.FromLimits(minX, minZ, maxX, maxZ);
            GetEnabled = getEnabled;
            PullEnabled = pullEnabled;
            InputEnabled = inputEnabled;
            OutputEnabled = outputEnabled;
            StoragePriority = storagePriority;
            IngredientFilters.Clear();
            if (ingredientFilters != null)
            {
                IngredientFilters.AddRange(ingredientFilters);
            }
            ExcludedShelfThingIds.Clear();
            if (excludedShelfThingIds != null)
            {
                ExcludedShelfThingIds.AddRange(excludedShelfThingIds);
            }

            RegisteredShelves.Clear();
            MaterialInventory.Clear();
        }
    }

    /// <summary>
    /// Registers a shelf into this network.
    /// </summary>
    /// <param name="shelf">The shelf to register.</param>
    public void RegisterShelf(StockifyShelf shelf)
    {
        if (!this.RegisteredShelves.Contains(shelf))
        {
            this.RegisteredShelves.Add(shelf);
            shelf.Network = this;
            this.UpdateMaterialInventory();
        }
    }

    /// <summary>
    /// Unregisters a shelf from this network.
    /// </summary>
    /// <param name="shelf">The shelf to unregister.</param>
    public void UnregisterShelf(StockifyShelf shelf)
    {
        if (this.RegisteredShelves.Remove(shelf) && shelf.Network == this)
        {
            shelf.Network = null;
        }
        this.UpdateMaterialInventory();
    }

    public void UpdateShelfRegistration(Map map)
    {
        if (map == null) return;

        var networks = map.GetComponent<StockifyMapComponent>()?.Networks;
        var earlierNetworks = networks == null
            ? new List<StockifyNetwork>()
            : networks.TakeWhile(network => network != this).ToList();
        var shelvesInArea = map.listerThings.AllThings
            .Where(thing => thing is Building_Storage &&
                            thing.TryGetComp<CompStockifyShelf>() == null &&
                            this.DesignatedArea.Contains(thing.Position) &&
                            !this.ExcludedShelfThingIds.Contains(thing.thingIDNumber) &&
                            !earlierNetworks.Any(network =>
                                network.RegisteredShelves.Any(shelf => shelf.Thing == thing)))
            .Select(thing => this.RegisteredShelves.FirstOrDefault(shelf => shelf.Thing == thing)
                ?? new StockifyShelf(thing))
            .ToList();

        foreach (var shelf in this.RegisteredShelves.ToList())
        {
            if (!shelvesInArea.Contains(shelf))
            {
                this.UnregisterShelf(shelf);
            }
        }

        foreach (var shelf in shelvesInArea)
        {
            this.RegisterShelf(shelf);
        }
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
            shelf.UpdateStoredItems();
            foreach (var item in shelf.StoredItems)
            {
                if (item?.def == null || item.IsForbidden(Faction.OfPlayer) ||
                    !AllowsMaterial(item) ||
                    shelf.Thing is Building_Storage storage &&
                    storage.GetStoreSettings()?.AllowedToAccept(item) != true)
                    continue;

                var def = item.def;
                var defName = def.defName;

                if (!this.MaterialInventory.ContainsKey(defName))
                {
                    this.MaterialInventory[defName] = new NetworkMaterialInfo(def);
                }

                this.MaterialInventory[defName].AddItem(item);
            }
        }
    }

    /// <summary>
    /// Gets the stored quantity of a specific material definition.
    /// </summary>
    /// <param name="materialDefName">The ThingDef defName.</param>
    /// <returns>The physical quantity stored in this network.</returns>
    public int GetStoredQuantity(string materialDefName)
    {
        if (!this.MaterialInventory.ContainsKey(materialDefName))
        {
            return 0;
        }

        return this.MaterialInventory[materialDefName].StoredCount;
    }

    /// <summary>
    /// Gets the physical quantity of a specific material definition.
    /// </summary>
    /// <param name="materialDefName">The ThingDef defName.</param>
    /// <returns>The physical quantity stored in this network.</returns>
    public int GetTotalQuantity(string materialDefName)
    {
        if (!this.MaterialInventory.ContainsKey(materialDefName))
        {
            return 0;
        }

        return this.MaterialInventory[materialDefName].TotalCount;
    }

}