using DepotExtended.DepotVehicles;
using DepotExtended.GoToDepot;
using HarmonyLib;
using VoxelTycoon.Modding;
using VoxelTycoon.Serialization;
using VoxelTycoon.Tracks.Rails;
using XMNUtils;

namespace DepotExtended
{
    [HarmonyPatch]
    [SchemaVersion(1)]
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
            SimpleManager<GoToDepotManager>.Initialize();
        }

        protected override void OnGameStarted()
        {
            //SimpleManager<GoToDepotManager>.Initialize();
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
                SimpleManager<GoToDepotManager>.Current?.Read(reader);
                SimpleLazyManager<RailDepotManager>.Current.Read(reader);
            }
        }

        protected override void Write(StateBinaryWriter writer)
        {
            SimpleManager<GoToDepotManager>.Current?.Write(writer);
            SimpleLazyManager<RailDepotManager>.Current.Write(writer);
        }
    }
}