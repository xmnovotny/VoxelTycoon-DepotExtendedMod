using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DepotExtended.DepotVehicles;
using DepotExtended.UI.VehicleEditorWindowViews;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon;
using VoxelTycoon.Audio;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Game.UI.VehicleEditorWindowViews;
using VoxelTycoon.Game.UI.VehicleUnitPickerWindowViews;
using VoxelTycoon.Tracks;
using VoxelTycoon.Tracks.Rails;
using VoxelTycoon.UI;
using VoxelTycoon.UI.Controls;
using VoxelTycoon.UI.Windows;
using XMNUtils;

namespace DepotExtended.UI
{
    public class DepotVehiclesWindow: Window
    {
		private const int MinWidth = 400;

		private readonly List<VehicleUnitCheckboxGroup> _checkboxGroups = new List<VehicleUnitCheckboxGroup>();

		private readonly List<VehicleRecipeInstance> _selection = new List<VehicleRecipeInstance>();

		private readonly List<VehicleUnitView> _vehicleUnitViews = new List<VehicleUnitView>();

		private DepotActionsView _actions;
		private ActionsViewAddition _editorActionsAddition;

		private LayoutElement _body;

		private RawImage _image;

		private Action _onClosed = null;

		private Transform _root;

		private Button _scrollControlsLeft;

		private Button _scrollControlsRight;

		private ScrollRect _scrollView;

		private HorizontalLayoutGroup _scrollViewContent;

		private Text _titleText;

		private Transform _unitsRoot;

		private VehicleEditorRenderer _vehicleRenderer;
		private bool _sectionsChanged; 

		public ImmutableList<VehicleRecipeInstance> Selection => new ImmutableList<VehicleRecipeInstance>(_selection);

		public VehicleConsist Consist { get; private set; }
		public bool Changed => _sectionsChanged || _addedInstances.Count != 0 || _removedInstances.Count != 0;

		private readonly List<VehicleUnit> _units = new();
		private VehicleEditorWindow _vehicleEditorWindow;
		private readonly HashSet<VehicleRecipeInstance> _addedInstances = new();
		private readonly HashSet<VehicleRecipeInstance> _removedInstances = new();
		private double _originalVehiclesPrice;

		public RailDepot Depot { get; set; }

		private ActionsViewAddition EditorActionsAddition
		{
			get {
				if (_editorActionsAddition == null)
				{
					_editorActionsAddition = _vehicleEditorWindow.transform.Find<ActionsViewAddition>("Root/Content(Clone)/Footer/Actions");					
				}

				return _editorActionsAddition;
			}
		}

		public IReadOnlyCollection<VehicleRecipeInstance> AddedInstances => _addedInstances;
		public IReadOnlyCollection<VehicleRecipeInstance> RemovedInstances => _removedInstances;

		public static DepotVehiclesWindow ShowFor(VehicleEditorWindow vehicleEditorWindow, VehicleConsist consist, RailDepot depot, Vector2Int rendererDimensions)
		{
			DepotVehiclesWindow depotVehiclesWindow = UIManager.Current.CreateFrame<DepotVehiclesWindow>(FrameAnchoring.Center, new Vector2(0, 280));
			depotVehiclesWindow.Initialize(vehicleEditorWindow, consist, depot, rendererDimensions);
			depotVehiclesWindow.Show();
			return depotVehiclesWindow;
		}
		
		public void AddUnitToDepot(VehicleRecipeInstance instance, VehicleConsist sourceConsist)
		{
			int? index = FindIndexOfVehicleRecipe(instance.Original);
			VehicleConsistsHelper.MoveBetween(sourceConsist, Consist, instance, index);
			if (instance.Flipped)
				VehicleConsistsHelper.FlipRecipeInstance(instance);
			if (!_removedInstances.Remove(instance))
				_addedInstances.Add(instance);
		}

