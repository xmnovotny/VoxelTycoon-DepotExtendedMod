using System.Collections.Generic;
using DepotExtended.DepotVehicles;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using VoxelTycoon;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Rails;
using XMNUtils;

namespace DepotExtended.UI
{
    [HarmonyPatch]
    public static class DepotWindowExtender
    {
        private static Dictionary<VehicleDepot, DepotWindow> _windows = new ();

        public static void OnDepotVehiclesChanged(RailDepot depot)
        {
            if (_windows.TryGetValue(depot, out DepotWindow window))
            {
                window.InvalidateItems();
            }
        }
        
        [UsedImplicitly]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DepotWindowContent), "InvalidateItems")]
        // ReSharper disable once InconsistentNaming
        private static void DepotWindowContent_InvalidateItems_pof(DepotWindowContent __instance, DepotWindow ____window)
        {
            if (____window.Depot is not RailDepot railDepot) 
                return;
            
            IReadOnlyList<VehicleUnit> units = SimpleLazyManager<RailDepotManager>.Current.GetDepotVehicleUnits(railDepot);
            if (units == null || units.Count == 0)
                return;

            DepotWindowDepotVehiclesListItem listItem =
                DepotWindowDepotVehiclesListItem.InstantiateItem(__instance.ItemContainer.transform);
            listItem.Initialize(____window, __instance, units);
            __instance.Header.SetActive(true);
            __instance.Placeholder.SetActive(false);
            __instance.ScrollRect.SetActive(true);
        }

        [UsedImplicitly]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DepotWindow), "Initialize")]
        // ReSharper disable once InconsistentNaming
        private static void DepotWindow_Initialize_pof(DepotWindow __instance)
        {
            if (__instance.Depot is not RailDepot railDepot) 
                return;
            _windows.Add(railDepot, __instance);
        }

        [UsedImplicitly]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DepotWindow), "OnClose")]
        // ReSharper disable once InconsistentNaming
        private static void DepotWindow_OnClose_pof(DepotWindow __instance)
        {
            _windows.Remove(__instance.Depot);
        }
    }
}