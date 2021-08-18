using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Unity.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Game.UI.VehicleEditorWindowViews;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Rails;
using VoxelTycoon.UI;
using VoxelTycoon.UI.Controls;

namespace DepotExtended.UI.VehicleEditorWindowViews
{
    public class ActionsViewAddition: MonoBehaviour
    {
        private VehicleEditorWindow _editorWindow;
        private List<VehicleUnitCheckboxGroup> _checkboxGroups;
        private Vehicle _vehicle;
        private ActionsView _actionsView;
        private ActionButton _moveLeftButton;
        private ActionButton _moveRightButton;
        private static Transform _buttonTemplate = null;

        private void Initialize(ActionsView actionsView, VehicleEditorWindow vehicleEditorWindow, List<VehicleUnitCheckboxGroup> checkboxGroups)
        {
            _editorWindow = vehicleEditorWindow;
            _vehicle = vehicleEditorWindow.Vehicle;
            _actionsView = actionsView;
            _checkboxGroups = checkboxGroups;
            Transform actionsRow = _actionsView.transform.Find("ActionsRow");
            _moveLeftButton = AddActionButton(actionsRow, "<", MoveLeft, InvalidataMoveLeft,"Move selected vehicle(s) to the left.\nHold <b>Shift</b> to move vehicle to the front."); //TODO: translate
            _moveRightButton = AddActionButton(actionsRow, ">", MoveRight, InvalidateMoveRight, "Move selected vehicle(s) to the right.\nHold <b>Shift</b> to move vehicle to the rear."); //TODO: translate
        }

        private ActionButton AddActionButton(Transform parent, string text, UnityAction onClick, UnityAction<ActionButton> onInvalidate, string toolTipText = null)
        {
            if (_buttonTemplate == null)
            {
                CreateButtonTemplate();
            }

            Transform transf = Instantiate(_buttonTemplate, parent);
            Button.ButtonClickedEvent onClEvent = new Button.ButtonClickedEvent();
            onClEvent.AddListener(onClick);
            transf.GetComponent<Button>().onClick = onClEvent;
            transf.Find<Text>("Icon").text = text;
            ActionButton actButt = transf.GetComponent<ActionButton>();
            actButt.OnInvalidate = new ActionButtonOnInvalidateEvent();
            actButt.OnInvalidate.AddListener(onInvalidate);
/*            if (toolTipText != null)
                actButt.TooltipTarget = Tooltip.For(actButt, toolTipText);*/
            actButt.TooltipTarget.Text = toolTipText;
            
            return actButt;
        }

        private void InvalidataMoveLeft(ActionButton button)
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

        private void MoveLeft()
        {
            MoveVehicles(-1, InputHelper.Shift);
        }

        private void MoveRight()
        {
            MoveVehicles(1, InputHelper.Shift);
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

        private void CreateButtonTemplate()
        {
            _buttonTemplate = Instantiate<Transform>(R.Game.UI.VehicleEditorWindow.Content.transform.Find("Footer/Actions/ActionsRow/Remove"));
            Button button = _buttonTemplate.GetComponent<Button>();
            button.onClick = null;
            ActionButton actionButton = _buttonTemplate.GetComponent<ActionButton>();
            actionButton.OnInvalidate = null;
/*            actionButton.TooltipTarget.enabled = false;
            DestroyImmediate(actionButton.TooltipTarget);
            actionButton.TooltipTarget = null;*/
            ClickableDecorator decorator = _buttonTemplate.GetComponent<ClickableDecorator>();
            decorator.OnClick = null;
            FileLog.Log(XMNUtils.GameObjectDumper.DumpGameObject(_buttonTemplate.gameObject));
        }
        
        public static void TryInsertInstance(ActionsView actionsView, VehicleEditorWindow vehicleEditorWindow, List<VehicleUnitCheckboxGroup> checkboxGroups)
        {
            if (vehicleEditorWindow.Vehicle is Train)
            {
                ActionsViewAddition actAdd = actionsView.gameObject.AddComponent<ActionsViewAddition>();
                actAdd.Initialize(actionsView, vehicleEditorWindow, checkboxGroups);
            }
        }
    }
}