		public void AddUnitFromDepot(VehicleRecipeInstance instance, int? index = null)
		{
			if (index == null && instance.Original.Power > 0f)
				index = 0;
			VehicleConsistsHelper.MoveBetween(Consist, _vehicleEditorWindow.Vehicle.Consist, instance, index);
			EditorActionsAddition.MovedFromDepot(instance);
			if (!_addedInstances.Remove(instance))
				_removedInstances.Add(instance);
		}

		public void DeselectAll()
		{
			foreach (VehicleUnitView vehicleUnitView in _vehicleUnitViews)
			{
				vehicleUnitView.CheckboxGroup.Checked = false;
			}
		}

		public void Invalidate()
		{
			_unitsRoot.Clear(immediate: true);
			while (_scrollView.content.childCount > 1)
			{
				DestroyImmediate(_scrollView.content.GetChild(0).gameObject);
			}
			InvalidateWindow();
		}

		public void OnDestroy()
		{
			if ((bool)_root)
			{
				Destroy(_root.gameObject);
			}
			((RenderTexture)_image.texture).Release();
		}

		public override void TryClose()
		{
			Close();
			_onClosed?.Invoke();
		}

		internal double GetBuyPrice()
		{
			return _vehicleEditorWindow.Vehicle.GetPrice(true) + Consist.GetPrice(true) - _originalVehiclesPrice;
		}

		protected override void InitializeFrame()
		{
			base.InitializeFrame();
			Transform transform1 = Instantiate(R.Game.UI.VehicleEditorWindow.Content, Root).transform;
			_body = transform1.Find<LayoutElement>("Body");
			_image = transform1.Find<RawImage>("Body/RawImage");
			_scrollView = transform1.transform.Find<ScrollRect>("ScrollView");
			_scrollViewContent = _scrollView.content.GetComponent<HorizontalLayoutGroup>();
			_scrollControlsLeft = _scrollView.transform.Find<Button>("ScrollControls/Left");
			_scrollControlsRight = _scrollView.transform.Find<Button>("ScrollControls/Right");
			_scrollControlsLeft.onClick.AddListener(delegate
			{
				_scrollView.horizontalNormalizedPosition -= 0.1f;
			});
			_scrollControlsRight.onClick.AddListener(delegate
			{
				_scrollView.horizontalNormalizedPosition += 0.1f;
			});
			Button button = _scrollViewContent.transform.Find<Button>("AddButton/Button");
			button.DestroyGameObject(true);
			transform1.Find<Button>("Body/WindowCloseButton").DestroyGameObject(true);
			//transform.Find<Button>("Body/WindowCloseButton").onClick.AddListener(TryClose);
			Transform transform2 = transform1.Find("Footer");
			transform1.Find<Panel>("Footer").EnabledCorners = PanelCorners.Bottom;
			transform2.Find<SummaryView>("Summary").DestroyGameObject(true);
			
			ActionsView actions = transform2.Find<ActionsView>("Actions");
			actions.SetActive(active: false);
			_actions = actions.gameObject.AddComponent<DepotActionsView>();
			DestroyImmediate(actions);
			
			Button button2 = transform1.Find<Button>("BuyButton");
			button2.DestroyGameObject(true);
			
			RectTransform transform3 = (RectTransform) _scrollView.transform;
			Vector2 pos = transform3.offsetMin;
			pos.y -= 45f;
			transform3.offsetMin = pos;
			pos = transform3.offsetMax;
			pos.y -= 0f;
			transform3.offsetMax = pos;
			
			_titleText = transform1.Find<Text>("Body/Title");
			_titleText.text = "Vehicle units in the depot"; //TODO: translate
			InitializeVehicleRenderer();
		}

		protected override void OnClose()
		{
			base.OnClose();
			Cleanup();
		}

		private void Cleanup()
		{
		}

