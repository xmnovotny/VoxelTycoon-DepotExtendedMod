using System.Collections.Generic;
using System.Linq;
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
            foreach (ActionButton button in buttons)
            {
                button.DestroyGameObject();
            }
            transform.Find<Text>("SelectionInfoRow/DeselectAllButton/Text").text = S.Deselect.ToUpper();
            _selectedUnitsCountText = transform.Find<Text>("SelectionInfoRow/SelectedUnitsCountText");
            Transform actionsRow = transform.Find("ActionsRow");
            ActionButton moveButton = AddActionButton(actionsRow, "", MoveFromDepot, InvalidateMoveButton, "Move selected unit(s) to the train.", R.Fonts.Ketizoloto); //TODO: translate
            _buttons = new[] {moveButton};
        }
        
        public void Invalidate()
        {
            List<VehicleRecipeInstance> source = _depotVehiclesWindow.Selection.ToList();
            int value = source.Sum((VehicleRecipeInstance x) => x.GetUnitsCount());
            double dollars = source.Sum((VehicleRecipeInstance x) => x.GetPrice(actual: true));
            string text = StringHelper.ToPluralString(value, S.UnitSelected, S.UnitsSelected);
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