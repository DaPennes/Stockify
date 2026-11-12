namespace Stockify;

/// <summary>
/// Represents a storage network with connected shelves and material inventory.
/// </summary>
public class StockifyNetwork : IExposable
{
    private Map _map;

    public Map Map => _map;

    /// <summary>
    /// Gets the unique identifier for this network.
    /// </summary>
    public int ID { get; private set; }

    /// <summary>
    /// Gets or sets the network name.
    /// </summary>
    public string Name { get; set; }

    private HashSet<IntVec3> _designatedCellLookup = new();

    public List<IntVec3> DesignatedCells { get; private set; } = new();

    public CellRect DesignatedArea
    {
        get => DesignatedCells.Count == 0
            ? CellRect.Empty
            : CellRect.FromCellList(DesignatedCells);
        set => SetDesignatedCells(value.Cells);
    }

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
        return thing?.def != null && AllowsMaterial(thing.def);
    }

    public bool AllowsMaterial(ThingDef material)
    {
        return material != null && !IngredientFilters.Contains(material.defName);
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
        : this(name, area.Cells)
    {
    }

    public StockifyNetwork(string name, IEnumerable<IntVec3> designatedCells)
    {
        this.ID = UnityEngine.Random.Range(100000, 999999);
        this.Name = name;
        SetDesignatedCells(designatedCells);
    }

    public StockifyNetwork() : this("", CellRect.Empty)
    {
    }

    public void ExposeData()
    {
        var id = ID;
        var name = Name;
        var area = DesignatedArea;
        var minX = area.minX;
        var minZ = area.minZ;
        var maxX = area.maxX;
        var maxZ = area.maxZ;
        var designatedCells = DesignatedCells;
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
        Scribe_Collections.Look(ref designatedCells, "designatedCells", LookMode.Value);
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
            if (designatedCells == null || designatedCells.Count == 0)
            {
                var legacyArea = minX <= maxX && minZ <= maxZ
                    ? CellRect.FromLimits(minX, minZ, maxX, maxZ)
                    : CellRect.Empty;
                designatedCells = legacyArea.Cells.ToList();
            }

            SetDesignatedCells(designatedCells);
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

    public void SetDesignatedCells(IEnumerable<IntVec3> cells)
    {
        DesignatedCells = cells?
            .Distinct()
            .OrderBy(cell => cell.x)
            .ThenBy(cell => cell.z)
            .ToList() ?? new List<IntVec3>();
        _designatedCellLookup = new HashSet<IntVec3>(DesignatedCells);
    }

    public bool ContainsCell(IntVec3 cell, Map map = null)
    {
        if (!_designatedCellLookup.Contains(cell))
            return false;

        map ??= _map ?? RegisteredShelves
            .Select(shelf => shelf.Thing?.Map)
            .FirstOrDefault(candidate => candidate != null);
        var networks = map?.GetComponent<StockifyMapComponent>()?.Networks;
        return networks == null ||
               !networks.TakeWhile(network => network != this)
                   .Any(network => network._designatedCellLookup.Contains(cell));
    }

    public void AttachToMap(Map map)
    {
        _map = map;
    }

    public bool IsValidStorageCell(IntVec3 cell, Map map, Thing item)
    {
        return map != null &&
               item != null &&
               ContainsCell(cell, map) &&
               StoreUtility.IsValidStorageFor(cell, map, item) &&
               !map.thingGrid.ThingsListAt(cell)
                   .OfType<Building_Storage>()
                   .Any(storage => storage.TryGetComp<CompStockifyShelf>() != null);
    }

    public IEnumerable<Thing> GetStoredItems(Map map)
    {
        if (map == null)
            yield break;

        var seen = new HashSet<Thing>();
        foreach (var cell in DesignatedCells)
        {
            if (!cell.InBounds(map) || !ContainsCell(cell, map))
                continue;

            foreach (var item in map.thingGrid.ThingsListAt(cell))
            {
                if (item.def?.category == ThingCategory.Item &&
                    seen.Add(item) &&
                    IsValidStorageCell(item.Position, map, item))
                    yield return item;
            }
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
            this.UpdateMaterialInventory(shelf.Thing?.Map);
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
        this.UpdateMaterialInventory(shelf.Thing?.Map ?? _map);
    }

    public void UpdateShelfRegistration(Map map)
    {
        if (map == null) return;

        var shelvesInArea = map.listerThings.AllThings
            .Where(thing => thing is Building_Storage storage &&
                            thing.TryGetComp<CompStockifyShelf>() == null &&
                            storage.AllSlotCells().Any(cell => ContainsCell(cell, map)) &&
                            !this.ExcludedShelfThingIds.Contains(thing.thingIDNumber) &&
                            !map.GetComponent<StockifyMapComponent>().Networks
                                .TakeWhile(network => network != this)
                                .Any(network =>
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
    public void UpdateMaterialInventory(Map map = null)
    {
        this.MaterialInventory.Clear();
        map ??= _map ?? RegisteredShelves
            .Select(shelf => shelf.Thing?.Map)
            .FirstOrDefault(candidate => candidate != null);

        foreach (var item in GetStoredItems(map))
        {
            if (item.def == null || item.IsForbidden(Faction.OfPlayer) || !AllowsMaterial(item))
                continue;

            var defName = item.def.defName;
            if (!MaterialInventory.TryGetValue(defName, out var info))
            {
                info = new NetworkMaterialInfo(item.def);
                MaterialInventory.Add(defName, info);
            }

            info.AddItem(item);
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