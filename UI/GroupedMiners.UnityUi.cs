using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GroupedMiners;
public sealed partial class GroupedMiners
{
	private static readonly bool UseUnityUi = true;

	private GameObject? _unityUiRoot;
	private RectTransform? _unityWindowRect;
	private GameObject? _unityMinerPanel;
	private GameObject? _unityGroupsPanel;
	private Text? _unityTitleText;
	private Text? _unityHintText;
	private Text? _unityTargetText;
	private Text? _unityCoordsText;
	private Text? _unityAllGroupsCountText;
	private Image? _unityAllGroupsStatusDot;
	private Dropdown? _unityMinerGroupDropdown;
	private readonly List<int> _unityMinerGroupOptionValues = new();
	private RectTransform? _unityGroupsContent;
	private readonly List<UnityGroupRowBinding> _unityGroupRows = new();
	private Font? _unityFont;
	private bool _unityUiSyncing;
	private bool _unityConfigOpen;
	private bool _unityGroupConfigOpen;
	private static Sprite? _unityUiSolidSprite;
	private static Sprite? _unityUiCircleSprite;
	private const float UnityGroupNameWidth = 160f;
	private const float UnityGroupCountWidth = 68f;
	private const float UnityGroupStatusDotWidth = 14f;
	private const float UnityGroupToggleWidth = 56f;
	private const float UnityGroupRemoveWidth = 24f;
	private const float UnityMinerRowSpacing = 8f;
	private const float UnityGroupRowSpacing = 6f;
	private const float UnityGroupsScrollReserveWidth = 110f;
	private const float UnityMinerRightReserveWidth = 50f;
	private const float UnityGroupsWindowHeight = 620f;
	private const float UnityMinerWindowHeight = 360f;
	private const float UnityInteriorPadding = 20f;

	private sealed class UnityGroupRowBinding
	{
		public int group;
		public InputField nameInput = null!;
		public Text countText = null!;
		public Image statusDot = null!;
	}

	private void SetUnityUiVisible(bool visible)
	{
		if (!UseUnityUi) return;
		EnsureUnityUiBuilt();
		if (_unityUiRoot != null)
		{
			_unityUiRoot.SetActive(visible);
		}
		if (visible)
		{
			RefreshUnityUi();
		}
	}

	private void DestroyUnityUi()
	{
		if (_unityUiRoot == null) return;
		Destroy(_unityUiRoot);
		_unityUiRoot = null;
		_unityWindowRect = null;
		_unityMinerPanel = null;
		_unityGroupsPanel = null;
		_unityTitleText = null;
		_unityHintText = null;
		_unityTargetText = null;
		_unityCoordsText = null;
		_unityAllGroupsCountText = null;
		_unityAllGroupsStatusDot = null;
		_unityMinerGroupDropdown = null;
		_unityGroupsContent = null;
		_unityGroupRows.Clear();
		_unityMinerGroupOptionValues.Clear();
	}

	private void EnsureUnityUiBuilt()
	{
		if (_unityUiRoot != null) return;

		EnsureEventSystem();
		_unityFont ??= Resources.GetBuiltinResource<Font>("Arial.ttf");

		var root = new GameObject(
			"GroupedMiners_UnityUI",
			typeof(RectTransform),
			typeof(Canvas),
			typeof(CanvasScaler),
			typeof(GraphicRaycaster));
		DontDestroyOnLoad(root);

		var canvas = root.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 6000;

		var scaler = root.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
		scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
		scaler.matchWidthOrHeight = 0.5f;

		var overlay = CreateImageRect(root.transform, "Overlay", new Color(0.01f, 0.02f, 0.04f, 0.76f));
		StretchToParent(overlay, 0f);

		var window = CreateImageRect(overlay, "Window", new Color(0.06f, 0.09f, 0.14f, 0.96f));
		window.anchorMin = new Vector2(0.5f, 0.5f);
		window.anchorMax = new Vector2(0.5f, 0.5f);
		window.pivot = new Vector2(0.5f, 0.5f);
		window.sizeDelta = new Vector2(GetUnityWindowWidth(isMinerPanel: false), UnityGroupsWindowHeight);
		window.anchoredPosition = Vector2.zero;
		var outline = window.gameObject.AddComponent<Outline>();
		outline.effectColor = new Color(0.22f, 0.66f, 0.97f, 0.45f);
		outline.effectDistance = new Vector2(1f, -1f);
		_unityWindowRect = window;

		var content = CreateRectOnly(window, "Content");
		StretchToParent(content, 16f);
		var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
		contentLayout.padding = new RectOffset(8, 8, 8, 8);
		contentLayout.spacing = 10f;
		contentLayout.childControlHeight = true;
		contentLayout.childControlWidth = true;
		contentLayout.childForceExpandHeight = false;
		contentLayout.childForceExpandWidth = true;

		_unityTitleText = CreateTextLine(content, "Grouped Miners", 20, FontStyle.Bold, TextAnchor.MiddleCenter, 36f, new Color(0.9f, 0.96f, 1f, 1f));
		_unityHintText = CreateTextLine(content, "", 12, FontStyle.Normal, TextAnchor.MiddleCenter, 20f, new Color(0.78f, 0.88f, 0.97f, 0.95f));

		_unityMinerPanel = CreateRectOnly(content, "MinerPanel").gameObject;
		BuildUnityMinerPanel(_unityMinerPanel.transform);

		_unityGroupsPanel = CreateRectOnly(content, "GroupsPanel").gameObject;
		BuildUnityGroupsPanel(_unityGroupsPanel.transform);

		var footer = CreateRectOnly(content, "Footer");
		var footerLayout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
		footerLayout.spacing = 8f;
		footerLayout.childAlignment = TextAnchor.MiddleCenter;
		footerLayout.childControlHeight = true;
		footerLayout.childControlWidth = false;
		footerLayout.childForceExpandHeight = false;
		footerLayout.childForceExpandWidth = false;
		var footerElement = footer.gameObject.AddComponent<LayoutElement>();
		footerElement.preferredHeight = 40f;
		CreateActionButton(footer, "Close", _closeFocusAndClose, 120f, 34f);

		_unityUiRoot = root;
		root.SetActive(false);
	}

