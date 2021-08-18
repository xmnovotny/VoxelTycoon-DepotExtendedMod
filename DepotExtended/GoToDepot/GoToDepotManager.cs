﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using VoxelTycoon;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Rails;
using VoxelTycoon.Tracks.Roads;
using VoxelTycoon.Tracks.Tasks;
using XMNUtils;

namespace DepotExtended.GoToDepot
{
    //TODO: Saving and loading
    [HarmonyPatch]
    public class GoToDepotManager: SimpleManager<GoToDepotManager>
    {
        private readonly Dictionary<Vehicle, GoToDepotVehicleData> _vehiclesToDepot = new();

        private void GoToDepot(VehicleDepot depot, [NotNull] GoToDepotOverrideTask task)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));
            _vehiclesToDepot[task.Vehicle] = new GoToDepotVehicleData(depot, task);
        }

        [UsedImplicitly]
        private static IVehicleDestination GetDepotVehicleDestination(Vehicle vehicle)
        {
            if (Current != null && vehicle.Schedule.OverrideTask is GoToDepotOverrideTask gotoTask && Current._vehiclesToDepot.TryGetValue(vehicle, out GoToDepotVehicleData data))
            {
                data.Invalidate();
                if (!data.IsValid || data.Task != gotoTask)
                {
                    Current._vehiclesToDepot.Remove(vehicle);
                    return null;
                }

                return data;
            }

            return null;
        }

        private IEnumerator RunGoToDepotTask(GoToDepotOverrideTask __instance, IEnumerator origEnumerator)
        {
            GoToDepotVehicleData data;
            while ((data = _vehiclesToDepot.GetValueOrDefault(__instance.Vehicle)) != null && __instance.Vehicle.FrontBound.Connection != data.TargetConnection)
            {
                yield return new WaitForSeconds(1);
            }
            //remove data, they are no longer needed
            _vehiclesToDepot.Remove(__instance.Vehicle);
            yield return origEnumerator;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GoToDepotDelayedAction), "Initialize")]
        // ReSharper disable once InconsistentNaming
        private static void GoToDepotDelayedAction_Initialize_pof(ref float ____delay)
        {
            if (Current != null)
                ____delay = 1f;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GoToDepotOverrideTask), "Initialize")]
        // ReSharper disable once InconsistentNaming
        private static void GoToDepotOverrideTask_Initialize_pof(GoToDepotOverrideTask __instance, VehicleDepot depot)
        {
            Current?.GoToDepot(depot, __instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GoToDepotOverrideTask), "Run")]
        // ReSharper disable once InconsistentNaming
        private static void GoToDepotOverrideTask_Run_pof(GoToDepotOverrideTask __instance, ref IEnumerator __result)
        {
            if (Current != null)
            {
                __result = Current.RunGoToDepotTask(__instance, __result);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(VehicleSchedule), "PushOverrideTask")]
        // ReSharper disable once InconsistentNaming
        private static void GoToDepotOverrideTask_PushOverrideTask_prf(VehicleSchedule __instance, OverrideTask task)
        {
            GoToDepotVehicleData data;
            if (Current != null && __instance.Vehicle is Train &&
                 (data = Current._vehiclesToDepot.GetValueOrDefault(__instance.Vehicle)) != null)
            {
                if (data.TurningTask == null && __instance.OverrideTask is GoToDepotOverrideTask gotoTask &&
                    task is TurnAroundOverrideTask turnTask && data.Task == gotoTask)
                {
                    //train starts the turn around override task to be able to get to the depot
                    data.TurningTask = turnTask;
                }
                else if (task != data.Task)
                {
                    //in other cases remove data
                    Current._vehiclesToDepot.Remove(__instance.Vehicle);
                }
            }
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(VehicleTask), "OnCompleted")]
        // ReSharper disable once InconsistentNaming
        private static void GoToDepotOverrideTask_OnCompleted_pof(VehicleTask __instance)
        {
            GoToDepotVehicleData data;
            if (Current != null && __instance is TurnAroundOverrideTask && __instance.Vehicle is Train && (data = Current._vehiclesToDepot.GetValueOrDefault(__instance.Vehicle)) != null)
            {
                if (data.TurningTask == __instance)
                {
                    //turn around task is completed, restore original go to depot task
                    data.TurningTask = null;
                    __instance.Vehicle.Schedule.PushOverrideTask(data.Task);
                }
                else
                    Current._vehiclesToDepot.Remove(__instance.Vehicle);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TrackPathNodeManager<RoadPathNodeManager, RoadPathNode>), "CanPass")]
        // ReSharper disable once InconsistentNaming
        private static void RoadPathNodeManager_CanPass_pof(RoadPathNodeManager __instance, ref bool __result, TrackConnection connection1)
        {
            if (Current != null && __result)
            {
                if (connection1.Track.Parent is RoadDepot depot && connection1 == depot.SpawnConnection.OuterConnections[0].InnerConnection.OuterConnections[0].InnerConnection)
                    __result = false;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Vehicle), "UpdateDestination")]
        private static IEnumerable<CodeInstruction> Vehicle_UpdateDestination_trans(IEnumerable<CodeInstruction> instructions)
        {
            bool found = false;
            bool finished = false;
            foreach (CodeInstruction instruction in instructions)
            {
                if (found && !finished)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0); //load this to the stack = current Vehicle, will be as 1. argument of static function
                    yield return CodeInstruction.Call(typeof(GoToDepotManager), "GetDepotVehicleDestination");  //call GetDepotVehicleDestination
                    yield return new CodeInstruction(OpCodes.Stloc_0); //store result to local variable "vehicleDestination1" 
                    finished = true; //that is all
                }

                if (finished)
                {
                    yield return instruction;
                    continue;
                }

                found = instruction.opcode == OpCodes.Stloc_2; //instruction vehicleDestination2 = null, after that we will place out code
                yield return instruction;
            }
        }

    }
}