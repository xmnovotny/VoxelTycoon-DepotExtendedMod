using System;
using System.Collections.Generic;
using DepotExtended.UI;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using VoxelTycoon;
using VoxelTycoon.Serialization;
using VoxelTycoon.Tools.Remover.Handlers;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Rails;
using XMNUtils;

namespace DepotExtended.DepotVehicles
{
    [HarmonyPatch]
    [SchemaVersion(1)]
    public class RailDepotManager: SimpleLazyManager<RailDepotManager>
    {
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

        internal void Read(StateBinaryReader reader)
        {
            if (SchemaVersion<RailDepotManager>.AtLeast(1))
            {
                int depotCount = reader.ReadInt();
                for (int i = 0; i < depotCount; i++)
                {
                    RailDepot depot = reader.ReadBuilding<RailDepot>();
                    RailDepotData data = new();
                    data.Read(reader);
                    _depotData.Add(depot, data);
                }
            }
        }

        internal void Write(StateBinaryWriter writer)
        {
            writer.WriteInt(_depotData.Count);
            foreach (KeyValuePair<RailDepot,RailDepotData> pair in _depotData)
            {
                writer.WriteBuilding(pair.Key);
                pair.Value.Write(writer);
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