	private void BuildUnityMinerPanel(Transform parent)
	{
		var panel = parent as RectTransform;
		if (panel == null) return;
		var panelElement = panel.gameObject.AddComponent<LayoutElement>();
		panelElement.flexibleHeight = 0f;
		panelElement.minHeight = 120f;
		var panelBg = panel.gameObject.AddComponent<Image>();
		EnsureUnityUiImageSprite(panelBg);
		panelBg.color = new Color(0.09f, 0.13f, 0.19f, 0.95f);
		var panelOutline = panel.gameObject.AddComponent<Outline>();
		panelOutline.effectColor = new Color(0.2f, 0.53f, 0.81f, 0.3f);
		panelOutline.effectDistance = new Vector2(1f, -1f);

		var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
		layout.padding = new RectOffset(16, 16, 16, 16);
		layout.spacing = 8f;
		layout.childAlignment = TextAnchor.UpperLeft;
		layout.childControlHeight = true;
		layout.childControlWidth = true;
		layout.childForceExpandHeight = false;
		layout.childForceExpandWidth = true;

		var detailsRow = CreateRectOnly(panel, "DetailsRow");
		var detailsLayout = detailsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
		detailsLayout.spacing = 8f;
		detailsLayout.childAlignment = TextAnchor.MiddleLeft;
		detailsLayout.childControlHeight = true;
		detailsLayout.childControlWidth = true;
		detailsLayout.childForceExpandHeight = false;
		detailsLayout.childForceExpandWidth = false;
		var detailsElement = detailsRow.gameObject.AddComponent<LayoutElement>();
		detailsElement.preferredHeight = 28f;

		_unityTargetText = CreateTextLine(detailsRow, "Miner: N/A", 13, FontStyle.Normal, TextAnchor.MiddleLeft, 24f, new Color(0.84f, 0.93f, 1f, 0.95f));
		var targetElement = _unityTargetText.gameObject.GetComponent<LayoutElement>();
		targetElement.preferredWidth = 230f;
		targetElement.minWidth = 230f;
		_unityCoordsText = CreateTextLine(detailsRow, "Pos: N/A", 13, FontStyle.Normal, TextAnchor.MiddleLeft, 24f, new Color(0.84f, 0.93f, 1f, 0.95f));
		var coordsElement = _unityCoordsText.gameObject.GetComponent<LayoutElement>();
		coordsElement.flexibleWidth = 1f;

		var row = CreateRectOnly(panel, "GroupRow");
		var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
		rowLayout.spacing = 8f;
		rowLayout.childAlignment = TextAnchor.MiddleLeft;
		rowLayout.childControlHeight = true;
		rowLayout.childControlWidth = false;
		rowLayout.childForceExpandHeight = false;
		rowLayout.childForceExpandWidth = false;
		var rowElement = row.gameObject.AddComponent<LayoutElement>();
		rowElement.preferredHeight = 36f;

		var label = CreateTextLine(row, "Group", 13, FontStyle.Bold, TextAnchor.MiddleLeft, 32f, new Color(0.87f, 0.95f, 1f, 1f));
		var labelElement = label.gameObject.GetComponent<LayoutElement>();
		labelElement.preferredWidth = 44f;
		labelElement.minWidth = 44f;

		_unityMinerGroupDropdown = CreateDropdown(row, 154f, OnUnityMinerGroupChanged);
		CreateActionButton(row, "Save", ApplySelectedConfig, 86f, 32f);
		CreateActionButton(row, "Clear", () =>
		{
			_edit.group = 0;
			ApplySelectedConfig();
		}, 58f, 32f);
	}

