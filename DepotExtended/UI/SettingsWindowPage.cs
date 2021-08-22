using ModSettingsUtils;
using VoxelTycoon;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Localization;

namespace DepotExtended.UI
{
    public class SettingsWindowPage : ModSettingsWindowPage
    {
        protected override void InitializeInternal(SettingsControl settingsControl)
        {
            Settings settings = Settings.Current;
            Locale locale = LazyManager<LocaleManager>.Current.Locale;
            settingsControl.AddToggle(/*locale.GetString("advanced_pathfinder_mod/highlight_train_path")*/ "Real vehicle go to depot", "Vehicles drives to the depot instead of \"stop and disappear\".\nThe setting is effectively only after the game is reloaded.", settings.VehiclesRidesToDepot, delegate ()
            {
                settings.VehiclesRidesToDepot = true;
            }, delegate ()
            {
                settings.VehiclesRidesToDepot = false;
            });

        }

    }
}