		private void Initialize(VehicleEditorWindow vehicleEditorWindow, VehicleConsist consist, RailDepot depot, Vector2Int rendererDimensions)
		{
			Consist = consist;
			Depot = depot;
			_titleText.raycastTarget = true;
			_vehicleEditorWindow = vehicleEditorWindow;
			vehicleEditorWindow.Closed += TryClose;
			if (vehicleEditorWindow is EditVehicleWindow editorWindow)
			{
				SubscribeToDepotVehiclesConsistsChanges();
			} else if (vehicleEditorWindow is BuyVehicleWindow buyVehicleWindow)
			{
				_originalVehiclesPrice = consist.GetPrice(true);
			}
			
			InitializeVehicleRendererDimensions(rendererDimensions);
			_actions.Initialize(this, _vehicleEditorWindow);
			Show();
			InvalidateWindow();
		}

		private void SubscribeToDepotVehiclesConsistsChanges()
		{
			for (int i = 0; i < Consist.Items.Count; i++)
			{
				VehicleRecipeInstance vehicleRecipeInstance = Consist.Items[i];
				for (int j = 0; j < vehicleRecipeInstance.Sections.Count; j++)
				{
					SubscribeToSectionChanges(vehicleRecipeInstance.Sections[j]);
				}
				vehicleRecipeInstance.OnSectionAdded = (Action<VehicleRecipeSectionInstance>)Delegate.Combine(vehicleRecipeInstance.OnSectionAdded, new Action<VehicleRecipeSectionInstance>(SubscribeToSectionChanges));
			}
		}

		private void SubscribeToSectionChanges(VehicleRecipeSectionInstance section)
		{
			section.OnUnitAdded = (Action<VehicleUnit>)Delegate.Combine(section.OnUnitAdded, new Action<VehicleUnit>(OnEditVehicleWindowUnitAdded));
			section.OnUnitRemoved = (Action<VehicleUnit>)Delegate.Combine(section.OnUnitRemoved, new Action<VehicleUnit>(OnEditVehicleWindowUnitRemoved));
		}

		private void OnEditVehicleWindowUnitAdded(VehicleUnit unit)
		{
			_sectionsChanged = true;
			AccessTools.Method(typeof(EditVehicleWindow), "OnUnitAdded").Invoke((EditVehicleWindow)_vehicleEditorWindow, new object []{unit});
		}

		private void OnEditVehicleWindowUnitRemoved(VehicleUnit unit)
		{
			_sectionsChanged = true;
			AccessTools.Method(typeof(EditVehicleWindow), "OnUnitRemoved").Invoke((EditVehicleWindow)_vehicleEditorWindow, new object []{unit});
		}

		protected override void OnUpdate()
		{
			float num = _vehicleRenderer.Camera.PixelsToWorld(_scrollViewContent.padding.left);
			float num2 = _vehicleRenderer.Camera.PixelsToWorld(_scrollView.content.localPosition.x);
			_vehicleRenderer.transform.localPosition = new Vector3(_vehicleRenderer.Camera.GetHorizontalExtent() - num2 - num, _vehicleRenderer.Camera.orthographicSize, -10f);
			bool flag = _scrollView.viewport.rect.width < _scrollView.content.rect.width;
			_scrollControlsLeft.transform.SetActive(flag && _scrollView.horizontalNormalizedPosition > 0f);
			_scrollControlsRight.transform.SetActive(flag && _scrollView.horizontalNormalizedPosition < 1f);
		}

		private VehicleUnitCheckboxGroup AddCheckboxGroup(float pixelOffset, VehicleRecipeInstance recipeInstance)
		{
			VehicleUnitCheckboxGroup vehicleUnitCheckboxGroup = Instantiate(R.Game.UI.VehicleEditorWindow.VehicleUnitCheckboxGroup, _scrollView.content);
			vehicleUnitCheckboxGroup.transform.SetAsSibling(-2);
			vehicleUnitCheckboxGroup.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, pixelOffset + (float)_scrollViewContent.padding.left, 0f);
			vehicleUnitCheckboxGroup.RecipeInstance = recipeInstance;
			return vehicleUnitCheckboxGroup;
		}

