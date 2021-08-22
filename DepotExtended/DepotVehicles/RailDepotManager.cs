using System;
using System.Collections.Generic;
using DepotExtended.UI;
using JetBrains.Annotations;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Rails;
using XMNUtils;

namespace DepotExtended.DepotVehicles
{
    public class RailDepotManager: SimpleLazyManager<RailDepotManager>
    {
        //TODO: Block removing depot when there are some vehicles
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
    }
}