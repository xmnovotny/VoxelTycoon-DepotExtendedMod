using System;
using System.Collections.Generic;
using DepotExtended.DepotVehicles;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Audio;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Rails;
using VoxelTycoon.UI;
using VoxelTycoon.UI.Windows;
using XMNUtils;
using Object = UnityEngine.Object;

namespace DepotExtended.UI
{
    [HarmonyPatch]
    public class DepotWindowExtender: SimpleLazyManager<DepotWindowExtender>
    {
        private readonly Dictionary<VehicleDepot, DepotWindow> _windows = new ();
        private Button _putTrainToDepotButtonTemplate; 

        public void OnDepotVehiclesChanged(RailDepot depot)
        {
            if (_windows.TryGetValue(depot, out DepotWindow window))
            {
                window.InvalidateItems();
            }
        }

        private void UpdateItems(DepotWindowContent depotWindowContent, RailDepot depot)
        {
            foreach (DepotWindowVehicleListItem listItem in depotWindowContent.ItemContainer.GetComponentsInChildren<DepotWindowVehicleListItem>())
            {
                if (ReferenceEquals(_putTrainToDepotButtonTemplate, null))
                {
                    CreatePutTrainToDepotButtonTemplate();
                }

                Button button = Object.Instantiate(_putTrainToDepotButtonTemplate, listItem.transform);
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(() => PutTrainToDepotButtonClick(listItem.Vehicle, depotWindowContent));
            }
        }

        private void PutTrainToDepotButtonClick(Vehicle vehicle, DepotWindowContent depotWindowContent)
        {
            if (vehicle is not Train train)
                throw new ArgumentException("Vehicle is not a train");
            
            Dialog.ShowConfirmation(string.Format(S.MoveAllVehiclesToStorageConfirm, train.Name), delegate
            {
                SimpleLazyManager<RailDepotManager>.Current.PutTrainToStoredVehicles(train);
                Manager<SoundManager>.Current.PlayOnce(new Sound
                {
                    Clip = R.Audio.Raw.Click
                });
                depotWindowContent.InvalidateItems();
            });
        }

        private void CreatePutTrainToDepotButtonTemplate()
        {
             _putTrainToDepotButtonTemplate = Object.Instantiate(R.Game.UI.DepotWindow.DepotWindowVehicleListItem.transform.Find<Button>("SetupCog"));
             _putTrainToDepotButtonTemplate.onClick = null;
             Text text = _putTrainToDepotButtonTemplate.gameObject.GetComponent<Text>();
             text.text = "";
             text.font = R.Fonts.Ketizoloto;
             RectTransform transform = (RectTransform) _putTrainToDepotButtonTemplate.transform;
             var pos = transform.anchoredPosition;
             pos.x -= 30f;
             transform.anchoredPosition = pos;
        }
        
        [UsedImplicitly]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DepotWindowContent), "InvalidateItems")]
        // ReSharper disable once InconsistentNaming
        private static void DepotWindowContent_InvalidateItems_pof(DepotWindowContent __instance, DepotWindow ____window)
        {
            if (____window.Depot is not RailDepot railDepot) 
                return;
            
            Current.UpdateItems(__instance, railDepot);
            
            IReadOnlyList<VehicleUnit> units = SimpleLazyManager<RailDepotManager>.Current.GetDepotVehicleUnits(railDepot);
            if (units == null || units.Count == 0)
                return;

            DepotWindowDepotVehiclesListItem listItem =
                DepotWindowDepotVehiclesListItem.InstantiateItem(__instance.ItemContainer.transform);
            listItem.Initialize(____window, __instance, units);
            __instance.Header.SetActive(true);
            __instance.Placeholder.SetActive(false);
            __instance.ScrollRect.SetActive(true);
        }

        [UsedImplicitly]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DepotWindow), "Initialize")]
        // ReSharper disable once InconsistentNaming
        private static void DepotWindow_Initialize_pof(DepotWindow __instance)
        {
            if (__instance.Depot is not RailDepot railDepot) 
                return;
            Current._windows.Add(railDepot, __instance);
        }

        [UsedImplicitly]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DepotWindow), "OnClose")]
        // ReSharper disable once InconsistentNaming
        private static void DepotWindow_OnClose_pof(DepotWindow __instance)
        {
            Current._windows.Remove(__instance.Depot);
        }
    }
}