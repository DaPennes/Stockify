namespace Stockify;

/// <summary>
/// Manages storage operations including shelf detection, material source/destination selection,
/// and capacity checking.
/// </summary>
public class StockifyStorageManager
{
    /// <summary>
    /// Detects eligible shelves within a designated area.
    /// </summary>
    /// <param name="area">The area to scan for shelves.</public>
    /// <param name="map">The map to scan on.</public>
    /// <returns>A list of eligible shelves found in the area.</public>
    public List<StockifyShelf> DetectShelvesInArea(CellRect area, Verse.Map map)
    {
        var shelves = new List<StockifyShelf>();

        // Find all things in the area that could be shelves
        var thingsInArea = map.listerThings.ThingsInArea(area, ThingRequest.ForDescribed(Lister.ThingsInRole(ThingRequest.Group.Building)));

        foreach (var thing in thingsInArea)
        {
            var shelf = new StockifyShelf(thing);

            // Verify this is actually a compatible shelf
            if (shelf.IsCompatibleShelf)
            {
                shelves.Add(shelf);
            }
        }

        return shelves;
    }

    /// <summary>
    /// Finds the best source shelf for retrieving materials.
    /// </summary>
    /// <param name="materialLabel">The material label to find.</public>
    /// <param name="network">The network to search.</public>
    /// <param name="selectionMode">The selection mode to use.</public>
    /// <param name="excludedShelves">Shelves to exclude from selection.</public>
    /// <returns>The best source shelf, or null if none found.</public>
    public StockifyShelf FindBestSourceShelf(string materialLabel, StockifyNetwork network,
        StockifySelectionMode selectionMode, List<StockifyShelf> excludedShelves = null)
    {
        excludedShelves ??= new List<StockifyShelf>();

        var eligibleShelves = network.RegisteredShelves
            .Where(s => !excludedShelves.Contains(s))
            .Where(s => s.HasCapacity())
            .ToList();

        if (eligibleShelves.Count == 0)
        {
            return null;
        }

        StockifyShelf bestShelf = null;
        int bestScore = int.MinValue;

        foreach (var shelf in eligibleShelves)
        {
            var score = this.CalculateSelectionScore(shelf, materialLabel, selectionMode);

            if (selectionMode == StockifySelectionMode.NearestFirst)
            {
                // Prefer nearest shelf
                if (bestShelf == null || shelf.Thing.Position.DistanceTo(network.DesignatedArea.Center) <
                    bestShelf.Thing.Position.DistanceTo(network.DesignatedArea.Center))
                {
                    bestShelf = shelf;
                }
            }
            else if (selectionMode == StockifySelectionMode.PriorityFirst)
            {
                // Prefer highest priority
                if (score > bestScore)
                {
                    bestScore = score;
                    bestShelf = shelf;
                }
            }
            else if (selectionMode == StockifySelectionMode.Balanced)
            {
                // Consider both distance and priority
                if (bestShelf == null || this.IsBetterBalancedScore(score, bestScore,
                    shelf.Thing.Position.DistanceTo(network.DesignatedArea.Center),
                    bestShelf.Thing.Position.DistanceTo(network.DesignatedArea.Center)))
                {
                    bestShelf = shelf;
                }
            }
        }

        return bestShelf;
    }

    /// <summary>
    /// Finds the best destination shelf for storing materials.
    /// </summary>
    /// <param name="materialLabel">The material label to store.</public>
    /// <param name="network">The target network.</public>
    /// <param name="itemCount">The number of items to store.</public>
    /// <param name="selectionMode">The selection mode to use.</public>
    /// <returns>The best destination shelf, or null if none found.</public>
    public StockifyShelf FindBestDestinationShelf(string materialLabel, StockifyNetwork network,
        int itemCount, StockifySelectionMode selectionMode)
    {
        var eligibleShelves = network.RegisteredShelves
            .Where(s => s.HasCapacity(itemCount))
            .ToList();

        if (eligibleShelves.Count == 0)
        {
            return null;
        }

        StockifyShelf bestShelf = null;
        int bestScore = int.MinValue;

        foreach (var shelf in eligibleShelves)
        {
            var score = this.CalculateDestinationSelectionScore(shelf, materialLabel, selectionMode);

            if (selectionMode == StockifySelectionMode.NearestFirst)
            {
                if (bestShelf == null || shelf.Thing.Position.DistanceTo(network.DesignatedArea.Center) <
                    bestShelf.Thing.Position.DistanceTo(network.DesignatedArea.Center))
                {
                    bestShelf = shelf;
                }
            }
            else if (selectionMode == StockifySelectionMode.PriorityFirst)
            {
                if (score > bestScore)
                {
                    bestScore = score;
                    bestShelf = shelf;
                }
            }
            else if (selectionMode == StockifySelectionMode.Balanced)
            {
                if (bestShelf == null || this.IsBetterBalancedScore(score, bestScore,
                    shelf.Thing.Position.DistanceTo(network.DesignatedArea.Center),
                    bestShelf.Thing.Position.DistanceTo(network.DesignatedArea.Center)))
                {
                    bestShelf = shelf;
                }
            }
        }

        return bestShelf;
    }

    /// <summary>
    /// Calculates the selection score for source shelf selection.
    /// </summary>
    /// <param name="shelf">The shelf to score.</public>
    /// <param name="materialLabel">The material label.</public>
    /// <param name="mode">The selection mode.</public>
    /// <returns>The selection score.</public>
    private int CalculateSelectionScore(StockifyShelf shelf, string materialLabel, StockifySelectionMode mode)
    {
        var score = 0;

        // Base score from storage priority
        score += shelf.StoragePriority * 100;

        // Available quantity factor
        var available = shelf.GetAvailableCount(Verse.ParseToolDef(materialLabel) ?? Verse.Content.Lookup<ThingDef>(materialLabel));
        score += available;

        return score;
    }

    /// <summary>
    /// Calculates the selection score for destination shelf selection.
    /// </summary>
    /// <param name="shelf">The shelf to score.</public>
    /// <param name="materialLabel">The material label.</public>
    /// <param name="mode">The selection mode.</public>
    /// <returns>The selection score.</public>
    private int CalculateDestinationSelectionScore(StockifyShelf shelf, string materialLabel, StockifySelectionMode mode)
    {
        var score = 0;

        // Capacity factor - prefer shelves with more room
        var capacity = shelf.HasCapacity(1) ? 100 : 0;
        score += capacity;

        // Storage priority
        score += shelf.StoragePriority * 10;

        return score;
    }

    /// <summary>
    /// Determines if a balanced score is better than the current best.
    /// </summary>
    /// <param name="newScore">The new score.</public>
    /// <param name="currentBest">The current best score.</public>
    /// <param name="newDistance">The new shelf's distance.</public>
    /// <param name="currentDistance">The current best shelf's distance.</public>
    /// <returns>True if the new score is better.</public>
    private bool IsBetterBalancedScore(int newScore, int currentBest, int newDistance, int currentDistance)
    {
        // Balanced: prefer higher priority and available capacity, but distance matters too
        // Simple heuristic: if scores are close, prefer nearer shelf
        var scoreDifference = newScore - currentBest;
        if (Math.Abs(scoreDifference) < 50) // scores are close
        {
            return newDistance < currentDistance; // prefer nearer
        }
        return scoreDifference > 0; // prefer higher score
    }
}