using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Game.UI.VehicleEditorWindowViews;
using VoxelTycoon.Tracks;

namespace DepotExtended.UI.VehicleEditorWindowViews
{
    public class DepotActionsView: ActionsViewBase
    {
        private ActionButton[] _buttons;

        private Text _selectedUnitsCountText;

        private DepotVehiclesWindow _depotVehiclesWindow;
        private VehicleEditorWindow _vehicleEditorWindow;

        public void Initialize(DepotVehiclesWindow depotVehiclesWindow, VehicleEditorWindow vehicleEditorWindow)
        {
            _depotVehiclesWindow = depotVehiclesWindow;
            _vehicleEditorWindow = vehicleEditorWindow;
            ActionButton[] buttons = GetComponentsInChildren<ActionButton>();
            string removeTextIcon = "";
            Font removeTextFont = null;
            foreach (ActionButton button in buttons)
            {
                if (button.transform.name == "Remove")
                {
                    button.OnInvalidate = null;
                    Text component = button.transform.Find<Text>("Icon");
                    removeTextIcon = component.text;
                    removeTextFont = component.font;
                }
                button.DestroyGameObject();
            }
            transform.Find<Text>("SelectionInfoRow/DeselectAllButton/Text").text = VoxelTycoon.S.Deselect.ToUpper();
            (transform.Find<Button>("SelectionInfoRow/DeselectAllButton").onClick = new Button.ButtonClickedEvent()).AddListener(DeselectAll);
            _selectedUnitsCountText = transform.Find<Text>("SelectionInfoRow/SelectedUnitsCountText");
            Transform actionsRow = transform.Find("ActionsRow");
            ActionButton moveButton = AddActionButton(actionsRow, "", MoveFromDepot, InvalidateMoveButton, S.UnitMoveToTrain, R.Fonts.Ketizoloto);
            ActionButton removeButton = AddActionButton(actionsRow, removeTextIcon, Remove, null, VoxelTycoon.S.Remove, removeTextFont);
            _buttons = new[] {moveButton, removeButton};
        }
        
        public void Invalidate()
        {
            List<VehicleRecipeInstance> source = _depotVehiclesWindow.Selection.ToList();
            int value = source.Sum((VehicleRecipeInstance x) => x.GetUnitsCount());
            double dollars = source.Sum((VehicleRecipeInstance x) => x.GetPrice(actual: true));
            string text = StringHelper.ToPluralString(value, VoxelTycoon.S.UnitSelected, VoxelTycoon.S.UnitsSelected);
            _selectedUnitsCountText.text = (text + " <color=#00000088>(" + UIFormat.Money.Format(dollars) + ")</color>").ToUpper();
            ActionButton[] buttons = _buttons;
            foreach (ActionButton actionButton in buttons)
            {
                actionButton.OnInvalidate.Invoke(actionButton);
            }
        }
        
        public void DeselectAll()
        {
            _depotVehiclesWindow.DeselectAll();
        }

        private void InvalidateMoveButton(ActionButton button)
        {
        }

        public void Remove(PointerEventData eventData)
        {
            ImmutableList<VehicleRecipeInstance> selection = _depotVehiclesWindow.Selection;
            for (int i = 0; i < selection.Count; i++)
            {
                _depotVehiclesWindow.SellFromDepot(selection[i]);
            }

            _depotVehiclesWindow.Invalidate();
            _vehicleEditorWindow.Invalidate();
        }

        private void MoveFromDepot(PointerEventData data)
        {
            //TODO: Test couplings
            ImmutableList<VehicleRecipeInstance> selection = _depotVehiclesWindow.Selection;
            int? index = null;
            if (_vehicleEditorWindow.Selection.Count == 1)
                index = _vehicleEditorWindow.Vehicle.Consist.IndexOf(_vehicleEditorWindow.Selection[0]);
            for (int i = selection.Count - 1; i >= 0; i--)
            {
                _depotVehiclesWindow.AddUnitFromDepot(selection[i], index);
            }
            _depotVehiclesWindow.Invalidate();
            _vehicleEditorWindow.Invalidate();
        } 
        
    }
}