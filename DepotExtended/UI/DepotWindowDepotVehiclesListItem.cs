using System.Collections.Generic;
using DepotExtended.DepotVehicles;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Audio;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Rails;
using VoxelTycoon.UI;
using VoxelTycoon.UI.Windows;
using XMNUtils;

namespace DepotExtended.UI
{
    public class DepotWindowDepotVehiclesListItem: MonoBehaviour
    {
        private static DepotWindowDepotVehiclesListItem _template;
        private IReadOnlyList<VehicleUnit> _units;
        private DepotWindow _window;
        private RectTransform _unitThumbsContainer;
        private DepotWindowContent _content;

        public void Initialize(DepotWindow window, DepotWindowContent content, IReadOnlyList<VehicleUnit> units)
        {
            _units = units;
            _window = window;
            _content = content;
            _unitThumbsContainer = transform.Find<RectTransform>("Units");
            Button sellButton = transform.Find<Button>("SellButton");
            sellButton.onClick = new Button.ButtonClickedEvent();
            sellButton.onClick.AddListener(Sell);
            InitializeThumbs(units);
        }

        private void InitializeThumbs(IReadOnlyList<VehicleUnit> units)
        {
            _unitThumbsContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _window.ThumbContainerHeight);
            float thumbScale = _window.ThumbScale;
            float currOffset = 0f; 
            foreach (VehicleUnit vehicleUnit in units)
            {
                if (currOffset * thumbScale > (float)_window.ItemWidth)
                {
                    break;
                }
                Image image = Object.Instantiate(R.Game.UI.DepotWindow.DepotWindowVehicleListItemUnitThumb, _unitThumbsContainer);
                Sprite vehicleUnitIcon = LazyManager<IconRenderer>.Current.GetVehicleUnitIcon(vehicleUnit.SharedData.AssetId, vehicleUnit.Flipped, thumbScale);
                float length = vehicleUnit.SharedData.Length;
                float z = 0;
                float x = (currOffset + length / 2f - z) * thumbScale;
                image.rectTransform.anchoredPosition = new Vector2(x, 0f);
                image.GetComponent<Image>().sprite = vehicleUnitIcon;
                image.SetNativeSize();
                currOffset += length;
            }
        }
        
        public static DepotWindowDepotVehiclesListItem InstantiateItem(Transform parent)
        {
            if (_template == null)
                CreateTemplate();
            return Instantiate(_template, parent);
        }

        public void Sell()
        {
            Dialog.ShowConfirmation(string.Format(S.SellForConfirmation, "all stored units", "<b><color=#89b454>" + UIFormat.Money.Format(SimpleLazyManager<RailDepotManager>.Current.GetStoredUnitsPrice((RailDepot)_window.Depot)) + "</color></b>"), delegate //TODO: translate
            {
                SimpleLazyManager<RailDepotManager>.Current.SellAllVehicles((RailDepot)_window.Depot);
                Manager<SoundManager>.Current.PlayOnce(new Sound
                {
                    Clip = R.Audio.Raw.Coins
                });
                _window.InvalidateItems();
            });
        }
        
        private static void CreateTemplate()
        {
            DepotWindowVehicleListItem listItem = Instantiate(R.Game.UI.DepotWindow.DepotWindowVehicleListItem);
            Transform transform = listItem.transform;
            _template = listItem.gameObject.AddComponent<DepotWindowDepotVehiclesListItem>();
            DestroyImmediate(listItem);
            transform.Find("OnOffToggle").DestroyGameObject(true);
            transform.Find("Route").DestroyGameObject(true);
            transform.Find("SetupCog").DestroyGameObject(true);
            Button sellButton = transform.Find<Button>("SellButton");
            sellButton.onClick = null;
            transform.Find("Storages").DestroyGameObject(true);
            transform.Find<Text>("Text").text = "Stored vehicle units"; //TODO: translate
        }
        
    }
}