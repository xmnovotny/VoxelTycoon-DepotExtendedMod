using VoxelTycoon;
using VoxelTycoon.Localization;

namespace DepotExtended
{
    public static class S
    {
        public static string UnitMoveLeftTooltip => LazyManager<LocaleManager>.Current.Locale.GetString("depot_extended/unit_move_left_tooltip");
        public static string UnitMoveRightTooltip => LazyManager<LocaleManager>.Current.Locale.GetString("depot_extended/unit_move_right_tooltip");
        public static string UnitMoveToDepotTooltip => LazyManager<LocaleManager>.Current.Locale.GetString("depot_extended/unit_move_to_depot_tooltip");
        public static string SelectUnitsFirst => LazyManager<LocaleManager>.Current.Locale.GetString("depot_extended/select_units_first");
        public static string OneUnitMustRemain => LazyManager<LocaleManager>.Current.Locale.GetString("depot_extended/one_unit_must_remain");
        public static string UnitMoveToTrain => LazyManager<LocaleManager>.Current.Locale.GetString("depot_extended/unit_move_to_train_tooltip");
        public static string VehicleUnitsInDepot => LazyManager<LocaleManager>.Current.Locale.GetString("depot_extended/vehicle_units_in_depot");
        public static string AllStoredUnits => LazyManager<LocaleManager>.Current.Locale.GetString("depot_extended/all_stored_units");
        public static string StoredVehicleUnits => LazyManager<LocaleManager>.Current.Locale.GetString("depot_extended/stored_vehicle_units");
        public static string MoveAllVehiclesToStorageConfirm => LazyManager<LocaleManager>.Current.Locale.GetString("depot_extended/move_all_vehicles_to_storage_confirm");
        public static string VehicleArrivedDepotTitle => LazyManager<LocaleManager>.Current.Locale.GetString("depot_extended/vehicle_arrived_depot_title");
        public static string VehicleArrivedDepotMessage => LazyManager<LocaleManager>.Current.Locale.GetString("depot_extended/vehicle_arrived_depot_message");

    }
}