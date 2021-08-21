using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using VoxelTycoon.Tracks;

namespace DepotExtended.DepotVehicles
{
    public class RailDepotData
    {
        private Dictionary<VehicleRecipe, List<VehicleRecipeInstance>> _vehicles = new();

        public bool IsEmpty => _vehicles.Count == 0;

        public void UpdateVehiclesFromConsists(VehicleConsist consist)
        {
            _vehicles.Clear();
            for (int i = consist.Items.Count - 1; i >= 0; i--)
            {
                AddVehicleInstance(consist.Items[i]);
            }
        }

        public VehicleConsist CreateFullConsists()
        {
            VehicleConsist consist = new ();
            foreach (List<VehicleRecipeInstance> instances in _vehicles.Values)
            {
                foreach (VehicleRecipeInstance instance in instances)
                {
                    consist.Add(instance.Original).CopyFrom(instance, true);
                }
            }

            return consist;
        }
        
        private void AddVehicleInstance([NotNull] VehicleRecipeInstance instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (!_vehicles.TryGetValue(instance.Original, out List<VehicleRecipeInstance> vehicleList))
            {
               vehicleList = _vehicles[instance.Original] = new List<VehicleRecipeInstance>();
            }
            vehicleList.Add(instance);
        }
    }
}