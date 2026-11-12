using Verse.AI;

namespace Stockify;

public class CompStockifyShelf : ThingComp
{
    public string ShelfName { get; private set; }
    public Thing Workbench { get; private set; }
    public List<int> SourceNetworkIds { get; private set; } = new();
    public List<StockifyStockLevel> StockLevels { get; private set; } = new();
    public List<StockifyManualRequest> ManualRequests { get; private set; } = new();
    public bool PullEnabled { get; set; } = true;
    public bool ReturnEnabled { get; set; } = true;
    public int TransferPriority { get; set; }

    public StockifyStockLevel GetStockLevel(ThingDef material)
    {
        return StockLevels.FirstOrDefault(level => level.Material == material);
    }

    public int GetLocalCount(ThingDef material)
    {
        if (parent is not Building_Storage storage || material == null) return 0;
        var heldThings = storage.GetSlotGroup()?.HeldThings;
        return heldThings?.Where(thing => thing.def == material && !thing.IsForbidden(Faction.OfPlayer))
            .Sum(thing => thing.stackCount) ?? 0;
    }

    public int GetMinimumCount(ThingDef material)
    {
        var level = GetStockLevel(material);
        return Math.Max(level?.Minimum ?? 0, GetBillRequirement(material));
    }

    public int GetTargetCount(ThingDef material)
    {
        var level = GetStockLevel(material);
        return Math.Max(level?.Target ?? 0, GetBillRequirement(material));
    }

    public int GetMaximumCount(ThingDef material)
    {
        var level = GetStockLevel(material);
        return Math.Max(level?.Maximum ?? 0, GetBillRequirement(material));
    }

    public int GetReturnLimit(ThingDef material)
    {
        var level = GetStockLevel(material);
        var billRequirement = GetBillRequirement(material);
        return level == null && billRequirement == 0
            ? -1
            : Math.Max(level?.Maximum ?? 0, billRequirement);
    }

    public bool CanPullFrom(StockifyNetwork network)
    {
        return PullEnabled && network != null && network.PullEnabled &&
               SourceNetworkIds.Contains(network.ID);
    }

    public bool CanServeRequestFrom(StockifyNetwork network)
    {
        return network != null && network.GetEnabled &&
               ManualRequests.Any(request => request.SourceNetworkIds.Contains(network.ID));
    }

    public int GetOutstandingDemandFrom(StockifyNetwork network)
    {
        if (network == null)
            return 0;

        var targets = new Dictionary<ThingDef, int>();
        if (network.GetEnabled)
        {
            foreach (var request in ManualRequests.Where(request =>
                         request.Material != null &&
                         request.SourceNetworkIds.Contains(network.ID)))
            {
                SetTarget(targets, request.Material, request.TargetCount);
            }
        }

        if (CanPullFrom(network))
        {
            foreach (var level in StockLevels.Where(level => level.Material != null))
            {
                if (GetLocalCount(level.Material) < GetMinimumCount(level.Material))
                    SetTarget(targets, level.Material, GetTargetCount(level.Material));
            }

            foreach (var requirement in GetCurrentBillRequirements())
            {
                if (GetStockLevel(requirement.Key) == null &&
                    GetLocalCount(requirement.Key) < requirement.Value)
                    SetTarget(targets, requirement.Key, requirement.Value);
            }
        }

        return targets.Sum(entry =>
            Math.Max(0, entry.Value - GetLocalCount(entry.Key)));
    }

    public int GetManualTarget(ThingDef material)
    {
        return ManualRequests
            .Where(request => request.Material == material)
            .Select(request => request.TargetCount)
            .DefaultIfEmpty(0)
            .Max();
    }

    public int GetOutstandingManualCount(ThingDef material, int sourceNetworkId)
    {
        return ManualRequests
            .Where(request => request.Material == material &&
                              request.SourceNetworkIds.Contains(sourceNetworkId))
            .Sum(request => Math.Max(0, request.TargetCount - GetLocalCount(material)));
    }

    public int GetUnassignedManualCount(ThingDef material, int sourceNetworkId)
    {
        var outstanding = GetOutstandingManualCount(material, sourceNetworkId);
        if (parent.Map == null || outstanding == 0)
            return outstanding;

        var inFlight = parent.Map.mapPawns.AllPawnsSpawned
            .Select(pawn => pawn.CurJob)
            .Where(job => job?.def == JobDefOf.HaulToCell &&
                          job.targetB.Cell == parent.Position &&
                          job.targetA.Thing?.def == material)
            .Sum(job => job.count > 0 ? job.count : job.targetA.Thing.stackCount);
        return Math.Max(0, outstanding - inFlight);
    }

