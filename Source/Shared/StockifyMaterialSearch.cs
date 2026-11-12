namespace Stockify;

/// <summary>
/// Provides material search functionality for Stockify networks.
/// Allows searching by material name, filtering, and sorting.
/// </summary>
public static class StockifyMaterialSearch
{
    /// <summary>
    /// Searches for materials by name pattern across a network.
    /// </summary>
    /// <param name="network">The network to search.</param>
    /// <param name="searchTerm">The search term (partial match, case-insensitive).</param>
    /// <returns>List of material entries matching the search.</returns>
    public static List<StockifyInventoryManager.MaterialInventoryEntry> SearchMaterials(StockifyNetwork network, string searchTerm)
    {
        var allMaterials = StockifyInventoryManager.GetAllMaterials(network);
        var results = new List<StockifyInventoryManager.MaterialInventoryEntry>();

        var searchLower = searchTerm.ToLowerInvariant();

        foreach (var material in allMaterials)
        {
            if (material.MaterialLabel.ToLowerInvariant().Contains(searchLower))
            {
                results.Add(material);
            }
        }

        // Sort by relevance (exact match first, then partial, by total count)
        results.Sort((a, b) =>
        {
            var aExact = a.MaterialLabel.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            var bExact = b.MaterialLabel.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            if (aExact != bExact) return bExact.CompareTo(aExact);
            return b.Total.CompareTo(a.Total);
        });

        return results;
    }

    /// <summary>
    /// Filters materials by category/group.
    /// </summary>
    /// <param name="network">The network to filter.</param>
    /// <param name="category">The category to filter by (e.g., "Metal", "Wood", "Food").</param>
    /// <returns>List of material entries in the category.</returns>
    public static List<StockifyInventoryManager.MaterialInventoryEntry> FilterByCategory(StockifyNetwork network, string category)
    {
        var allMaterials = StockifyInventoryManager.GetAllMaterials(network);
        var results = new List<StockifyInventoryManager.MaterialInventoryEntry>();

        var categoryLower = category.ToLowerInvariant();

        foreach (var material in allMaterials)
        {
            // Check if material label contains the category term
            if (material.MaterialLabel.ToLowerInvariant().Contains(categoryLower))
            {
                results.Add(material);
            }
        }

        return results;
    }

    /// <summary>
    /// Sorts materials by the specified criteria.
    /// </summary>
    /// <param name="materials">The materials to sort.</param>
    /// <param name="sortBy">The sort criteria.</param>
    /// <param name="descending">Whether to sort descending.</param>
    /// <returns>Sorted list of materials.</returns>
    public static List<StockifyInventoryManager.MaterialInventoryEntry> SortMaterials(
        List<StockifyInventoryManager.MaterialInventoryEntry> materials,
        StockifySelectionMode sortBy, bool descending = false)
    {
        var sorted = new List<StockifyInventoryManager.MaterialInventoryEntry>(materials);

        switch (sortBy)
        {
            case StockifySelectionMode.NearestFirst:
                // Not applicable for pure inventory sort, fall back to total
                sorted.Sort((a, b) => descending ? b.Total.CompareTo(a.Total) : a.Total.CompareTo(b.Total));
                break;
            case StockifySelectionMode.PriorityFirst:
                // Priority sort would need shelf priority data, fall back to total
                sorted.Sort((a, b) => descending ? b.Total.CompareTo(a.Total) : a.Total.CompareTo(b.Total));
                break;
            case StockifySelectionMode.Balanced:
                // Balanced sort considering availability and total
                sorted.Sort((a, b) =>
                {
                    var aScore = a.Available + (a.Total / 10);
                    var bScore = b.Available + (b.Total / 10);
                    return descending ? bScore.CompareTo(aScore) : aScore.CompareTo(bScore);
                });
                break;
        }

        return sorted;
    }
}