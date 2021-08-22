using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Tracks;
using VoxelTycoon.UI;

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
            InitializeThumbs(units);
        }

        public void Update()
        {
            
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
                float z = 0;// vehicleUnit.GetComponent<MeshFilter>().sharedMesh.bounds.center.z;
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
        
        private static void CreateTemplate()
        {
            DepotWindowVehicleListItem listItem = Instantiate(R.Game.UI.DepotWindow.DepotWindowVehicleListItem);
            Transform transform = listItem.transform;
            _template = listItem.gameObject.AddComponent<DepotWindowDepotVehiclesListItem>();
            DestroyImmediate(listItem);
            transform.Find("OnOffToggle").DestroyGameObject(true);
            transform.Find("Route").DestroyGameObject(true);
            transform.Find("SetupCog").DestroyGameObject(true);
            transform.Find("SellButton").DestroyGameObject(true);
            transform.Find("Storages").DestroyGameObject(true);
            transform.Find<Text>("Text").text = "Stored vehicle units"; //TODO: translate
        }
        
    }
}