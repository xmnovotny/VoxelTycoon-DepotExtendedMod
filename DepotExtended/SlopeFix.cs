using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using VoxelTycoon.Tracks;

namespace DepotExtended
{
    [HarmonyPatch]
    public static class SlopeFix
    {
        [UsedImplicitly]
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Vehicle), "GetSlopeForce")]
        // ReSharper disable InconsistentNaming
        private static bool Vehicle_GetSlopeForce_prf(Vehicle __instance, bool useMaxWeight, out float __result)
        {
            float num = 0f;
            int unitsCount = __instance.Units.Count;
            for (int i = 0; i < unitsCount; i++)
            {
                VehicleUnit vehicleUnit = __instance.Units[i];
                float num2 = Vector3.Dot(vehicleUnit.transform.forward, Vector3.down) * (5f / 26f);
                float num3 = (useMaxWeight ? vehicleUnit.MaxWeight : vehicleUnit.Weight) * num2 * (vehicleUnit.Flipped ? -1 : 1);
                num += num3;
            }
            __result = num * 9.8f * (__instance.Flipped ? -1 : 1);
            return false;
        }

    }
}