		private void ImitateDeselectionAnimation()
		{
			List<VehicleRecipeInstance> list = _selection.ToList();
			_selection.Clear();
			foreach (VehicleRecipeInstance recipeInstance in list)
			{
				VehicleUnitCheckboxGroup vehicleUnitCheckboxGroup = _checkboxGroups.FirstOrDefault((VehicleUnitCheckboxGroup x) => x.RecipeInstance == recipeInstance);
				if (!(vehicleUnitCheckboxGroup == null))
				{
					Action<bool> onCheckedValueChanged = vehicleUnitCheckboxGroup.OnCheckedValueChanged;
					vehicleUnitCheckboxGroup.OnCheckedValueChanged = null;
					vehicleUnitCheckboxGroup.SetInstantStateTransition(enabled: true);
					vehicleUnitCheckboxGroup.Checked = true;
					vehicleUnitCheckboxGroup.SetInstantStateTransition(enabled: false);
					vehicleUnitCheckboxGroup.Checked = false;
					vehicleUnitCheckboxGroup.OnCheckedValueChanged = onCheckedValueChanged;
				}
			}
			_actions.SetActive(active: false);
		}

		private void InitializeVehicleRenderer()
		{
			Transform transform = Manager<LightingManager>.Current.Light.transform;
			_root = new GameObject("VehicleEditorWindowRendererRoot").transform;
			_root.localPosition = new Vector3(0f, 3000f, 0f);
			_root.eulerAngles = new Vector3(0f, transform.eulerAngles.y, 0f);
			_unitsRoot = new GameObject("Units").transform;
			_unitsRoot.SetParent(_root, worldPositionStays: false);
			_vehicleRenderer = new GameObject("VehicleEditorWindowRendererCamera").AddComponent<VehicleEditorRenderer>();
			_vehicleRenderer.transform.SetParent(_root, worldPositionStays: false);
		}

		private void InitializeVehicleRendererDimensions(Vector2Int rendererDimensions)
		{
			rendererDimensions.x = Mathf.Clamp(rendererDimensions.x, 400, Mathf.Max(400, Screen.width - 200));
			_body.preferredWidth = rendererDimensions.x;
			RenderTexture renderTexture = new RenderTexture(rendererDimensions.x, rendererDimensions.y, 24);
			_image.rectTransform.sizeDelta = new Vector2(renderTexture.width, renderTexture.height);
			_image.texture = renderTexture;
			_vehicleRenderer.TargetTexture = renderTexture;
		}

		private void FillUnits()
		{
			_units.Clear();
			Consist.FillAllUnits(_units);
		}

		private int? FindIndexOfVehicleRecipe(VehicleRecipe recipe)
		{
			ImmutableList<VehicleRecipeInstance> items = Consist.Items;
			for (int i = items.Count - 1; i >= 0; i--)
			{
				if (items[i].Original == recipe)
					return i;
			}

			return null;
		}
		
		private void InvalidateActionsView(bool forceInstantStateTransition = false)
		{
			if (_selection.Count > 0)
			{
				foreach (VehicleUnitView vehicleUnitView in _vehicleUnitViews)
				{
					vehicleUnitView.CheckboxGroup.SetInstantStateTransition(forceInstantStateTransition);
					vehicleUnitView.CheckboxGroup.AlwaysVisible = true;
					vehicleUnitView.CheckboxGroup.SetInstantStateTransition(enabled: false);
				}
				_actions.SetActive(active: true);
				_actions.Invalidate();
				return;
			}
			foreach (VehicleUnitView vehicleUnitView2 in _vehicleUnitViews)
			{
				vehicleUnitView2.CheckboxGroup.AlwaysVisible = false;
			}
			_actions.SetActive(active: false);
		}

		private void InvalidateWindow()
		{
			FillUnits();
			SpawnVehicleUnits();
			SpawnVehicleUnitViews();
//			_summary.Invalidate();
		}