    public bool RequestMaterial(ThingDef material, int quantity, int sourceNetworkId)
    {
        if (material == null || quantity <= 0 ||
            parent is not Building_Storage storage ||
            storage.GetStoreSettings()?.AllowedToAccept(material) != true)
            return false;

        var localCount = GetLocalCount(material);
        var targetCount = localCount + quantity;
        var request = ManualRequests.FirstOrDefault(existing =>
            existing.Material == material);
        if (request == null)
        {
            if (GetInFlightCount(material) > 0)
                return false;

            ManualRequests.Add(new StockifyManualRequest(
                material, localCount, targetCount, quantity, sourceNetworkId));
        }
        else
        {
            if (!request.SourceNetworkIds.Contains(sourceNetworkId))
                return false;

            request.RequestedCount += quantity;
            request.TargetCount = Math.Max(request.TargetCount, localCount) + quantity;
        }

        if (!SourceNetworkIds.Contains(sourceNetworkId))
            SourceNetworkIds.Add(sourceNetworkId);
        return true;
    }

    public override void CompTick()
    {
        base.CompTick();
        var tick = Find.TickManager.TicksGame;
        if (tick % 250 == 0)
        {
            if (Workbench?.Destroyed == true)
                Workbench = null;

            var mapComponent = parent.Map?.GetComponent<StockifyMapComponent>();
            if (mapComponent != null)
            {
                SourceNetworkIds.RemoveAll(id => mapComponent.GetNetworkByID(id) == null);
                foreach (var request in ManualRequests)
                    request.SourceNetworkIds.RemoveAll(id => mapComponent.GetNetworkByID(id) == null);
                ManualRequests.RemoveAll(request => request.SourceNetworkIds.Count == 0);
            }
        }

        if (ManualRequests.Count == 0 || tick % 60 != 0)
            return;

        ManualRequests.RemoveAll(request => request.Material == null ||
            GetLocalCount(request.Material) >= request.TargetCount);
    }

    public void AssignWorkbench(Thing workbench)
    {
        Workbench = workbench;
    }

    public void SetShelfName(string name)
    {
        ShelfName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra())
        {
            yield return gizmo;
        }