	private void BuildUnityGroupsPanel(Transform parent)
	{
		var panel = parent as RectTransform;
		if (panel == null) return;
		var panelElement = panel.gameObject.AddComponent<LayoutElement>();
		panelElement.flexibleHeight = 1f;
		panelElement.minHeight = 420f;
		var panelBg = panel.gameObject.AddComponent<Image>();
		EnsureUnityUiImageSprite(panelBg);
		panelBg.color = new Color(0.09f, 0.13f, 0.19f, 0.95f);
		var panelOutline = panel.gameObject.AddComponent<Outline>();
		panelOutline.effectColor = new Color(0.2f, 0.53f, 0.81f, 0.3f);
		panelOutline.effectDistance = new Vector2(1f, -1f);

		var panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
		panelLayout.padding = new RectOffset(16, 16, 16, 16);
		panelLayout.spacing = 8f;
		panelLayout.childControlHeight = true;
		panelLayout.childControlWidth = true;
		panelLayout.childForceExpandHeight = false;
		panelLayout.childForceExpandWidth = true;

		var scrollGo = DefaultControls.CreateScrollView(new DefaultControls.Resources());
		scrollGo.name = "GroupsScroll";
		scrollGo.transform.SetParent(panel, false);
		var scrollRect = scrollGo.GetComponent<ScrollRect>();
		scrollRect.horizontal = false;
		scrollRect.vertical = true;
		var scrollElement = scrollGo.GetComponent<LayoutElement>() ?? scrollGo.AddComponent<LayoutElement>();
		scrollElement.flexibleHeight = 1f;
		scrollElement.minHeight = 320f;

		var scrollImage = scrollGo.GetComponent<Image>();
		if (scrollImage != null)
		{
			EnsureUnityUiImageSprite(scrollImage);
			scrollImage.color = new Color(0.08f, 0.12f, 0.18f, 0.94f);
		}

		var viewportImage = scrollGo.transform.Find("Viewport")?.GetComponent<Image>();
		if (viewportImage != null)
		{
			EnsureUnityUiImageSprite(viewportImage);
			viewportImage.color = new Color(0.07f, 0.11f, 0.17f, 0.96f);
		}

		var horizontalScrollbar = scrollGo.transform.Find("Scrollbar Horizontal");
		if (horizontalScrollbar != null)
		{
			horizontalScrollbar.gameObject.SetActive(false);
		}

		var content = scrollGo.transform.Find("Viewport/Content")?.GetComponent<RectTransform>();
		if (content == null)
		{
			Logger.LogWarning("GroupedMiners Unity UI: failed to build groups scroll content.");
			return;
		}

		content.anchorMin = new Vector2(0f, 1f);
		content.anchorMax = new Vector2(1f, 1f);
		content.pivot = new Vector2(0.5f, 1f);
		content.offsetMin = new Vector2(0f, 0f);
		content.offsetMax = new Vector2(0f, 0f);
		var contentLayout = content.gameObject.GetComponent<VerticalLayoutGroup>() ?? content.gameObject.AddComponent<VerticalLayoutGroup>();
		contentLayout.padding = new RectOffset(6, 6, 6, 6);
		contentLayout.spacing = 8f;
		contentLayout.childControlWidth = true;
		contentLayout.childControlHeight = true;
		contentLayout.childForceExpandWidth = true;
		contentLayout.childForceExpandHeight = false;
		contentLayout.childAlignment = TextAnchor.UpperLeft;
		var fitter = content.gameObject.GetComponent<ContentSizeFitter>() ?? content.gameObject.AddComponent<ContentSizeFitter>();
		fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		_unityGroupsContent = content;
		RebuildUnityGroupRows();

		var addRow = CreateRectOnly(panel, "GroupsAddRow");
		var addLayout = addRow.gameObject.AddComponent<HorizontalLayoutGroup>();
		addLayout.spacing = 6f;
		addLayout.childAlignment = TextAnchor.MiddleCenter;
		addLayout.childControlHeight = true;
		addLayout.childControlWidth = false;
		addLayout.childForceExpandHeight = false;
		addLayout.childForceExpandWidth = false;
		var addElement = addRow.gameObject.AddComponent<LayoutElement>();
		addElement.preferredHeight = 36f;
		CreateActionButton(addRow, "+", () =>
		{
			if (!TryAddGroupInternal(showToast: true)) return;
			RebuildUnityGroupRows();
			RefreshUnityMinerGroupDropdown();
		}, 32f, 30f);
		CreateTextLine(addRow, "Add Group", 12, FontStyle.Normal, TextAnchor.MiddleLeft, 30f, new Color(0.79f, 0.9f, 0.98f, 1f));
	}

