using Verse.AI;

namespace Stockify;

public class StockifyWorkGiver : WorkGiver_Scanner
{
    private bool IsGetWorkGiver => def.defName == "StockifyGetShelf";

    public override ThingRequest PotentialWorkThingRequest =>
        ThingRequest.ForGroup(ThingRequestGroup.HaulableEver);

    public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        return pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver);
    }

    public override Job JobOnThing(Pawn pawn, Thing item, bool forced = false)
    {
        if (item == null || item.Destroyed || item.IsForbidden(pawn) ||
            !item.def.EverHaulable || !pawn.CanReserveAndReach(
                item, PathEndMode, Danger.Some, 1, -1, null, forced))
            return null;

        var mapComponent = pawn.Map.GetComponent<StockifyMapComponent>();
        if (mapComponent == null)
            return null;

        foreach (var network in mapComponent.Networks.OrderByDescending(network => network.StoragePriority))
        {
            if ((IsGetWorkGiver ? !network.GetEnabled : !network.PullEnabled && !network.OutputEnabled) ||
                !network.AllowsMaterial(item) ||
                !network.RegisteredShelves.Any(shelf =>
                    shelf.Thing.TryGetComp<CompStockifyShelf>() == null &&
                    shelf.Thing is Building_Storage storage &&
                    storage.GetStoreSettings()?.AllowedToAccept(item) == true &&
                    storage.GetSlotGroup()?.HeldThings.Contains(item) == true))
                continue;

            if (IsGetWorkGiver || network.PullEnabled)
            {
                foreach (var comp in mapComponent.LocalShelves
                             .OrderByDescending(shelf => shelf.TransferPriority))
                {
                    var automaticPull = !IsGetWorkGiver && comp?.CanPullFrom(network) == true;
                    var hasManualRequest = IsGetWorkGiver && comp?.CanServeRequestFrom(network) == true;
                    if (comp == null || (!automaticPull && !hasManualRequest) ||
                        comp.parent is not Building_Storage destination ||
                        destination.GetStoreSettings()?.AllowedToAccept(item) != true ||
                        !pawn.CanReserveAndReach(
                            destination, PathEndMode, Danger.Some, 1, -1, null, forced))
                        continue;

                    var localCount = comp.GetLocalCount(item.def);
                    var manualTarget = hasManualRequest
                        ? comp.GetManualTarget(item.def)
                        : 0;
                    var target = manualTarget;
                    if (automaticPull && localCount < comp.GetMinimumCount(item.def))
                        target = Math.Max(target, comp.GetTargetCount(item.def));
                    if (target <= localCount)
                        continue;

                    var inFlight = CountInFlight(pawn.Map, item.def, destination);
                    var needed = target - localCount - inFlight;
                    if (needed <= 0)
                        continue;

                    var job = HaulAIUtility.HaulToCellStorageJob(
                        pawn, item, destination.Position, forced);
                    if (job != null)
                    {
                        job.count = Math.Min(needed, item.stackCount);
                        return job;
                    }
                }
            }
        }

        if (!IsStoredInAnyBuilding(pawn.Map, item))
        {
            if (StoreUtility.TryFindBestBetterStoreCellFor(
                    item, pawn, pawn.Map, StoragePriority.Unstored, Faction.OfPlayer,
                    out var bestStoreCell, false))
            {
                foreach (var network in mapComponent.Networks.Where(candidate =>
                             candidate.InputEnabled && candidate.AllowsMaterial(item))
                         .OrderByDescending(candidate => candidate.StoragePriority))
                {
                    foreach (var destinationShelf in network.RegisteredShelves)
                    {
                        if (destinationShelf.Thing is not Building_Storage destination ||
                            !destination.AllSlotCells().Contains(bestStoreCell) ||
                            destination.GetStoreSettings()?.AllowedToAccept(item) != true ||
                            !pawn.CanReserveAndReach(
                                destination, PathEndMode, Danger.Some, 1, -1, null, forced))
                            continue;

                        var inputJob = HaulAIUtility.HaulToCellStorageJob(
                            pawn, item, bestStoreCell, forced);
                        if (inputJob != null)
                            return inputJob;
                    }
                }
            }
        }

        foreach (var source in pawn.Map.thingGrid.ThingsListAt(item.Position).OfType<Building_Storage>())
        {
            var comp = source.TryGetComp<CompStockifyShelf>();
            if (comp == null || !comp.ReturnEnabled ||
                source.GetSlotGroup()?.HeldThings.Contains(item) != true)
                continue;

            var localCount = comp.GetLocalCount(item.def);
            var returnLimit = comp.GetReturnLimit(item.def);
            var incomingToShelf = CountInFlight(pawn.Map, item.def, source);
            var excess = Math.Min(localCount, localCount + incomingToShelf - returnLimit);
            if (returnLimit < 0 || excess <= 0 ||
                !pawn.CanReserveAndReach(
                    source, PathEndMode, Danger.Some, 1, -1, null, forced))
                continue;

            foreach (var network in mapComponent.Networks
                         .Where(candidate =>
                             candidate.OutputEnabled &&
                             candidate.AllowsMaterial(item) &&
                             comp.SourceNetworkIds.Contains(candidate.ID))
                         .OrderByDescending(candidate => candidate.StoragePriority))
            {
                foreach (var destinationShelf in network.RegisteredShelves)
                {
                    if (destinationShelf.Thing is not Building_Storage destination ||
                        destination.GetStoreSettings()?.AllowedToAccept(item) != true ||
                        !pawn.CanReserveAndReach(
                            destination, PathEndMode, Danger.Some, 1, -1, null, forced))
                        continue;

                    var inFlight = CountInFlight(pawn.Map, item.def, destination);
                    var needed = excess - inFlight;
                    if (needed <= 0)
                        continue;

                    var job = HaulAIUtility.HaulToCellStorageJob(
                        pawn, item, destination.Position, forced);
                    if (job != null)
                    {
                        job.count = Math.Min(needed, item.stackCount);
                        return job;
                    }
                }
            }
        }

        return null;
    }

    private static bool IsStoredInAnyBuilding(Map map, Thing item)
    {
        var heldInBuilding = map.thingGrid.ThingsListAt(item.Position)
            .OfType<Building_Storage>()
            .Any(storage => storage.GetSlotGroup()?.HeldThings.Contains(item) == true);
        if (heldInBuilding)
            return true;

        return map.zoneManager.ZoneAt(item.Position) is Zone_Stockpile stockpile &&
               stockpile.GetStoreSettings()?.AllowedToAccept(item) == true;
    }

    private static int CountInFlight(Map map, ThingDef material, Building_Storage destination)
    {
        var count = 0;
        foreach (var pawn in map.mapPawns.AllPawnsSpawned)
        {
            var job = pawn.CurJob;
            if (job?.def != JobDefOf.HaulToCell ||
                job.targetB.Cell != destination.Position ||
                job.targetA.Thing?.def != material)
                continue;

            count += job.count > 0 ? job.count : job.targetA.Thing.stackCount;
        }

        return count;
    }
}
