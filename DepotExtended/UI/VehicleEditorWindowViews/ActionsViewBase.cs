using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Game.UI.VehicleEditorWindowViews;
using VoxelTycoon.UI.Controls;

namespace DepotExtended.UI.VehicleEditorWindowViews
{
    public class ActionsViewBase : MonoBehaviour
    {
        private static Transform _buttonTemplate;

        protected ActionButton AddActionButton(Transform parent, string text, Action<PointerEventData> onClick, UnityAction<ActionButton> onInvalidate, string toolTipText = null, Font font = null)
        {
            if (_buttonTemplate == null)
            {
                CreateButtonTemplate();
            }

            Transform transf = Instantiate(_buttonTemplate, parent);
            transf.GetComponent<ClickableDecorator>().OnClick = onClick;
            Text textIcon = transf.Find<Text>("Icon");
            textIcon.text = text;
            if (font != null)
                textIcon.font = font;
            ActionButton actButt = transf.GetComponent<ActionButton>();
            if (onInvalidate != null)
            {
                actButt.OnInvalidate = new ActionButtonOnInvalidateEvent();
                actButt.OnInvalidate.AddListener(onInvalidate);
            }

            actButt.TooltipTarget.Text = toolTipText;
            
            return actButt;
        }

        private void CreateButtonTemplate()
        {
            _buttonTemplate = Instantiate(R.Game.UI.VehicleEditorWindow.Content.transform.Find("Footer/Actions/ActionsRow/Remove"));
            Button button = _buttonTemplate.GetComponent<Button>();
            button.onClick = null;
            ActionButton actionButton = _buttonTemplate.GetComponent<ActionButton>();
            actionButton.OnInvalidate = null;
            ClickableDecorator decorator = _buttonTemplate.GetComponent<ClickableDecorator>();
            decorator.OnClick = null;
        }
    }
}