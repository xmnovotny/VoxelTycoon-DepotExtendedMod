using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using VoxelTycoon.Tracks;

namespace DepotExtended.DepotVehicles
{
    public class RailDepotData
    {
        private Dictionary<VehicleRecipe, List<VehicleRecipeInstance>> _vehicles = new();
        private List<VehicleUnit> _vehicleUnits = new();  //all vehicle units stored in the depot
        private bool _dirty = true;

        public bool IsEmpty => _vehicles.Count == 0;

        public void UpdateVehiclesFromConsists(VehicleConsist consist)
        {
            _vehicles.Clear();
            int itemsCount = consist.Items.Count;
            for (int i = 0; i < itemsCount; i++)
            {
                AddVehicleInstance(consist.Items[i]);
            }

            _dirty = true;
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

        public IReadOnlyList<VehicleUnit> GetAllUnits()
        {
            if (_dirty) 
                FillUnits();

            return _vehicleUnits;
        }

        private void FillUnits()
        {
            _dirty = false;
            _vehicleUnits.Clear();
            foreach (List<VehicleRecipeInstance> recipeInstances in _vehicles.Values)
            {
                foreach (VehicleRecipeInstance recipeInstance in recipeInstances)
                {
                    recipeInstance.FillAllUnits(_vehicleUnits);
                }
            }
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