using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using VoxelTycoon;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Game.UI.VehicleEditorWindowViews;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Rails;

namespace DepotExtended.UI.VehicleEditorWindowViews
{
    public class ActionsViewAddition: ActionsViewBase
    {
        private VehicleEditorWindow _editorWindow;
        private List<VehicleUnitCheckboxGroup> _checkboxGroups;
        private Vehicle _vehicle;
        private ActionsView _actionsView;
        private DepotVehiclesWindow _depotVehiclesWindow;
        public bool Changed { get; private set; }

        public void MovedFromDepot(VehicleRecipeInstance instance)
        {
            Changed = true;
        }
        
        private void Initialize(ActionsView actionsView, VehicleEditorWindow vehicleEditorWindow, List<VehicleUnitCheckboxGroup> checkboxGroups, DepotVehiclesWindow depotVehiclesWindow)
        {
            _editorWindow = vehicleEditorWindow;
            _vehicle = vehicleEditorWindow.Vehicle;
            _actionsView = actionsView;
            _checkboxGroups = checkboxGroups;
            _depotVehiclesWindow = depotVehiclesWindow;
            Transform actionsRow = _actionsView.transform.Find("ActionsRow");
            AddActionButton(actionsRow, "<", MoveLeft, InvalidateMoveLeft,S.UnitMoveLeftTooltip);
            AddActionButton(actionsRow, ">", MoveRight, InvalidateMoveRight, S.UnitMoveRightTooltip);
            AddActionButton(actionsRow, "", MoveToDepot, InvalidateMoveToDepot, S.UnitMoveToDepotTooltip, R.Fonts.Ketizoloto);
        }

        private void InvalidateMoveLeft(ActionButton button)
        {
            ImmutableList<VehicleRecipeInstance> selection = _editorWindow.Selection;
            if (selection.Count == 0)
            {
                button.Toggle(false);
                return;
            }

            for (int i = selection.Count - 1; i >= 0; i--)
            {
                if (_vehicle.Consist.IndexOf(selection[i]) == 0)
                {
                    button.Toggle(false);
                    return;
                }
            }

            button.Toggle(true);
        }

        private void InvalidateMoveRight(ActionButton button)
        {
            ImmutableList<VehicleRecipeInstance> selection = _editorWindow.Selection;
            if (selection.Count == 0)
            {
                button.Toggle(false);
                return;
            }

            int itemsCount = _vehicle.Consist.Items.Count;
            for (int i = selection.Count - 1; i >= 0; i--)
            {
                if (_vehicle.Consist.IndexOf(selection[i]) >= itemsCount-1)
                {
                    button.Toggle(false);
                    return;
                }
            }
            
            button.Toggle(true);
        }

        private void InvalidateMoveToDepot(ActionButton button)
        {
            ImmutableList<VehicleRecipeInstance> selection = _editorWindow.Selection;
            if (selection.Count == 0)
            {
                button.Toggle(false);
                button.TooltipTarget.Text = S.SelectUnitsFirst;
                return;
            }

            if (selection.Count == _editorWindow.Vehicle.Consist.Items.Count)
            {
                button.Toggle(false);
                button.TooltipTarget.Text = S.OneUnitMustRemain;
                return;
            }

            button.TooltipTarget.Text = S.UnitMoveToDepotTooltip;
            button.Toggle(true);
        }

        private void MoveLeft(PointerEventData data)
        {
            MoveVehicles(-1, InputHelper.Shift);
        }

        private void MoveRight(PointerEventData data)
        {
            MoveVehicles(1, InputHelper.Shift);
        }

        private void MoveToDepot(PointerEventData data)
        {
            ImmutableList<VehicleRecipeInstance> selection = _editorWindow.Selection;
            for (int i = selection.Count - 1; i >= 0; i--)
            {
                _depotVehiclesWindow.AddUnitToDepot(selection[i], _vehicle.Consist);
                Changed = true;
            }
            _editorWindow.Invalidate();
            _depotVehiclesWindow.Invalidate();
        }
        
        private void MoveVehicles(int difference, bool toTheEnd)
        {
            ImmutableList<VehicleRecipeInstance> selection = _editorWindow.Selection;
            if (selection.Count == 0)
                return;

            ImmutableList<VehicleRecipeInstance> oldItems = _vehicle.Consist.Items;
            int oldItemsCount = oldItems.Count;
            HashSet<int> selectionIdxs = new();
            Dictionary<int, int> reverseTransform = new();
            int selectionCount = selection.Count;

            if (toTheEnd)
            {
                int min = int.MaxValue;
                int max = 0;
                for (int i = 0; i < selectionCount; i++)
                {
                    int idx = _vehicle.Consist.IndexOf(selection[i]);
                    if (min > idx)
                        min = idx;
                    if (max < idx)
                        max = idx;
                }

                if (difference > 0)
                    difference = oldItemsCount - max - 1;
                else
                    difference = 0-min;
            }
            
            for (int i = 0; i < selectionCount; i++)
            {
                int idx = _vehicle.Consist.IndexOf(selection[i]);
                if (idx + difference >= oldItemsCount || idx + difference < 0)
                    return;
                selectionIdxs.Add(idx);
                reverseTransform.Add(idx + difference, idx);
            }

            List<VehicleRecipeInstance> newItems = new();
            int oldIdx = 0;
            for (int newIdx = 0; newIdx < oldItemsCount; newIdx++)
            {
                int idxToAdd;
                if (reverseTransform.TryGetValue(newIdx, out int oldMovedIdx))
                    idxToAdd = oldMovedIdx;
                else
                {
                    while (selectionIdxs.Contains(oldIdx))
                        oldIdx++;
                    idxToAdd = oldIdx++;
                }

                newItems.Add(oldItems[idxToAdd]);
            }

            if (!ValidateCouplings(newItems))
                return;
            
            List<VehicleRecipeInstance> consistsRecipes = (List<VehicleRecipeInstance>) AccessTools.Field(typeof(VehicleConsist), "_recipeInstances").GetValue(_vehicle.Consist);
            consistsRecipes.Clear();
            consistsRecipes.AddRange(newItems);
            _editorWindow.Invalidate();
            foreach (int newIdx in reverseTransform.Keys)
            {
                _checkboxGroups[newIdx].Checked = true;
            }

            Changed = true;
        }

        private bool ValidateCouplings(List<VehicleRecipeInstance> recipes)
        {
            for (int i = recipes.Count - 1; i >= 1; i--)
            {
                if (recipes[i].Coupling1 != recipes[i - 1].Coupling2)
                    return false;
            }

            return true;
        }

        public static void TryInsertInstance(ActionsView actionsView, VehicleEditorWindow vehicleEditorWindow, List<VehicleUnitCheckboxGroup> checkboxGroups, DepotVehiclesWindow depotVehiclesWindow)
        {
            if (vehicleEditorWindow.Vehicle is Train)
            {
                ActionsViewAddition actAdd = actionsView.gameObject.AddComponent<ActionsViewAddition>();
                actAdd.Initialize(actionsView, vehicleEditorWindow, checkboxGroups, depotVehiclesWindow);
            }
        }
    }
}