        yield return new Command_Action
        {
            defaultLabel = "Stockify: Assign workbench",
            defaultDesc = "Choose the workbench whose bills this shelf should supply.",
            action = OpenWorkbenchMenu
        };
        yield return new Command_Action
        {
            defaultLabel = "Stockify: Rename shelf",
            action = () => Find.WindowStack.Add(new StockifyShelfNameWindow(this))
        };
        yield return new Command_Action
        {
            defaultLabel = "Stockify: Select warehouses",
            defaultDesc = "Choose which central storage areas can supply this shelf.",
            action = OpenWarehouseMenu
        };
        yield return new Command_Action
        {
            defaultLabel = "Stockify: GET materials",
            defaultDesc = "Request materials from a selected warehouse using normal hauling.",
            action = OpenGetWindow
        };
        yield return new Command_Action
        {
            defaultLabel = "Stockify: Configure stock levels",
            defaultDesc = "Set minimum, target, and maximum quantities by material.",
            action = () => Find.WindowStack.Add(new StockifyStockLevelWindow(this))
        };
        if (ManualRequests.Count > 0)
        {
            yield return new Command_Action
            {
                defaultLabel = "Stockify: Cancel GET requests",
                defaultDesc = "Cancel pending GET requests and stop their active hauling jobs.",
                action = CancelManualRequests
            };
        }
        yield return new Command_Action
        {
            defaultLabel = $"Stockify PULL: {(PullEnabled ? "On" : "Off")}",
            action = () => PullEnabled = !PullEnabled
        };
        yield return new Command_Action
        {
            defaultLabel = $"Stockify return: {(ReturnEnabled ? "On" : "Off")}",
            action = () => ReturnEnabled = !ReturnEnabled
        };
        yield return new Command_Action
        {
            defaultLabel = $"Stockify transfer priority: {TransferPriority}",
            action = () => TransferPriority = (TransferPriority + 1) % 4
        };
    }

    public override string CompInspectStringExtra()
    {
        var workbenchLabel = Workbench?.LabelCap ?? "None";
        var shelfLabel = ShelfName ?? parent.LabelCap;
        var status = $"Stockify shelf: {shelfLabel}\nWorkbench: {workbenchLabel}\n" +
                     $"PULL: {(PullEnabled ? "On" : "Off")} | Return: {(ReturnEnabled ? "On" : "Off")}";
        var networks = parent.Map?.GetComponent<StockifyMapComponent>()?.Networks;
        var sourceNames = networks?
            .Where(network => SourceNetworkIds.Contains(network.ID))
            .Select(network => network.Name)
            .ToList() ?? new List<string>();
        status += $"\nSources: {(sourceNames.Count == 0 ? "None" : string.Join(", ", sourceNames))}";

        var stockLevels = StockLevels
            .Where(level => level.Material != null)
            .Select(level =>
                $"{level.Material.LabelCap}: {GetLocalCount(level.Material)} " +
                $"(min {level.Minimum}, target {level.Target}, max {level.Maximum})")
            .ToList();
        if (stockLevels.Count > 0)
            status += $"\nStock: {string.Join("; ", stockLevels)}";

        var requirements = GetCurrentBillRequirements()
            .OrderBy(entry => entry.Key.label)
            .Select(entry =>
            {
                var localCount = GetLocalCount(entry.Key);
                return $"{entry.Key.LabelCap}: {localCount}/{entry.Value} on shelf, " +
                       $"{Math.Max(0, entry.Value - localCount)} missing";
            })
            .ToList();
        if (requirements.Count > 0)
            status += $"\nCurrent bill ingredients: {string.Join("; ", requirements)}";

        var pending = ManualRequests
            .Where(request => request.Material != null)
            .Select(GetRequestProgress)
            .ToList();
        if (pending.Count > 0)
            status += $"\nGET pending: {string.Join(", ", pending)}";

        var incoming = GetIncomingTransferStatus();
        return incoming == null ? status : $"{status}\nIncoming Stockify hauling: {incoming}";
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        var workbench = Workbench;
        var shelfName = ShelfName;
        var pullEnabled = PullEnabled;
        var returnEnabled = ReturnEnabled;
        var transferPriority = TransferPriority;
        var sourceNetworkIds = SourceNetworkIds;
        var stockLevels = StockLevels;
        var manualRequests = ManualRequests;

        Scribe_References.Look(ref workbench, "workbench");
        Scribe_Values.Look(ref shelfName, "shelfName");
        Scribe_Collections.Look(ref sourceNetworkIds, "sourceNetworkIds", LookMode.Value);
        Scribe_Collections.Look(ref stockLevels, "stockLevels", LookMode.Deep);
        Scribe_Collections.Look(ref manualRequests, "manualRequests", LookMode.Deep);
        Scribe_Values.Look(ref pullEnabled, "pullEnabled", true);
        Scribe_Values.Look(ref returnEnabled, "returnEnabled", true);
        Scribe_Values.Look(ref transferPriority, "transferPriority");

        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            Workbench = workbench;
            ShelfName = shelfName;
            SourceNetworkIds = sourceNetworkIds ?? new List<int>();
            StockLevels = stockLevels ?? new List<StockifyStockLevel>();
            ManualRequests = manualRequests ?? new List<StockifyManualRequest>();
            PullEnabled = pullEnabled;
            ReturnEnabled = returnEnabled;
            TransferPriority = transferPriority;
        }
    }

    private void OpenWorkbenchMenu()
    {
        var workbenches = parent.Map?.listerThings.AllThings
            .Where(thing => thing is Building_WorkTable && thing is IBillGiver)
            .ToList();
        var options = new List<FloatMenuOption>();

        if (workbenches != null)
        {
            options.AddRange(workbenches.Select(workbench => new FloatMenuOption(
                workbench.LabelCap, () => AssignWorkbench(workbench))));
        }

        options.Add(new FloatMenuOption("Clear assignment", () => AssignWorkbench(null)));
        Find.WindowStack.Add(new FloatMenu(options));
    }

    private void OpenWarehouseMenu()
    {
        var mapComponent = parent.Map?.GetComponent<StockifyMapComponent>();
        if (mapComponent == null) return;

        var options = mapComponent.Networks.Select(network =>
        {
            var selected = SourceNetworkIds.Contains(network.ID);
            return new FloatMenuOption(
                $"{(selected ? "[x] " : "")}{network.Name}",
                () =>
                {
                    if (selected) SourceNetworkIds.Remove(network.ID);
                    else SourceNetworkIds.Add(network.ID);
                });
        }).ToList();

        Find.WindowStack.Add(new FloatMenu(options));
    }

    private void OpenGetWindow()
    {
        var networks = parent.Map?.GetComponent<StockifyMapComponent>()?.Networks;
        var network = networks?.FirstOrDefault(candidate =>
            SourceNetworkIds.Contains(candidate.ID) && candidate.GetEnabled)
            ?? networks?.FirstOrDefault(candidate => candidate.GetEnabled);
        if (network == null)
        {
            Messages.Message("No GET-enabled Stockify warehouse is available.",
                MessageTypeDefOf.RejectInput);
            return;
        }

        Find.WindowStack.Add(new StockifyGetWindow(network, this));
    }

    private void CancelManualRequests()
    {
        var materials = new HashSet<ThingDef>(ManualRequests
            .Select(request => request.Material)
            .Where(material => material != null)
        );
        var getWorkGiver = DefDatabase<WorkGiverDef>.GetNamed("StockifyGetShelf");
        if (parent.Map != null)
        {
            foreach (var pawn in parent.Map.mapPawns.AllPawnsSpawned)
            {
                var job = pawn.CurJob;
                if (job?.def == JobDefOf.HaulToCell &&
                    job.workGiverDef == getWorkGiver &&
                    job.targetB.Cell == parent.Position &&
                    materials.Contains(job.targetA.Thing?.def))
                {
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
            }
        }

        ManualRequests.Clear();
        Messages.Message("GET requests cancelled. In-progress GET hauling jobs were stopped.",
            MessageTypeDefOf.NeutralEvent);
    }

    private string GetRequestProgress(StockifyManualRequest request)
    {
        var delivered = Math.Min(request.RequestedCount,
            Math.Max(0, GetLocalCount(request.Material) - request.InitialCount));
        var inTransit = GetInFlightCount(request.Material);
        return $"{request.Material.LabelCap}: {delivered}/{request.RequestedCount} delivered, " +
               $"{Math.Min(inTransit, Math.Max(0, request.TargetCount - GetLocalCount(request.Material)))} in transit";
    }

    private int GetInFlightCount(ThingDef material)
    {
        if (parent.Map == null)
            return 0;

        return parent.Map.mapPawns.AllPawnsSpawned
            .Select(pawn => pawn.CurJob)
            .Where(job => job?.def == JobDefOf.HaulToCell &&
                          job.workGiverDef == DefDatabase<WorkGiverDef>.GetNamed("StockifyGetShelf") &&
                          job.targetB.Cell == parent.Position &&
                          job.targetA.Thing?.def == material)
            .Sum(job => job.count > 0 ? job.count : job.targetA.Thing.stackCount);
    }

    private string GetIncomingTransferStatus()
    {
        if (parent.Map == null)
            return null;

        var incoming = parent.Map.mapPawns.AllPawnsSpawned
            .Select(pawn => pawn.CurJob)
            .Where(job => job?.def == JobDefOf.HaulToCell &&
                          (job.workGiverDef?.defName == "StockifyReplenishShelf" ||
                           job.workGiverDef?.defName == "StockifyGetShelf") &&
                          job.targetB.Cell == parent.Position &&
                          job.targetA.Thing?.def != null)
            .GroupBy(job => job.targetA.Thing.def)
            .Select(group =>
                $"{group.Key.LabelCap}: {group.Sum(job =>
                    job.count > 0 ? job.count : job.targetA.Thing.stackCount)}")
            .ToList();

        return incoming.Count == 0 ? null : string.Join(", ", incoming);
    }

    private int GetBillRequirement(ThingDef material)
    {
        if (Workbench is not IBillGiver billGiver || material == null)
            return 0;

        var required = 0;
        foreach (var bill in billGiver.BillStack.Bills.OfType<Bill_Production>())
        {
            if (!bill.ShouldDoNow() || bill.recipe?.ingredients == null)
                continue;

            foreach (var ingredient in bill.recipe.ingredients)
            {
                if (ingredient.filter?.Allows(material) != true &&
                    ingredient.FixedIngredient != material)
                    continue;
                if (!bill.IsFixedOrAllowedIngredient(material))
                    continue;

                required += ingredient.CountRequiredOfFor(material, bill.recipe, bill);
            }
        }

        return required;
    }

    private Dictionary<ThingDef, int> GetCurrentBillRequirements()
    {
        var requiredMaterials = new Dictionary<ThingDef, int>();
        if (Workbench is not IBillGiver billGiver)
            return requiredMaterials;

        foreach (var bill in billGiver.BillStack.Bills.OfType<Bill_Production>())
        {
            if (!bill.ShouldDoNow() || bill.recipe?.ingredients == null)
                continue;

            foreach (var ingredient in bill.recipe.ingredients)
            {
                var candidates = ingredient.FixedIngredient != null
                    ? new[] { ingredient.FixedIngredient }
                    : ingredient.filter?.AllowedThingDefs ?? Enumerable.Empty<ThingDef>();
                foreach (var material in candidates)
                {
                    if (material == null ||
                        (ingredient.filter?.Allows(material) != true &&
                         ingredient.FixedIngredient != material) ||
                        !bill.IsFixedOrAllowedIngredient(material))
                        continue;

                    var count = ingredient.CountRequiredOfFor(material, bill.recipe, bill);
                    if (count <= 0)
                        continue;

                    requiredMaterials.TryGetValue(material, out var current);
                    requiredMaterials[material] = current + count;
                }
            }
        }

        return requiredMaterials;
    }

    private static void SetTarget(
        Dictionary<ThingDef, int> targets, ThingDef material, int target)
    {
        targets.TryGetValue(material, out var current);
        targets[material] = Math.Max(current, target);
    }
}

