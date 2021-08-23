using DepotExtended.DepotVehicles;
using DepotExtended.GoToDepot;
using DepotExtended.UI;
using HarmonyLib;
using ModSettingsUtils;
using VoxelTycoon.Modding;
using VoxelTycoon.Serialization;
using VoxelTycoon.Tracks.Rails;
using XMNUtils;

namespace DepotExtended
{
    [HarmonyPatch]
    [SchemaVersion(2)]
    public class DepotExtendedMod: Mod
    {
        private Harmony _harmony;
        private const string HarmonyID = "cz.xmnovotny.depotextended.patch";

        protected override void Initialize()
        {
            Harmony.DEBUG = false;
            _harmony = new Harmony(HarmonyID);
            FileLog.Reset();
            _harmony.PatchAll();
        }

        protected override void OnGameStarted()
        {
            ModSettingsWindowManager.Current.Register<SettingsWindowPage>("DepotExtended", "Depot extended");
            if (!ModSettings<Settings>.Current.VehiclesRidesToDepot)
                SimpleManager<GoToDepotManager>.Current?.Disable();
            else if (SimpleManager<GoToDepotManager>.Current == null)
                SimpleManager<GoToDepotManager>.Initialize();

        }

        protected override void Deinitialize()
        {
            _harmony.UnpatchAll(HarmonyID);
            _harmony = null;
        }

        protected override void Read(StateBinaryReader reader)
        {
            if (SchemaVersion<DepotExtendedMod>.AtLeast(1))
            {
                bool loadDepotManager = true;
                
                if (SchemaVersion<DepotExtendedMod>.AtLeast(2))
                    loadDepotManager = reader.ReadBool();

                if (loadDepotManager)
                {
                    SimpleManager<GoToDepotManager>.Initialize();
                    SimpleManager<GoToDepotManager>.Current?.Read(reader);
                }

                SimpleLazyManager<RailDepotManager>.Current.Read(reader);
            }
        }

        protected override void Write(StateBinaryWriter writer)
        {
            writer.WriteBool(SimpleManager<GoToDepotManager>.Current != null); 
            SimpleManager<GoToDepotManager>.Current?.Write(writer);
            SimpleLazyManager<RailDepotManager>.Current.Write(writer);
        }
    }
}