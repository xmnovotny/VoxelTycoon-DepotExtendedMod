using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using VoxelTycoon;
using VoxelTycoon.Serialization;
using VoxelTycoon.Tracks;

namespace DepotExtended.DepotVehicles
{
    [SchemaVersion(1)]
    public class RailDepotData
    {
        private readonly Dictionary<VehicleRecipe, List<VehicleRecipeInstance>> _vehicles = new();
        private readonly List<VehicleUnit> _vehicleUnits = new();  //all vehicle units stored in the depot
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
            }
            vehicleList.Add(instance);
        }
    }
}