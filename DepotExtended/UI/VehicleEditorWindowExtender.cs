using System;
using System.Collections.Generic;
using DepotExtended.DepotVehicles;
using DepotExtended.UI.VehicleEditorWindowViews;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using VoxelTycoon;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Game.UI.VehicleEditorWindowViews;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Rails;
using XMNUtils;

namespace DepotExtended.UI
{
    [HarmonyPatch]
    public static class VehicleEditorWindowExtender
    {
        private static List<VehicleUnitCheckboxGroup> _checkboxGroupsTmp;
        private static bool _doingPrimaryAction;

        private static DepotVehiclesWindow _depotVehiclesWindow;

        private static Dictionary<VehicleEditorWindow, DepotVehiclesWindow> _depotVehiclesWindows = new();

        private static void OnDoPrimaryAction(VehicleEditorWindow instance, bool result)
        {
            _doingPrimaryAction = false;
            if (result && _depotVehiclesWindows.TryGetValue(instance, out DepotVehiclesWindow depotVehiclesWindow) && depotVehiclesWindow.Changed)
            {
                //vehicle was bought / edited
                SimpleLazyManager<RailDepotManager>.Current.UpdateDepotVehicleConsist(depotVehiclesWindow.Depot, depotVehiclesWindow.Consist);
            }
        }

        [UsedImplicitly]
        [HarmonyPrefix]
        [HarmonyPatch(typeof(VehicleEditorWindow), "Initialize")]
        // ReSharper disable once InconsistentNaming
        private static void VehicleEditorWindow_Initialize_prf(VehicleEditorWindow __instance, List<VehicleUnitCheckboxGroup> ____checkboxGroups, VehicleDepot depot, Vector2Int rendererDimensions)
        {
            _depotVehiclesWindow = null;
            _checkboxGroupsTmp = ____checkboxGroups;
            if (depot is RailDepot railDepot)
            { 
                var consist = SimpleLazyManager<RailDepotManager>.Current.GetDepotVehicleConsist(railDepot);
                _depotVehiclesWindow = DepotVehiclesWindow.ShowFor(__instance, consist, railDepot, rendererDimensions);
                _depotVehiclesWindows[__instance] = _depotVehiclesWindow;
                _depotVehiclesWindow.Closed += () => _depotVehiclesWindows.Remove(__instance);
            }
        }

        [UsedImplicitly]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(VehicleEditorWindow), "Initialize")]
        // ReSharper disable once InconsistentNaming
        private static void VehicleEditorWindow_Initialize_pof(VehicleEditorWindow __instance, VehicleDepot depot, Vector2Int rendererDimensions)
        {
        }

        [UsedImplicitly]
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionsView), "Initialize")]
        // ReSharper disable once InconsistentNaming
        private static void ActionsView_Initialize_prf(ActionsView __instance, VehicleEditorWindow vehicleEditorWindow)
        {
            ActionsViewAddition.TryInsertInstance(__instance, vehicleEditorWindow, _checkboxGroupsTmp, _depotVehiclesWindow);
            _depotVehiclesWindow = null;
            _checkboxGroupsTmp = null;
        }

        [UsedImplicitly]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EditVehicleWindow), "HasChanges")]
        // ReSharper disable once InconsistentNaming
        private static void EditVehicleWindow_HasChanges_pof(EditVehicleWindow __instance, ref bool __result)
        {
            if (_doingPrimaryAction && !__result && __instance.Vehicle is Train)
            {
                __result = __instance.transform.Find<ActionsViewAddition>("Root/Content(Clone)/Footer/Actions").Changed ;
            }

            _doingPrimaryAction = false;
        }

        [UsedImplicitly]
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EditVehicleWindow), "DoPrimaryAction")]
        // ReSharper disable once InconsistentNaming
        private static void EditVehicleWindow_DoPrimaryAction_prf()
        {
            _doingPrimaryAction = true;
        }

        [UsedImplicitly]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EditVehicleWindow), "DoPrimaryAction")]
        // ReSharper disable once InconsistentNaming
        private static void EditVehicleWindow_DoPrimaryAction_pof(EditVehicleWindow __instance, bool __result)
        {
            OnDoPrimaryAction(__instance, __result);
        }
 
        [UsedImplicitly]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuyVehicleWindow), "DoPrimaryAction")]
        // ReSharper disable once InconsistentNaming
        private static void BuyVehicleWindow_DoPrimaryAction_pof(BuyVehicleWindow __instance, bool __result)
        {
            OnDoPrimaryAction(__instance, __result);
        }
        
        [UsedImplicitly]
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuyVehicleWindow), "GetPrice")]
        // ReSharper disable once InconsistentNaming
        private static bool BuyVehicleWindow_GetPrice_prf(BuyVehicleWindow __instance, ref double __result)
        {
            if (__instance.Vehicle is Train && _depotVehiclesWindows.TryGetValue(__instance, out DepotVehiclesWindow depotVehiclesWindow))
            {
                __result = depotVehiclesWindow.GetBuyPrice();
                return false;
            }

            return true;
        }
        
    }
}