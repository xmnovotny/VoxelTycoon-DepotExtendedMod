using System;
using System.Collections.Generic;
using VoxelTycoon;
using VoxelTycoon.Tracks;
using XMNUtils;

namespace DepotExtended
{
    internal static class VehicleConsistsHelper
    {
        private static Func<VehicleConsist, List<VehicleRecipeInstance>> _itemsGetter;

        private static List<VehicleRecipeInstance> GetRecipeInstances(VehicleConsist consist)
        {
            if (_itemsGetter == null)
                _itemsGetter = SimpleDelegateFactory.FieldGet<VehicleConsist, List<VehicleRecipeInstance>>("_recipeInstances");
            return _itemsGetter.Invoke(consist);
        }
        
        public static void MoveBetween(VehicleConsist sourceConsist, VehicleConsist targetConsist, VehicleRecipeInstance instance, int? targetIndex = null)
        {
            List<VehicleRecipeInstance> sourceItems = GetRecipeInstances(sourceConsist);
            List<VehicleRecipeInstance> targetItems = GetRecipeInstances(targetConsist);
            int idx = sourceConsist.IndexOf(instance);
            if (idx < 0)
                throw new InvalidOperationException("Instance is not in the source consist");
            
            if (targetIndex.HasValue)
                targetItems.Insert(targetIndex.Value, instance);
            else
                targetItems.Add(instance);
            sourceItems.RemoveAt(idx);
        }
        
        public static void FillAllUnits(this VehicleConsist consist, List<VehicleUnit> units)
        {
            ImmutableList<VehicleRecipeInstance> instances = consist.Items;
            for (int i = 0; i < instances.Count; i++)
            {
                ImmutableList<VehicleRecipeSectionInstance> sections = instances[i].Sections;
                for (int j = 0; j < sections.Count; j++)
                {
                    ImmutableList<VehicleUnit> sectionUnits = sections[j].Units;
                    for (int k = 0; k < sectionUnits.Count; k++)
                    {
                        units.Add(sectionUnits[k]);
                    }
                }
            }
        }
        
        public static void FillAllUnits(this VehicleRecipeInstance recipeInstance, List<VehicleUnit> units)
        {
            ImmutableList<VehicleRecipeSectionInstance> sections = recipeInstance.Sections;
            for (int j = 0; j < sections.Count; j++)
            {
                ImmutableList<VehicleUnit> sectionUnits = sections[j].Units;
                for (int k = 0; k < sectionUnits.Count; k++)
                {
                    units.Add(sectionUnits[k]);
                }
            }
        }
        
        public static double GetPrice(this VehicleConsist consist, bool actual)
        {
            double price = 0.0;
            for (int i = 0; i < consist.Items.Count; i++)
            {
                price += consist.Items[i].GetPrice(actual);
            }
            return price;
        }

        public static void FlipRecipeInstance(VehicleRecipeInstance vehicleRecipeInstance)
        {
            vehicleRecipeInstance.Flipped = !vehicleRecipeInstance.Flipped;
            ImmutableList<VehicleRecipeSectionInstance> sections = vehicleRecipeInstance.Sections;
            for (int j = 0; j < sections.Count; j++)
            {
                VehicleRecipeSectionInstance vehicleRecipeSectionInstance = vehicleRecipeInstance.Sections[j];
                for (int k = 0; k < vehicleRecipeSectionInstance.Units.Count; k++)
                {
                    VehicleUnit vehicleUnit = vehicleRecipeSectionInstance.Units[k];
                    vehicleUnit.Flipped = !vehicleUnit.Flipped;
                }
            }
        }

    }
}