		private void OnCheckboxGroupValueChanged(bool value, VehicleRecipeInstance recipeInstance)
		{
			if (value)
			{
				_selection.Add(recipeInstance);
			}
			else
			{
				_selection.Remove(recipeInstance);
			}
			InvalidateActionsView();
		}

		private IEnumerator ScrollToEndAnimated()
		{
			yield return null;
			_scrollView.horizontalNormalizedPosition = 1f;
		}

		private void SpawnVehicleUnits()
		{
			float num = 0f;
			for (int i = 0; i < _units.Count; i++)
			{
				VehicleUnit vehicleUnit = _units[i];
				VehicleUnit vehicleUnit2 = Instantiate(Manager<AssetLibrary>.Current.Get<VehicleUnit>(vehicleUnit.SharedData.AssetId), _unitsRoot);
				vehicleUnit2.transform.SetLayerRecursively(Layer.PreviewRenderer);
				vehicleUnit2.transform.localPosition = new Vector3(num + vehicleUnit2.SharedData.Length / 2f, 0f, 0f);
				vehicleUnit2.transform.localEulerAngles = new Vector3(0f, -90 + (vehicleUnit.Flipped ? 180 : 0), 0f);
				Renderer[] componentsInChildren = vehicleUnit2.GetComponentsInChildren<Renderer>();
				foreach (Renderer renderer in componentsInChildren)
				{
					renderer.sharedMaterial = LazyManager<TintMaterialCache>.Current.Get(renderer.sharedMaterial, null, ignoreWhiteMode: true);
				}
				num += vehicleUnit2.SharedData.Length;
			}
		}

		private void SpawnVehicleUnitViews()
		{
			_vehicleUnitViews.Clear();
			_checkboxGroups.Clear();
			
			float runningLength = 0f;
			for (int i = 0; i < Consist.Items.Count; i++)
			{
				VehicleRecipeInstance recipeInstance = Consist.Items[i];
				bool flipped = recipeInstance.Flipped;
				int num2 = ((!flipped) ? 1 : (-1));
				VehicleUnitCheckboxGroup vehicleUnitCheckboxGroup = AddCheckboxGroup(runningLength, recipeInstance);
				vehicleUnitCheckboxGroup.OnCheckedValueChanged = delegate(bool value)
				{
					OnCheckboxGroupValueChanged(value, recipeInstance);
				};
				_checkboxGroups.Add(vehicleUnitCheckboxGroup);
				for (int j = (flipped ? (recipeInstance.Sections.Count - 1) : 0); j != (flipped ? (-1) : recipeInstance.Sections.Count); j += num2)
				{
					VehicleRecipeSectionInstance vehicleRecipeSectionInstance = recipeInstance.Sections[j];
					ImmutableList<VehicleUnit> units = vehicleRecipeSectionInstance.Units;
					for (int k = 0; k < units.Count; k++)
					{
						VehicleUnit unit = units[k];
						VehicleUnitView vehicleUnitView = Instantiate(R.Game.UI.VehicleEditorWindow.VehicleUnitView, _scrollView.content);
						vehicleUnitView.transform.SetAsSibling(-2);
						vehicleUnitView.Initialize(unit);
						float length = _vehicleRenderer.Camera.WorldToPixels(unit.SharedData.Length);
						vehicleUnitView.GetComponent<LayoutElement>().preferredWidth = length;
						vehicleUnitView.CapacityPickerButton.SetActive(false);
						_vehicleUnitViews.Add(vehicleUnitView);
						RectTransform component2 = vehicleUnitCheckboxGroup.GetComponent<RectTransform>();
						component2.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, component2.rect.width + length);
						vehicleUnitCheckboxGroup.Add(vehicleUnitView.Checkbox);
						vehicleUnitView.CheckboxGroup = vehicleUnitCheckboxGroup;
						runningLength += length;
					}
				}
			}
			ImitateDeselectionAnimation();
		}

    }
}