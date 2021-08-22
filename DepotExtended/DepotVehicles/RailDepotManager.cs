using System;
using System.Collections.Generic;
using DepotExtended.UI;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using VoxelTycoon;
using VoxelTycoon.Tools.Remover.Handlers;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Rails;
using XMNUtils;

namespace DepotExtended.DepotVehicles
{
    [HarmonyPatch]
    public class RailDepotManager: SimpleLazyManager<RailDepotManager>
    {
        //TODO: Save and load depot content
        //TODO: Allow put a whole train to the depot content (from depot window)
        //TODO: Allow sell all stored vehicles (via button in the stored vehicles display or by selling all button) 
        private Dictionary<RailDepot, RailDepotData> _depotData = new();

        public VehicleConsist GetDepotVehicleConsist(RailDepot depot)
        {
            if (_depotData.TryGetValue(depot, out RailDepotData data))
            {
                return data.CreateFullConsists();
            }

            return new VehicleConsist();
        }

        [CanBeNull]
        public IReadOnlyList<VehicleUnit> GetDepotVehicleUnits(RailDepot depot)
        {
            return _depotData.TryGetValue(depot, out RailDepotData data) ? data.GetAllUnits() : null;
        }

        public void UpdateDepotVehicleConsist(RailDepot depot, VehicleConsist consist)
        {
            if (!_depotData.TryGetValue(depot, out RailDepotData data) && consist.Items.Count > 0)
            {
                data = _depotData[depot] = new RailDepotData();
            }

            if (data != null)
            {
                data.UpdateVehiclesFromConsists(consist);
                if (data.IsEmpty)
                    _depotData.Remove(depot);
                
                DepotWindowExtender.OnDepotVehiclesChanged(depot);
            }
        }

        private RailDepotData GetOrCreateDepotData(RailDepot depot)
        {
            if (!_depotData.TryGetValue(depot, out RailDepotData data))
            {
                data = _depotData[depot] = new RailDepotData();
            }

            return data;
        }

        [UsedImplicitly]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(VehicleDepotRemoverHandler), "CanRemoveInternal")]
        // ReSharper disable InconsistentNaming
        private static void VehicleDepotRemoverHandler_CanRemoveInternal_pof(VehicleDepotRemoverHandler __instance, List<VehicleDepot> targets, ref string reason, ref bool __result)
        {
            if (CurrentWithoutInit != null && __result)
            {
                foreach (VehicleDepot vehicleDepot in targets)
                {
                    if (vehicleDepot is RailDepot railDepot && Current._depotData.ContainsKey(railDepot))
                    {
                        reason = S.BuildingIsNotEmpty;
                        __result = false;
                        return;
                    }
                }
            }            
        }
        
    }
}