public class StockifyShelfNameWindow : Window
{
    private readonly CompStockifyShelf _shelf;
    private string _name;

    public StockifyShelfNameWindow(CompStockifyShelf shelf)
        : base(new DefaultWindowDrawing())
    {
        _shelf = shelf;
        _name = shelf.ShelfName ?? shelf.parent.LabelCap;
        layer = WindowLayer.Super;
        optionalTitle = "Rename Stockify Shelf";
        doCloseX = true;
        closeOnCancel = true;
        absorbInputAroundWindow = true;
    }

    public override Vector2 InitialSize => new(420f, 160f);

    public override void DoWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        _name = Widgets.TextField(listing.GetRect(32f), _name);
        if (Widgets.ButtonText(listing.GetRect(32f), "Save"))
        {
            _shelf.SetShelfName(_name);
            Close();
        }
        listing.End();
    }
}

public class StockifyManualRequest : IExposable
{
    public ThingDef Material;
    public int TargetCount;
    public List<int> SourceNetworkIds = new();
    public int InitialCount;
    public int RequestedCount;

    public StockifyManualRequest()
    {
    }

    public StockifyManualRequest(
        ThingDef material,
        int initialCount,
        int targetCount,
        int requestedCount,
        int sourceNetworkId)
    {
        Material = material;
        InitialCount = initialCount;
        TargetCount = targetCount;
        RequestedCount = requestedCount;
        SourceNetworkIds.Add(sourceNetworkId);
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref Material, "material");
        Scribe_Values.Look(ref TargetCount, "targetCount");
        Scribe_Collections.Look(ref SourceNetworkIds, "sourceNetworkIds", LookMode.Value);
        Scribe_Values.Look(ref InitialCount, "initialCount");
        Scribe_Values.Look(ref RequestedCount, "requestedCount");
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            SourceNetworkIds ??= new List<int>();
            if (RequestedCount <= 0)
                RequestedCount = TargetCount;
        }
    }
}

