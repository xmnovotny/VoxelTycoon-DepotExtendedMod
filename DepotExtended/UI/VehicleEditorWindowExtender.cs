using System.Collections.Generic;
using DepotExtended.UI.VehicleEditorWindowViews;
using HarmonyLib;
using JetBrains.Annotations;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Game.UI.VehicleEditorWindowViews;
using VoxelTycoon.Tracks;

namespace DepotExtended.UI
{
    [HarmonyPatch]
    public static class VehicleEditorWindowExtender
    {
        private static List<VehicleUnitCheckboxGroup> _checkboxGroupsTmp;
        
        [UsedImplicitly]
        [HarmonyPrefix]
        [HarmonyPatch(typeof(VehicleEditorWindow), "Initialize")]
        // ReSharper disable once InconsistentNaming
        private static void ActionsView_Initialize_prf(List<VehicleUnitCheckboxGroup> ____checkboxGroups)
        {
            _checkboxGroupsTmp = ____checkboxGroups;
        }

        [UsedImplicitly]
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionsView), "Initialize")]
        // ReSharper disable once InconsistentNaming
        private static void ActionsView_Initialize_prf(ActionsView __instance, VehicleEditorWindow vehicleEditorWindow)
        {
            ActionsViewAddition.TryInsertInstance(__instance, vehicleEditorWindow, _checkboxGroupsTmp);
            _checkboxGroupsTmp = null;
        }
    }
}