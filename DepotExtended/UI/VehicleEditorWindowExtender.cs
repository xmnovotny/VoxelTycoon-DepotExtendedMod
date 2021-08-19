using System.Collections.Generic;
using DepotExtended.UI.VehicleEditorWindowViews;
using HarmonyLib;
using JetBrains.Annotations;
using VoxelTycoon;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Game.UI.VehicleEditorWindowViews;
using VoxelTycoon.Tracks.Rails;

namespace DepotExtended.UI
{
    [HarmonyPatch]
    public static class VehicleEditorWindowExtender
    {
        private static List<VehicleUnitCheckboxGroup> _checkboxGroupsTmp;
        private static bool _doingPrimaryAction;
        
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

        [UsedImplicitly]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EditVehicleWindow), "HasChanges")]
        // ReSharper disable once InconsistentNaming
        private static void EditVehicleWindow_HasChanges_pof(EditVehicleWindow __instance, ref bool __result)
        {
            if (_doingPrimaryAction && !__result && __instance.Vehicle is Train)
            {
                __result = __instance.transform.Find<ActionsViewAddition>("Root/Content(Clone)/Footer/Actions").Changed;
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
        private static void EditVehicleWindow_DoPrimaryAction_pof()
        {
            _doingPrimaryAction = false;
        }
    }
}