public class StockifyStockLevelWindow : Window
{
    private readonly CompStockifyShelf _shelf;
    private Vector2 _scrollPosition;

    public StockifyStockLevelWindow(CompStockifyShelf shelf)
        : base(new DefaultWindowDrawing())
    {
        _shelf = shelf;
        layer = WindowLayer.Super;
        optionalTitle = "Stockify stock levels";
        doCloseX = true;
        closeOnCancel = true;
        absorbInputAroundWindow = true;
    }

    public override Vector2 InitialSize => new(560f, 480f);

    public override void DoWindowContents(Rect inRect)
    {
        var viewRect = new Rect(
            0f, 0f, inRect.width - 16f,
            Math.Max(inRect.height, 100f + _shelf.StockLevels.Count * 34f));
        Widgets.BeginScrollView(inRect, ref _scrollPosition, viewRect);
        var listing = new Listing_Standard();
        listing.Begin(viewRect);
        listing.Label("Set a minimum trigger, target stock, and return maximum.");
        if (Widgets.ButtonText(listing.GetRect(30f), "Add material"))
            OpenMaterialMenu();

        var header = listing.GetRect(24f);
        Widgets.Label(new Rect(header.x + 155f, header.y, 70f, header.height), "Minimum");
        Widgets.Label(new Rect(header.x + 230f, header.y, 70f, header.height), "Target");
        Widgets.Label(new Rect(header.x + 305f, header.y, 70f, header.height), "Maximum");

        foreach (var level in _shelf.StockLevels.ToList())
        {
            if (level.Material == null)
                continue;

            var row = listing.GetRect(32f);
            Widgets.Label(new Rect(row.x, row.y, 150f, row.height), level.Material.LabelCap);
            var minimum = Widgets.TextField(
                new Rect(row.x + 155f, row.y, 70f, row.height), level.Minimum.ToString());
            var target = Widgets.TextField(
                new Rect(row.x + 230f, row.y, 70f, row.height), level.Target.ToString());
            var maximum = Widgets.TextField(
                new Rect(row.x + 305f, row.y, 70f, row.height), level.Maximum.ToString());
            if (int.TryParse(minimum, out var minValue) &&
                int.TryParse(target, out var targetValue) &&
                int.TryParse(maximum, out var maxValue))
            {
                level.Minimum = Math.Max(0, minValue);
                level.Target = Math.Max(level.Minimum, targetValue);
                level.Maximum = Math.Max(level.Target, maxValue);
            }

            if (Widgets.ButtonText(
                    new Rect(row.x + 385f, row.y, 80f, row.height), "Remove"))
                _shelf.StockLevels.Remove(level);
        }

        listing.End();
        Widgets.EndScrollView();
    }

