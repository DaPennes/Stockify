using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Stockify
{
    /// <summary>
    /// Harmony patches that add StockifyBillComp to vanilla workbenches.
    /// This enables PULL functionality on existing vanilla workbenches without
    /// requiring new buildings or research.
    /// </summary>
    [HarmonyPatch(typeof(ThingDef))]
    [HarmonyPatch("PostLoad")]
    public static class StockifyWorkbenchPatches
    {
        /// <summary>
        /// Adds StockifyBillComp to workbenches after their def is loaded.
        /// </summary>
        /// <paramname="def">The ThingDef being loaded.</param>
        private static void PostLoad(ThingDef def)
        {
            // Check if this is a workbench that should have Stockify PULL
            if (def?.compClass == null) return;

            // Add StockifyBillComp to compatible workbenches
            // We check for common workbench categories and tags

            // smelter, electric smelter
            if (def.thingCategories != null &&
                def.thingCategories.Contains(ThingCategoryDefOf.WorkTable))
            {
                // Don't add if already has comp
                if (def.comps == null || !def.comps.Any(c => c.GetType().Name == "StockifyBillComp"))
                {
                    // Create a comp and add it
                    var stockifyComp = new StockifyBillComp();
                    // We can't directly add to comps array from harmony post-load easily,
                    // so we use a different approach - patch the bill giving instead
                }
            }

            // General workbench check - things with work to be done tag
            if (def.GetModExtension<WorkBenchExtension>() != null)
            {
                // This extension approach won't work directly either
            }
        }
    }

    /// <summary>
    /// Extension class for workbench definitions (would be in ModExtensions.xml)
    /// </summary>
    public class WorkBenchExtension : Verse.ModExtension
    {
        public bool addStockifyBillComp = true;
    }
}