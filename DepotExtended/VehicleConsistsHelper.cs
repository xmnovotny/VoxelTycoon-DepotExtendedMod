using System;
using System.Collections.Generic;
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

    }
}