	private void RefreshUnityUi()
	{
		if (!UseUnityUi || !_uiOpen) return;
		EnsureUnityUiBuilt();
		if (_unityUiRoot == null || !_unityUiRoot.activeSelf) return;

		EnsureGroupNamesInitialized();

		var isMinerPanel = _activePanel == ActivePanel.MinerConfig;
		if (_unityWindowRect != null)
		{
			var targetHeight = isMinerPanel ? UnityMinerWindowHeight : UnityGroupsWindowHeight;
			_unityWindowRect.sizeDelta = new Vector2(GetUnityWindowWidth(isMinerPanel), targetHeight);
		}
		if (_unityMinerPanel != null) _unityMinerPanel.SetActive(isMinerPanel);
		if (_unityGroupsPanel != null) _unityGroupsPanel.SetActive(!isMinerPanel);

		if (_unityTitleText != null)
		{
			_unityTitleText.text = isMinerPanel
				? (_editDirty ? "Assign Miner Group *" : "Assign Miner Group")
				: "Grouped Miners";
		}
		if (_unityHintText != null)
		{
			_unityHintText.text = isMinerPanel
				? "Use dropdown to set group for selected miner"
				: "Toggle groups on/off and rename group names";
		}

		if (isMinerPanel)
		{
			RefreshUnityMinerPanel();
		}
		else
		{
			RefreshUnityGroupsPanel();
		}
	}

	private void RefreshUnityMinerPanel()
	{
		if (_selectedMiner != null)
		{
			_selectedMinerPos = ReadMinerPosition(_selectedMiner);
			_selectedMinerLabel = ReadMinerLabel(_selectedMiner);
		}

		if (_unityTargetText != null)
		{
			_unityTargetText.text = string.IsNullOrWhiteSpace(_selectedMinerLabel)
				? "Miner: N/A"
				: $"Miner: {_selectedMinerLabel}";
		}

		if (_unityCoordsText != null)
		{
			_unityCoordsText.text = _selectedMiner == null
				? "Pos: N/A"
				: $"Pos: {_selectedMinerPos.x:0.##}, {_selectedMinerPos.y:0.##}, {_selectedMinerPos.z:0.##}";
		}

		RefreshUnityMinerGroupDropdown();
	}

	private void RefreshUnityMinerGroupDropdown()
	{
		if (_unityMinerGroupDropdown == null) return;

		_unityUiSyncing = true;
		_unityMinerGroupOptionValues.Clear();
		_unityMinerGroupDropdown.options.Clear();

		_unityMinerGroupOptionValues.Add(0);
		_unityMinerGroupDropdown.options.Add(new Dropdown.OptionData("None"));
		for (var group = 1; group <= _groupCount; group++)
		{
			_unityMinerGroupOptionValues.Add(group);
			_unityMinerGroupDropdown.options.Add(new Dropdown.OptionData(GetGroupDisplayName(group)));
		}

		var selectedGroup = Mathf.Clamp(_edit.group, 0, _groupCount);
		var selectedIndex = _unityMinerGroupOptionValues.IndexOf(selectedGroup);
		if (selectedIndex < 0) selectedIndex = 0;
		_unityMinerGroupDropdown.SetValueWithoutNotify(selectedIndex);
		_unityMinerGroupDropdown.RefreshShownValue();
		_unityUiSyncing = false;
	}

	private void OnUnityMinerGroupChanged(int optionIndex)
	{
		if (_unityUiSyncing) return;
		if (optionIndex < 0 || optionIndex >= _unityMinerGroupOptionValues.Count) return;
		var group = _unityMinerGroupOptionValues[optionIndex];
		if (_edit.group == group) return;
		_edit.group = group;
		_editDirty = true;
	}

	private void RefreshUnityGroupsPanel()
	{
		if (_unityGroupsContent == null) return;
		if (_unityGroupRows.Count != _groupCount)
		{
			RebuildUnityGroupRows();
		}
		if (_unityAllGroupsCountText != null)
		{
			_unityAllGroupsCountText.text = $"{_minerCache.Count} miner(s)";
		}
		UpdateStatusDotColor(_unityAllGroupsStatusDot, GetEnabledSummaryForGroup(-1));

		for (var i = 0; i < _unityGroupRows.Count; i++)
		{
			var row = _unityGroupRows[i];
			var group = row.group;
			if (!row.nameInput.isFocused)
			{
				row.nameInput.SetTextWithoutNotify(GetGroupDisplayName(group));
			}
			row.countText.text = $"{CountGroupMiners(group)} miner(s)";
			UpdateStatusDotColor(row.statusDot, GetEnabledSummaryForGroup(group));
		}
	}

	private void RebuildUnityGroupRows()
	{
		if (_unityGroupsContent == null) return;
		_unityGroupRows.Clear();
		for (var i = _unityGroupsContent.childCount - 1; i >= 0; i--)
		{
			Destroy(_unityGroupsContent.GetChild(i).gameObject);
		}

		BuildUnityAllMinersRow(_unityGroupsContent);
		for (var group = 1; group <= _groupCount; group++)
		{
			BuildUnityGroupRow(_unityGroupsContent, group);
		}
	}