    private void OpenMaterialMenu()
    {
        var mapComponent = _shelf.parent.Map?.GetComponent<StockifyMapComponent>();
        if (mapComponent == null)
            return;

        var sourceNetworks = mapComponent.Networks.Where(network =>
            _shelf.SourceNetworkIds.Contains(network.ID)).ToList();
        var map = _shelf.parent.Map;
        var materials = DefDatabase<ThingDef>.AllDefsListForReading
            .Where(material => material.category == ThingCategory.Item &&
                               material.EverHaulable &&
                               sourceNetworks.Any(network =>
                                   network.AllowsMaterial(material) &&
                                   network.DesignatedCells.Any(cell =>
                                       cell.InBounds(map) &&
                                       network.ContainsCell(cell, map) &&
                                       (map.thingGrid.ThingsListAt(cell)
                                            .OfType<Building_Storage>()
                                            .Any(storage =>
                                                storage.TryGetComp<CompStockifyShelf>() == null &&
                                                storage.GetStoreSettings()?.AllowedToAccept(material) == true) ||
                                        map.zoneManager.ZoneAt(cell) is Zone_Stockpile stockpile &&
                                        stockpile.GetStoreSettings()?.AllowedToAccept(material) == true))))
            .ToList();

        var options = materials
            .Where(material => _shelf.GetStockLevel(material) == null)
            .OrderBy(material => material.label)
            .Select(material => new FloatMenuOption(
                material.LabelCap,
                () => _shelf.StockLevels.Add(
                    new StockifyStockLevel(material, 10, 10, 10))))
            .ToList();
        if (options.Count == 0)
        {
            Messages.Message("No new materials can be stored in the selected warehouses.",
                MessageTypeDefOf.RejectInput);
            return;
        }

        Find.WindowStack.Add(new FloatMenu(options));
    }
}

public class StockifyStockLevel : IExposable
{
    public ThingDef Material;
    public int Minimum;
    public int Target;
    public int Maximum;

    public StockifyStockLevel()
    {
    }

    public StockifyStockLevel(ThingDef material, int minimum, int target, int maximum)
    {
        Material = material;
        Minimum = Math.Max(0, minimum);
        Target = Math.Max(Minimum, target);
        Maximum = Math.Max(Target, maximum);
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref Material, "material");
        Scribe_Values.Look(ref Minimum, "minimum");
        Scribe_Values.Look(ref Target, "target");
        Scribe_Values.Look(ref Maximum, "maximum");

        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            Minimum = Math.Max(0, Minimum);
            Target = Math.Max(Minimum, Target);
            Maximum = Math.Max(Target, Maximum);
        }
    }
}
