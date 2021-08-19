using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Notifications;
using VoxelTycoon.Serialization;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Rails;
using VoxelTycoon.Tracks.Roads;
using VoxelTycoon.Tracks.Tasks;
using XMNUtils;

namespace DepotExtended.GoToDepot
{
    [HarmonyPatch]
    [SchemaVersion(2)]
    public class GoToDepotManager: SimpleManager<GoToDepotManager>
    {
        private readonly Dictionary<Vehicle, GoToDepotVehicleData> _vehiclesToDepot = new();
        private readonly HashSet<Vehicle> _trainEnteringDepot = new ();
        private Action<TrackUnit,bool> _trainUpdatePosition;
        private Action<TrackUnit, TrackPosition> _setTrainFrontBound;
        private Func<TrackUnit, List<TrackUnitPoint>> _trainPointsGetter;
        private Action<TrackUnit, float> _setTrainLength;
        private Action<Vehicle> _trainUpdateLengthAndPoints;

        private GoToDepotVehicleData GoToDepot(VehicleDepot depot, [NotNull] GoToDepotOverrideTask task)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));
            return _vehiclesToDepot[task.Vehicle] = new GoToDepotVehicleData(task.Vehicle, depot, task);
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
            GoToDepotVehicleData data = _vehiclesToDepot.GetValueOrDefault(__instance.Vehicle);
            ReusableWaitForSeconds wfs = new ReusableWaitForSeconds();
            if (data != null)
            {
                while (__instance.Vehicle.FrontBound.Connection != data.TargetConnection)
                {
                    yield return wfs.Wait(0.1f);
                }
            }

            if (__instance.Vehicle is Train train)
            {
                _trainEnteringDepot.Add(train);
                while (_trainEnteringDepot.Contains(train))
                    yield return wfs.Wait(0.1f);
            }

            //remove data, they are no longer needed
            _vehiclesToDepot.Remove(__instance.Vehicle);
            yield return origEnumerator;
        }

        internal void Read(StateBinaryReader reader)
        {
            if (SchemaVersion<GoToDepotManager>.AtLeast(1))
            {
                int count = reader.ReadInt();
                if (count > 0)
                {
                    MethodInfo mInf = AccessTools.Method(typeof(GoToDepotOverrideTask), "Read");
                    for (int i = 0; i < count; i++)
                    {
                        Vehicle vehicle = LazyManager<VehicleManager>.Current.FindById(reader.ReadInt());
                        GoToDepotOverrideTask task = new(vehicle);
                        VehicleDepot depot = reader.ReadBuilding<VehicleDepot>();
                        mInf.Invoke(task, new object[] {reader});
                        if (vehicle.Schedule.OverrideTask is TurnAroundOverrideTask turnTask)
                        {
                            GoToDepotVehicleData data = GoToDepot(depot, task);
                            data.TurningTask = turnTask;
                        }
                    }
                }
            }

            ReadEnteringTrains(reader);
        }

        internal void Write(StateBinaryWriter writer)
        {
            using PooledList<KeyValuePair<Vehicle,GoToDepotVehicleData>> toSave = PooledList<KeyValuePair<Vehicle,GoToDepotVehicleData>>.Take();
            foreach (KeyValuePair<Vehicle,GoToDepotVehicleData> pair in _vehiclesToDepot)
            {
                //save only when there is turnaround task and vehicle is moving to the depot
                if (pair.Key.Schedule.OverrideTask is TurnAroundOverrideTask turnTask && pair.Value.TurningTask == turnTask) 
                    toSave.Add(pair);
            }
            writer.WriteInt(toSave.Count);
            if (toSave.Count != 0)
            {
                MethodInfo mInf = AccessTools.Method(typeof(GoToDepotOverrideTask), "Write");
                foreach (KeyValuePair<Vehicle, GoToDepotVehicleData> pair in toSave)
                {
                    GoToDepotVehicleData data = pair.Value;
                    writer.WriteInt(pair.Key.Id); //vehicle
                    writer.WriteBuilding(data.Depot);
                    mInf.Invoke(data.Task, new object[] {writer});
                }

            }

            //write data of trains entering depot
            WriteEnteringTrains(writer);
        }

        internal void TrainUpdatePosition(Train train)
        {
            if (_trainUpdatePosition == null)
            {
                MethodInfo mInf = typeof(TrackUnit).GetMethod("UpdatePosition", BindingFlags.NonPublic | BindingFlags.Instance);
                _trainUpdatePosition = (Action<TrackUnit, bool>)Delegate.CreateDelegate(typeof(Action<TrackUnit, bool>), mInf);
            }

            _trainUpdatePosition(train, true);
        }

        internal void SetTrainFrontBound(Train train, TrackPosition position)
        {
            if (_setTrainFrontBound == null)
            {
                MethodInfo mInf = typeof(TrackUnit).GetMethod("set_FrontBound", BindingFlags.NonPublic | BindingFlags.Instance);
                _setTrainFrontBound = (Action<TrackUnit, TrackPosition>)Delegate.CreateDelegate(typeof(Action<TrackUnit, TrackPosition>), mInf);
            }

            _setTrainFrontBound(train, position);
        }

        internal void SetTrainLength(Train train, float length)
        {
            if (_setTrainLength == null)
            {
                MethodInfo mInf = typeof(TrackUnit).GetMethod("set_Length", BindingFlags.NonPublic | BindingFlags.Instance);
                _setTrainLength = (Action<TrackUnit, float>)Delegate.CreateDelegate(typeof(Action<TrackUnit, float>), mInf);
            }

            _setTrainLength(train, length);
        }

        internal void TrainUpdateLengthAndPoints(Train train)
        {
            if (_trainUpdateLengthAndPoints == null)
            {
                MethodInfo mInf = typeof(Vehicle).GetMethod("UpdateLengthAndPoints", BindingFlags.NonPublic | BindingFlags.Instance);
                _trainUpdateLengthAndPoints = (Action<Vehicle>)Delegate.CreateDelegate(typeof(Action<Vehicle>), mInf);
            }

            _trainUpdateLengthAndPoints(train);
        }

        internal List<TrackUnitPoint> GetVehiclePoints(Vehicle train)
        {
            if (_trainPointsGetter == null)
                _trainPointsGetter = SimpleDelegateFactory.FieldGet<TrackUnit, List<TrackUnitPoint>>("Points");

            return _trainPointsGetter.Invoke(train);
        }

        private void ReadEnteringTrains(StateBinaryReader reader)
        {
            if (!SchemaVersion<GoToDepotManager>.AtLeast(2))
                return;
            int enteringCount = reader.ReadInt();
            for (int i = 0; i < enteringCount; i++)
            {
                int vehicleID = reader.ReadInt();
                if (vehicleID > 0)
                {
                    Vehicle vehicle = LazyManager<VehicleManager>.Current.FindById(vehicleID);
                    _trainEnteringDepot.Add(vehicle);
                    GoToDepotVehicleData data = _vehiclesToDepot[vehicle];
                    data.ReadEnteringData(reader);
                }
            }
        }

        private void WriteEnteringTrains(StateBinaryWriter writer)
        {
            writer.WriteInt(_trainEnteringDepot.Count);
            foreach (Vehicle vehicle in _trainEnteringDepot)
            {
                if (_vehiclesToDepot.TryGetValue(vehicle, out GoToDepotVehicleData data))
                {
                    writer.WriteInt(vehicle.Id);
                    data.WriteEnteringData(writer);
                }
                else
                    writer.WriteInt(0); //invalid data
            }
        }

        #region HARMONY
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GoToDepotDelayedAction), "Initialize")]
        [UsedImplicitly]
        // ReSharper disable once InconsistentNaming
        private static void GoToDepotDelayedAction_Initialize_pof(VehicleDepot depot, ref float ____delay)
        {
            if (Current != null)
                ____delay = depot is RailDepot ? 0.1f : 1f;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GoToDepotOverrideTask), "Initialize")]
        [UsedImplicitly]
        // ReSharper disable once InconsistentNaming
        private static void GoToDepotOverrideTask_Initialize_pof(GoToDepotOverrideTask __instance, VehicleDepot depot)
        {
            Current?.GoToDepot(depot, __instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GoToDepotOverrideTask), "Read")]
        [UsedImplicitly]
        // ReSharper disable once InconsistentNaming
        private static void GoToDepotOverrideTask_Read_pof(GoToDepotOverrideTask __instance, GoToDepotDelayedAction ____action)
        {
            if (Current != null)
            {
                VehicleDepot depot = AccessTools.Field(typeof(GoToDepotDelayedAction), "_depot").GetValue(____action) as VehicleDepot;
                if (depot != null)
                    Current.GoToDepot(depot, __instance);
            }
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GoToDepotOverrideTask), "Run")]
        [UsedImplicitly]
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
        [UsedImplicitly]
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
        [UsedImplicitly]
        // ReSharper disable once InconsistentNaming
        private static void VehicleTask_OnCompleted_pof(VehicleTask __instance)
        {
            GoToDepotVehicleData data;
            if (Current != null)
            {
                if (__instance is TurnAroundOverrideTask && __instance.Vehicle is Train && (data = Current._vehiclesToDepot.GetValueOrDefault(__instance.Vehicle)) != null)
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
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GoToDepotDelayedAction), "DoDelayedAction")]
        [UsedImplicitly]
        // ReSharper disable once InconsistentNaming`
        private static void GoToDepotDelayedAction_DoDelayedAction_pof(GoToDepotDelayedAction __instance, Vehicle ____vehicle, VehicleDepot ____depot)
        {
            if (Current != null && ____depot != null)
            {
                //TODO: Add translation
                Manager<NotificationManager>.Current.Push($"Vehicle arrived to the depot", $"{____vehicle.Name} just arrived to the {____depot.Name}", 
                    new GoToVehicleNotificationAction(____vehicle));
                //restore original train points
                if (____vehicle is Train train)
                {
                    Current.TrainUpdateLengthAndPoints(train);                    
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TrackPathNodeManager<RoadPathNodeManager, RoadPathNode>), "CanPass")]
        [UsedImplicitly]
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
        [UsedImplicitly]
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
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Vehicle), "Move")]
        [UsedImplicitly]
        // ReSharper disable once InconsistentNaming
        private static bool Vehicle_Move_pof(Vehicle __instance)
        {
            if (__instance is Train train && Current?._trainEnteringDepot.Count>0 && Current._trainEnteringDepot.Contains(__instance))
            {
                if (!Current._vehiclesToDepot.TryGetValue(train, out GoToDepotVehicleData data))
                {
                    Current._trainEnteringDepot.Remove(train);
                    return true;
                }

                if (data.MoveTrainInDepot())
                {
                    //done moving train in the depot
                    Current._trainEnteringDepot.Remove(train);
                }
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VehicleWindowHeaderView), "Update")]
        [UsedImplicitly]
        // ReSharper disable once InconsistentNaming`
        private static void VehicleWindowHeaderView_Update_pof(VehicleWindowHeaderView __instance, Vehicle ____vehicle)
        {
            if (Current != null && ____vehicle is Train {IsWrecked: false, IsInDepot: false} train)
            {
                //test if we are completely out of the depot
                if (train.RearBound.Position == 0f && train.RearBound.Connection.OuterConnectionCount == 0 && train.RearBound.Connection.Track.Parent is RailDepot  //train is leaving depot 
                 || train.Schedule.OverrideTask is GoToDepotOverrideTask && train.FrontBound.Position >= 1f && train.FrontBound.Connection.InnerConnection.OuterConnectionCount == 0 && train.FrontBound.Connection.Track.Parent is RailDepot)  //train is entering depot
                {
                    //train is leaving depot
                    __instance.GoHomeButton.interactable = false;
                    __instance.TurnAroundButton.interactable = false;
/*                    if (train.Schedule.OverrideTask is GoToDepotOverrideTask)
                        __instance.OnOffToggle.Toggle.interactable = false;*/
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VehicleEnabledToggle), "Initialize")]
        [UsedImplicitly]
        // ReSharper disable once InconsistentNaming`
        private static void VehicleEnabledToggle_Initialize_pof(VehicleEnabledToggle __instance, Vehicle vehicle)
        {
            Toggle.ToggleEvent newEvent = new Toggle.ToggleEvent();
            var origEvent = __instance.Toggle.onValueChanged;
            newEvent.AddListener(delegate (bool value)
                {
                    if (!value && vehicle is Train {IsWrecked: false, IsInDepot: false} train)
                    {
                        //test if we are completely out of the depot
                        if (train.Schedule.OverrideTask is GoToDepotOverrideTask && train.FrontBound.Position >= 1f && train.FrontBound.Connection.InnerConnection.OuterConnectionCount == 0 &&
                            train.FrontBound.Connection.Track.Parent is RailDepot) //train is entering depot
                        {
                            //disable event
                            return;
                        }
                    }
                    origEvent.Invoke(value);
                }
            );
            __instance.Toggle.onValueChanged = newEvent;
        }

       
#endregion
    }
}