	private void BuildUnityAllMinersRow(Transform parent)
	{
		var row = CreateRectOnly(parent, "AllRow");
		var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
		rowLayout.spacing = 6f;
		rowLayout.childAlignment = TextAnchor.MiddleLeft;
		rowLayout.childControlHeight = true;
		rowLayout.childControlWidth = false;
		rowLayout.childForceExpandHeight = false;
		rowLayout.childForceExpandWidth = false;
		var rowElement = row.gameObject.AddComponent<LayoutElement>();
		rowElement.preferredHeight = 36f;

		var allNameField = CreateTextInput(row, UnityGroupNameWidth, _ => { });
		allNameField.SetTextWithoutNotify("All AutoMiners");
		allNameField.readOnly = true;

		_unityAllGroupsCountText = CreateTextLine(row, $"{_minerCache.Count} miner(s)", 12, FontStyle.Normal, TextAnchor.MiddleLeft, 32f, new Color(0.79f, 0.9f, 0.98f, 1f));
		var countElement = _unityAllGroupsCountText.gameObject.GetComponent<LayoutElement>();
		countElement.preferredWidth = UnityGroupCountWidth;
		countElement.minWidth = UnityGroupCountWidth;
		_unityAllGroupsStatusDot = CreateStatusDot(row, "AllMinersStatusDot");

		CreateActionButton(row, "On", () => ToggleAllMiners(true), UnityGroupToggleWidth, 32f);
		CreateActionButton(row, "Off", () => ToggleAllMiners(false), UnityGroupToggleWidth, 32f);

		var removeSpacer = CreateRectOnly(row, "AllRowRemoveSpacer");
		var removeSpacerElement = removeSpacer.gameObject.AddComponent<LayoutElement>();
		removeSpacerElement.preferredWidth = UnityGroupRemoveWidth;
		removeSpacerElement.minWidth = UnityGroupRemoveWidth;
		removeSpacerElement.preferredHeight = 32f;
		removeSpacerElement.minHeight = 32f;
	}

	private void BuildUnityGroupRow(Transform parent, int group)
	{
		var row = CreateRectOnly(parent, $"GroupRow{group}");
		var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
		rowLayout.spacing = 6f;
		rowLayout.childAlignment = TextAnchor.MiddleLeft;
		rowLayout.childControlHeight = true;
		rowLayout.childControlWidth = false;
		rowLayout.childForceExpandHeight = false;
		rowLayout.childForceExpandWidth = false;
		var rowElement = row.gameObject.AddComponent<LayoutElement>();
		rowElement.preferredHeight = 36f;

		var input = CreateTextInput(row, UnityGroupNameWidth, value =>
		{
			if (!SetGroupName(group, value)) return;
			RefreshUnityGroupsPanel();
		});
		input.SetTextWithoutNotify(GetGroupDisplayName(group));

		var countText = CreateTextLine(row, "0 miner(s)", 12, FontStyle.Normal, TextAnchor.MiddleLeft, 32f, new Color(0.79f, 0.9f, 0.98f, 1f));
		var countElement = countText.gameObject.GetComponent<LayoutElement>();
		countElement.preferredWidth = UnityGroupCountWidth;
		countElement.minWidth = UnityGroupCountWidth;
		var statusDot = CreateStatusDot(row, $"Group{group}StatusDot");

		CreateActionButton(row, "On", () => ToggleGroupMiners(group, true), UnityGroupToggleWidth, 32f);
		CreateActionButton(row, "Off", () => ToggleGroupMiners(group, false), UnityGroupToggleWidth, 32f);
		CreateActionButton(row, "-", () =>
		{
			if (!TryRemoveGroupInternal(group, showToast: true)) return;
			RebuildUnityGroupRows();
			RefreshUnityMinerGroupDropdown();
		}, UnityGroupRemoveWidth, 32f);

		_unityGroupRows.Add(new UnityGroupRowBinding
		{
			group = group,
			nameInput = input,
			countText = countText,
			statusDot = statusDot
		});
	}

