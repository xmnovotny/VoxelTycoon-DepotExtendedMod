using DepotExtended.GoToDepot;
using HarmonyLib;
using VoxelTycoon.Modding;
using VoxelTycoon.Tracks.Rails;
using XMNUtils;

namespace DepotExtended
{
    [HarmonyPatch]
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
    }
}