using ModSettingsUtils;
using Newtonsoft.Json;

namespace DepotExtended
{
    [JsonObject(MemberSerialization.OptOut)]
    internal class Settings : ModSettings<Settings>
    {
        private bool _vehiclesRidesToDepot = true;

        public bool VehiclesRidesToDepot
        {
            get => _vehiclesRidesToDepot;
            set => SetProperty(value, ref _vehiclesRidesToDepot);
        }
    }
}