	private static void EnsureEventSystem()
	{
		if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
		var eventSystem = new GameObject("GroupedMinersEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
		DontDestroyOnLoad(eventSystem);
	}

	private static RectTransform CreateRectOnly(Transform parent, string name)
	{
		var go = new GameObject(name, typeof(RectTransform));
		go.transform.SetParent(parent, false);
		return go.GetComponent<RectTransform>();
	}

	private Image CreateStatusDot(Transform parent, string name)
	{
		var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		go.transform.SetParent(parent, false);
		var image = go.GetComponent<Image>();
		image.sprite = GetUnityUiCircleSprite();
		image.type = Image.Type.Simple;
		image.preserveAspect = true;
		image.raycastTarget = false;

		var element = go.AddComponent<LayoutElement>();
		element.preferredWidth = UnityGroupStatusDotWidth;
		element.minWidth = UnityGroupStatusDotWidth;
		element.preferredHeight = UnityGroupStatusDotWidth;
		element.minHeight = UnityGroupStatusDotWidth;
		return image;
	}

	private static void UpdateStatusDotColor(Image? dot, MinerEnabledSummary summary)
	{
		if (dot == null) return;
		dot.color = summary switch
		{
			MinerEnabledSummary.AllOn => new Color(0.23f, 0.85f, 0.42f, 1f),
			MinerEnabledSummary.Mixed => new Color(0.98f, 0.65f, 0.21f, 1f),
			_ => new Color(0.92f, 0.27f, 0.27f, 1f)
		};
	}

	private static RectTransform CreateImageRect(Transform parent, string name, Color color)
	{
		var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		go.transform.SetParent(parent, false);
		var image = go.GetComponent<Image>();
		EnsureUnityUiImageSprite(image);
		image.color = color;
		return go.GetComponent<RectTransform>();
	}

	private static void StretchToParent(RectTransform rect, float margin)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = new Vector2(margin, margin);
		rect.offsetMax = new Vector2(-margin, -margin);
	}

	private Text CreateTextLine(Transform parent, string text, int fontSize, FontStyle style, TextAnchor anchor, float height, Color color)
	{
		var go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
		go.transform.SetParent(parent, false);
		var t = go.GetComponent<Text>();
		t.font = _unityFont;
		t.text = text;
		t.fontSize = fontSize;
		t.fontStyle = style;
		t.alignment = anchor;
		t.color = color;
		t.horizontalOverflow = HorizontalWrapMode.Wrap;
		t.verticalOverflow = VerticalWrapMode.Truncate;

		var le = go.AddComponent<LayoutElement>();
		le.minHeight = height;
		le.preferredHeight = height;
		return t;
	}

	private Button CreateActionButton(Transform parent, string text, Action onClick, float width = 0f, float height = 34f)
	{
		var buttonGo = DefaultControls.CreateButton(new DefaultControls.Resources());
		buttonGo.name = text.Replace(" ", string.Empty) + "Button";
		buttonGo.transform.SetParent(parent, false);
		var button = buttonGo.GetComponent<Button>();
		var image = buttonGo.GetComponent<Image>();
		if (image != null)
		{
			EnsureUnityUiImageSprite(image);
			image.color = new Color(0.18f, 0.36f, 0.54f, 0.95f);
		}

		var label = buttonGo.GetComponentInChildren<Text>();
		if (label != null)
		{
			label.font = _unityFont;
			label.text = text;
			label.fontSize = 14;
			label.color = new Color(0.94f, 0.98f, 1f, 1f);
		}

		button.onClick.AddListener(() => onClick());

		var rect = buttonGo.GetComponent<RectTransform>();
		if (rect != null)
		{
			rect.sizeDelta = width > 0f ? new Vector2(width, height) : new Vector2(rect.sizeDelta.x, height);
		}

		var le = buttonGo.GetComponent<LayoutElement>() ?? buttonGo.AddComponent<LayoutElement>();
		if (width > 0f)
		{
			le.preferredWidth = width;
			le.minWidth = width;
			le.flexibleWidth = 0f;
		}
		else
		{
			le.flexibleWidth = 1f;
		}
		le.preferredHeight = height;
		le.minHeight = height;
		return button;
	}

	private InputField CreateTextInput(Transform parent, float width, Action<string> onEndEdit)
	{
		var inputGo = DefaultControls.CreateInputField(new DefaultControls.Resources());
		inputGo.name = "GroupNameInput";
		inputGo.transform.SetParent(parent, false);

		var input = inputGo.GetComponent<InputField>();
		input.contentType = InputField.ContentType.Standard;
		input.lineType = InputField.LineType.SingleLine;
		input.characterValidation = InputField.CharacterValidation.None;
		input.text = string.Empty;
		input.onEndEdit.AddListener(value => onEndEdit(value));

		if (input.targetGraphic is Image bg)
		{
			EnsureUnityUiImageSprite(bg);
			bg.color = new Color(0.12f, 0.19f, 0.28f, 0.95f);
		}

		if (input.textComponent != null)
		{
			input.textComponent.font = _unityFont;
			input.textComponent.fontSize = 12;
			input.textComponent.color = Color.white;
			input.textComponent.alignment = TextAnchor.MiddleLeft;
			input.textComponent.supportRichText = false;
		}
		if (input.placeholder is Text placeholderText)
		{
			placeholderText.font = _unityFont;
			placeholderText.fontSize = 12;
			placeholderText.text = string.Empty;
			placeholderText.color = new Color(0.75f, 0.82f, 0.9f, 0.7f);
			placeholderText.alignment = TextAnchor.MiddleLeft;
		}

		var element = inputGo.GetComponent<LayoutElement>() ?? inputGo.AddComponent<LayoutElement>();
		element.preferredWidth = width;
		element.minWidth = width;
		element.preferredHeight = 32f;
		element.minHeight = 32f;
		return input;
	}

