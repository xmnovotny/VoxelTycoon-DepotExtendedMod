using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using VoxelTycoon;
using VoxelTycoon.Serialization;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Rails;

namespace DepotExtended.DepotVehicles
{
    [SchemaVersion(1)]
    public class RailDepotData
    {
        private readonly Dictionary<VehicleRecipe, List<VehicleRecipeInstance>> _vehicles = new();
        private readonly List<VehicleUnit> _vehicleUnits = new();  //all vehicle units stored in the depot
        private bool _dirty = true;
        private bool _sorted = false;

        public bool IsEmpty => _vehicles.Count == 0;

        public void UpdateVehiclesFromConsists(VehicleConsist consist)
        {
            _vehicles.Clear();
            int itemsCount = consist.Items.Count;
            using PooledList<VehicleRecipeInstance> instances = PooledList<VehicleRecipeInstance>.Take();
            for (int i = 0; i < itemsCount; i++)
            {
                instances.Add(consist.Items[i]);
            }

            foreach (VehicleRecipeInstance instance in 
                from instance in instances
                orderby instance.Original.Power descending, instance.Original.DisplayName.ToString()
                select instance)
            {
                AddVehicleInstance(instance);
            }

            _dirty = true;
            _sorted = true;
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

        public void SellAllVehicles()
        {
            double price = 0;
            MethodInfo onRemoveMInf = AccessTools.Method(typeof(VehicleUnit), "OnRemove");
            foreach (VehicleUnit unit in GetAllUnits())
            {
                onRemoveMInf.Invoke(unit,new object[] {});
                price += unit.GetActualPrice();
            }
            Company.Current.AddMoney(price, BudgetItem.Vehicles);
            _vehicles.Clear();
            _dirty = true;
        }

        public double GetStoredUnitsPrice()
        {
            double price = 0;
            foreach (VehicleUnit unit in GetAllUnits())
            {
                price += unit.GetActualPrice();
            }

            return price;
        }

        public void PutTrainToStoredVehicles(Train train)
        {
            ImmutableList<VehicleRecipeInstance> items = train.Consist.Items;
            int itemsCount = items.Count;
            for (int i = 0; i < itemsCount; i++)
            {
                VehicleRecipeInstance oldInstance = items[i];
                VehicleRecipeInstance newInstance = new(oldInstance.Original);
                newInstance.CopyFrom(oldInstance, true);
                AddVehicleInstance(newInstance);
            }
            train.Remove();
            ReSort();
        }

        internal void Read(StateBinaryReader reader)
        {
            int instancesCount = reader.ReadInt();
            MethodInfo addInstanceMInf = AccessTools.Method(typeof(VehicleRecipeSectionInstance), "AddUnitInstance");
            for (int i = 0; i < instancesCount; i++)
            {
                VehicleRecipe vehicleRecipe = LazyManager<VehicleRecipeManager>.Current.Get(reader.ReadInt());
                VehicleRecipeInstance vehicleRecipeInstance = new (vehicleRecipe)
                {
                    Flipped = reader.ReadBool()
                };
                for (int j = 0; j < vehicleRecipe.Sections.Length; j++)
                {
                    VehicleRecipeSectionInstance vehicleRecipeSectionInstance = vehicleRecipeInstance.Add(vehicleRecipe.Sections[j]);
                    int num3 = reader.ReadInt();
                    for (int k = 0; k < num3; k++)
                    {
                        VehicleUnit vehicleUnit = UnityEngine.Object.Instantiate(reader.ReadAsset<VehicleUnit>());
                        addInstanceMInf.Invoke(vehicleRecipeSectionInstance, new object[] { vehicleUnit });
                        vehicleUnit.Read(reader);
                    }
                }
                AddVehicleInstance(vehicleRecipeInstance);
            }

            _sorted = true;
        }

        internal void Write(StateBinaryWriter writer)
        {
            using PooledList<VehicleRecipeInstance> allInstances = PooledList<VehicleRecipeInstance>.Take();
            foreach (List<VehicleRecipeInstance> recipeInstances in _vehicles.Values)
            {
                foreach (VehicleRecipeInstance instance in recipeInstances)
                {
                    allInstances.Add(instance);
                }
            }
            writer.WriteInt(allInstances.Count);
            foreach (VehicleRecipeInstance instance in allInstances)
            {
                WriteVehicleRecipeInstance(writer, instance);
            }
        }

        private void ReSort()
        {
            if (_sorted)
                return;
            _sorted = true;
            using PooledDictionary<VehicleRecipe, List<VehicleRecipeInstance>> tmpDict = PooledDictionary<VehicleRecipe, List<VehicleRecipeInstance>>.Take(); 
            foreach (KeyValuePair<VehicleRecipe, List<VehicleRecipeInstance>> instancesList in 
                from pair in _vehicles
                orderby pair.Key.Power descending, pair.Key.DisplayName.ToString()
                select pair)
            {
                tmpDict.Add(instancesList.Key, instancesList.Value);   
            }
            _vehicles.Clear();
            foreach (KeyValuePair<VehicleRecipe, List<VehicleRecipeInstance>> pair in tmpDict)
                _vehicles.Add(pair.Key, pair.Value);
        }

        private void WriteVehicleRecipeInstance(StateBinaryWriter writer, VehicleRecipeInstance vehicleRecipeInstance)
        {
            writer.WriteInt(vehicleRecipeInstance.Original.AssetId);
            writer.WriteBool(vehicleRecipeInstance.Flipped);
            ImmutableList<VehicleRecipeSectionInstance> sections = vehicleRecipeInstance.Sections;
            for (int j = 0; j < sections.Count; j++)
            {
                VehicleRecipeSectionInstance vehicleRecipeSectionInstance = vehicleRecipeInstance.Sections[j];
                writer.WriteInt(vehicleRecipeSectionInstance.Units.Count);
                for (int k = 0; k < vehicleRecipeSectionInstance.Units.Count; k++)
                {
                    VehicleUnit vehicleUnit = vehicleRecipeSectionInstance.Units[k];
                    writer.WriteAsset(vehicleUnit.SharedData.AssetId);
                    vehicleUnit.Write(writer);
                }
            }
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
               _sorted = false;
            }
            vehicleList.Add(instance);
            _dirty = true;
        }
    }
}