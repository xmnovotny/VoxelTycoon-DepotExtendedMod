using System.Collections.Generic;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Rails;
using XMNUtils;

namespace DepotExtended.DepotVehicles
{
    public class RailDepotManager: SimpleLazyManager<RailDepotManager>
    {
        //TODO: Block removing depot when there are some vehicles
        //TODO: Display depot content in the depot window
        //TODO: Save and load depot content
        private Dictionary<RailDepot, RailDepotData> _depotData = new();

        public VehicleConsist GetDepotVehicleConsist(RailDepot depot)
        {
            if (_depotData.TryGetValue(depot, out RailDepotData data))
            {
                return data.CreateFullConsists();
            }

            return new VehicleConsist();
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