	private Dropdown CreateDropdown(Transform parent, float width, Action<int> onChanged)
	{
		var dropdownGo = DefaultControls.CreateDropdown(new DefaultControls.Resources());
		dropdownGo.name = "GroupDropdown";
		dropdownGo.transform.SetParent(parent, false);

		var dropdown = dropdownGo.GetComponent<Dropdown>();
		dropdown.onValueChanged.AddListener(value => onChanged(value));

		var label = dropdownGo.transform.Find("Label")?.GetComponent<Text>();
		if (label != null)
		{
			label.font = _unityFont;
			label.fontSize = 12;
			label.color = Color.white;
			var labelRect = label.rectTransform;
			labelRect.anchorMin = new Vector2(0f, 0f);
			labelRect.anchorMax = new Vector2(1f, 1f);
			labelRect.offsetMin = new Vector2(10f, 0f);
			labelRect.offsetMax = new Vector2(-30f, 0f);
			label.alignment = TextAnchor.MiddleLeft;
		}
		dropdown.captionText = label;

		var itemText = dropdownGo.transform.Find("Template/Viewport/Content/Item/Item Label")?.GetComponent<Text>();
		if (itemText != null)
		{
			itemText.font = _unityFont;
			itemText.fontSize = 12;
			itemText.color = Color.white;
		}
		dropdown.itemText = itemText;

		if (dropdownGo.GetComponent<Image>() is Image image)
		{
			ForceUnityUiSolidImage(image);
			image.color = new Color(0.14f, 0.24f, 0.36f, 0.95f);
		}

		var arrow = dropdownGo.transform.Find("Arrow")?.GetComponent<Image>();
		if (arrow != null)
		{
			EnsureUnityUiImageSprite(arrow);
			arrow.color = new Color(0.86f, 0.93f, 1f, 0.98f);
			var arrowRect = arrow.rectTransform;
			arrowRect.anchorMin = new Vector2(1f, 0.5f);
			arrowRect.anchorMax = new Vector2(1f, 0.5f);
			arrowRect.pivot = new Vector2(0.5f, 0.5f);
			arrowRect.sizeDelta = new Vector2(8f, 8f);
			arrowRect.anchoredPosition = new Vector2(-12f, 0f);
		}

		var template = dropdownGo.transform.Find("Template");
		if (template != null)
		{
			var templateImage = template.GetComponent<Image>();
			if (templateImage != null)
			{
				ForceUnityUiSolidImage(templateImage);
				templateImage.color = new Color(0.08f, 0.12f, 0.18f, 0.98f);
			}

			var viewportImage = template.Find("Viewport")?.GetComponent<Image>();
			if (viewportImage != null)
			{
				ForceUnityUiSolidImage(viewportImage);
				viewportImage.color = new Color(0.09f, 0.14f, 0.21f, 0.98f);
			}

			var viewportRect = template.Find("Viewport") as RectTransform;
			if (viewportRect != null)
			{
				viewportRect.offsetMin = new Vector2(viewportRect.offsetMin.x, 0f);
				viewportRect.offsetMax = new Vector2(viewportRect.offsetMax.x, 0f);
			}

			var content = template.Find("Viewport/Content");
			if (content != null)
			{
				var contentBg = content.GetComponent<Image>();
				if (contentBg != null)
				{
					ForceUnityUiSolidImage(contentBg);
					contentBg.color = new Color(0.09f, 0.14f, 0.21f, 0.98f);
				}

				var item = content.Find("Item");
				if (item != null)
				{
					var itemBackground =
						item.Find("Item Background")?.GetComponent<Image>() ??
						item.Find("Background")?.GetComponent<Image>();
					if (itemBackground != null)
					{
						ForceUnityUiSolidImage(itemBackground);
						itemBackground.color = new Color(0.10f, 0.16f, 0.24f, 0.98f);
					}

					var itemCheckmark =
						item.Find("Item Checkmark")?.GetComponent<Image>() ??
						item.Find("Checkmark")?.GetComponent<Image>();
					if (itemCheckmark != null)
					{
						itemCheckmark.sprite = GetUnityUiCircleSprite();
						itemCheckmark.type = Image.Type.Simple;
						itemCheckmark.preserveAspect = true;
						itemCheckmark.color = new Color(0.35f, 0.83f, 1f, 1f);
						itemCheckmark.raycastTarget = false;
						var checkRect = itemCheckmark.rectTransform;
						checkRect.anchorMin = new Vector2(0f, 0.5f);
						checkRect.anchorMax = new Vector2(0f, 0.5f);
						checkRect.pivot = new Vector2(0.5f, 0.5f);
						checkRect.anchoredPosition = new Vector2(12f, 0f);
						checkRect.sizeDelta = new Vector2(11f, 11f);
					}
				}
			}

			var scrollbarImage = template.Find("Scrollbar")?.GetComponent<Image>();
			if (scrollbarImage != null)
			{
				ForceUnityUiSolidImage(scrollbarImage);
				scrollbarImage.color = new Color(0.10f, 0.16f, 0.24f, 0.95f);
			}
			var handleImage = template.Find("Scrollbar/Sliding Area/Handle")?.GetComponent<Image>();
			if (handleImage != null)
			{
				ForceUnityUiSolidImage(handleImage);
				handleImage.color = new Color(0.30f, 0.66f, 0.93f, 0.95f);
			}
		}

		var element = dropdownGo.GetComponent<LayoutElement>() ?? dropdownGo.AddComponent<LayoutElement>();
		element.preferredWidth = width;
		element.minWidth = width;
		element.flexibleWidth = 0f;
		element.preferredHeight = 32f;
		element.minHeight = 32f;

		return dropdown;
	}

	private void SetUnityConfigOpen(bool open)
	{
		_unityConfigOpen = open;
	}

	private void SetUnityGroupConfigOpen(bool open)
	{
		_unityGroupConfigOpen = open;
	}

	private void RebuildUnityConfigOverlayContent()
	{
		// Overlay panels removed in the simplified grouped-only UI.
	}

	private void RebuildUnityPresetEditor()
	{
		// Preset editor removed in the simplified grouped-only UI.
	}

	private static float GetRowWidth(float spacing, params float[] columnWidths)
	{
		if (columnWidths == null || columnWidths.Length == 0) return 0f;
		var total = 0f;
		for (var i = 0; i < columnWidths.Length; i++)
		{
			total += Mathf.Max(0f, columnWidths[i]);
		}
		total += Mathf.Max(0f, spacing) * Mathf.Max(0, columnWidths.Length - 1);
		return total;
	}

	private static float GetUnityWindowWidth(bool isMinerPanel)
	{
		var minerRowWidth = GetRowWidth(UnityMinerRowSpacing, 44f, 154f, 86f, 58f);
		var groupsRowWidth = GetGroupsRowWidth();
		var requiredInteriorWidth = isMinerPanel
			? minerRowWidth + (UnityInteriorPadding * 2f) + UnityMinerRightReserveWidth
			: (groupsRowWidth + UnityGroupsScrollReserveWidth) + (UnityInteriorPadding * 2f);

		const float windowToContentMargins = 32f; // StretchToParent(content, 16f)
		const float contentLayoutHorizontalPadding = 16f; // contentLayout.padding left+right (8+8)
		const float panelHorizontalPadding = 32f; // panel layout padding left+right (16+16)

		return Mathf.Ceil(requiredInteriorWidth
			+ windowToContentMargins
			+ contentLayoutHorizontalPadding
			+ panelHorizontalPadding);
	}

	private static float GetGroupsRowWidth()
	{
		return GetRowWidth(
			UnityGroupRowSpacing,
			UnityGroupNameWidth,
			UnityGroupCountWidth,
			UnityGroupStatusDotWidth,
			UnityGroupToggleWidth,
			UnityGroupToggleWidth,
			UnityGroupRemoveWidth);
	}

	private static void EnsureUnityUiImageSprite(Image image)
	{
		if (image.sprite != null) return;
		image.sprite = GetUnityUiSolidSprite();
		image.type = Image.Type.Simple;
	}

	private static void ForceUnityUiSolidImage(Image image)
	{
		image.sprite = GetUnityUiSolidSprite();
		image.type = Image.Type.Simple;
		image.preserveAspect = false;
	}

	private static Sprite GetUnityUiSolidSprite()
	{
		if (_unityUiSolidSprite != null) return _unityUiSolidSprite;
		var tex = Texture2D.whiteTexture;
		_unityUiSolidSprite = Sprite.Create(
			tex,
			new Rect(0f, 0f, tex.width, tex.height),
			new Vector2(0.5f, 0.5f));
		return _unityUiSolidSprite;
	}

	private static Sprite GetUnityUiCircleSprite()
	{
		if (_unityUiCircleSprite != null) return _unityUiCircleSprite;
		const int size = 32;
		var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
		{
			wrapMode = TextureWrapMode.Clamp,
			filterMode = FilterMode.Bilinear
		};

		var center = (size - 1) * 0.5f;
		var radius = size * 0.36f;
		var edge = 1.5f;
		for (var y = 0; y < size; y++)
		{
			for (var x = 0; x < size; x++)
			{
				var dx = x - center;
				var dy = y - center;
				var dist = Mathf.Sqrt((dx * dx) + (dy * dy));
				var alpha = Mathf.Clamp01((radius - dist) / edge);
				tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
			}
		}

		tex.Apply();
		_unityUiCircleSprite = Sprite.Create(
			tex,
			new Rect(0f, 0f, size, size),
			new Vector2(0.5f, 0.5f),
			100f);
		return _unityUiCircleSprite;
	}
}
