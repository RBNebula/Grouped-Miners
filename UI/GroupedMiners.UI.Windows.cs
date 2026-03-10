using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GroupedMiners;
public sealed partial class GroupedMiners
{
	private void DrawWindow(int id)
	{
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label(_editDirty ? "Assign Miner Group *" : "Assign Miner Group", _labelStyle ?? GUI.skin.label);
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Label($"Miner: {(string.IsNullOrWhiteSpace(_selectedMinerLabel) ? "Unknown" : _selectedMinerLabel)}", _labelStyle ?? GUI.skin.label);
		GUILayout.Label($"Pos: {_selectedMinerPos.x:0.##}, {_selectedMinerPos.y:0.##}, {_selectedMinerPos.z:0.##}", _labelStyle ?? GUI.skin.label);
		const float actionsBoxHeight = 96f;
		GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
		_mainScroll = GUILayout.BeginScrollView(_mainScroll, GUILayout.ExpandHeight(true));

		GUILayout.BeginVertical(GUI.skin.box);
		DrawSectionTitle("Group");
		GUILayout.Label("Select which group this miner belongs to.", _labelStyle ?? GUI.skin.label);
		DrawGroupSelectorRow();
		GUILayout.EndVertical();

		if (_groupPickerOpen)
		{
			DrawGroupPicker();
			GUILayout.Space(8f);
		}

		GUILayout.EndScrollView();
		GUILayout.EndVertical();

		GUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(actionsBoxHeight));
		DrawSectionTitle("Actions");
		GUILayout.BeginHorizontal();
		if (UiButton("Save Group", GUILayout.Width(120f), GUILayout.Height(26f)))
		{
			ApplySelectedConfig();
		}
		GUILayout.Space(6f);
		if (UiButton("Clear Group", GUILayout.Width(120f), GUILayout.Height(26f)))
		{
			_edit.group = 0;
			ApplySelectedConfig();
		}
		GUILayout.Space(6f);
		if (UiButton("Config", GUILayout.Width(80f), GUILayout.Height(26f)))
		{
			_configPanelOpen = !_configPanelOpen;
			ExitGuiAfterLayoutMutation();
		}
		GUILayout.Space(6f);
		if (UiButton("Close", GUILayout.Width(80f), GUILayout.Height(26f)))
		{
			_closeFocusAndClose();
			ExitGuiAfterLayoutMutation();
		}
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
		GUI.DragWindow();
	}

	private void DrawToggleMenuWindow(int id)
	{
		EnsureGroupNamesInitialized();
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label("Grouped Miners", _labelStyle ?? GUI.skin.label);
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		GUILayout.Label("Toggle miners by group and rename groups.", _labelStyle ?? GUI.skin.label);
		GUILayout.Space(8f);

		_mainScroll = GUILayout.BeginScrollView(_mainScroll, GUILayout.ExpandHeight(true));
		DrawToggleMenuRow("All AutoMiners", -1, 230f);
		GUILayout.Space(4f);
		for (var group = 1; group <= _groupCount; group++)
		{
			DrawToggleMenuGroupRow(group);
		}
		GUILayout.EndScrollView();

		GUILayout.Space(8f);
		GUILayout.BeginHorizontal();
		if (UiButton("Config", GUILayout.Width(80f)))
		{
			_configPanelOpen = !_configPanelOpen;
			ExitGuiAfterLayoutMutation();
		}
		GUILayout.Space(8f);
		if (UiButton("Close", GUILayout.Width(80f)))
		{
			_closeFocusAndClose();
			ExitGuiAfterLayoutMutation();
		}
		GUILayout.EndHorizontal();

		GUI.DragWindow();
	}

	private void DrawToggleMenuGroupRow(int group)
	{
		var index = group - 1;
		if (index < 0 || index >= _groupNames.Count) return;

		var cacheKey = $"menu_group_name:{group}";
		if (!_sliderTextCache.TryGetValue(cacheKey, out var text))
		{
			text = _groupNames[index];
		}

		GUILayout.BeginHorizontal();
		GUILayout.Label($"G{group}", _labelStyle ?? GUI.skin.label, GUILayout.Width(26f));
		GUI.SetNextControlName(cacheKey);
		text = GUILayout.TextField(text ?? string.Empty, _textFieldStyle ?? GUI.skin.textField, GUILayout.Width(170f));
		GUILayout.Space(6f);
		if (UiButton("On", GUILayout.Width(70f)))
		{
			ToggleGroupMiners(group, true);
		}
		if (UiButton("Off", GUILayout.Width(70f)))
		{
			ToggleGroupMiners(group, false);
		}
		GUILayout.EndHorizontal();

		_sliderTextCache[cacheKey] = text;
		var hasFocus = GUI.GetNameOfFocusedControl() == cacheKey;
		if (hasFocus &&
			Event.current.type == EventType.KeyDown &&
			(Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
		{
			_clearFocusNextGui = true;
		}
		if (hasFocus) return;

		var normalized = string.IsNullOrWhiteSpace(text) ? BuildDefaultGroupName(group) : text.Trim();
		if (string.Equals(_groupNames[index], normalized, StringComparison.Ordinal)) return;
		_groupNames[index] = normalized;
		_sliderTextCache[cacheKey] = normalized;
		SyncLegacyGroupNameFields();
		SaveSettingsToDisk();
	}

	private void DrawToggleMenuRow(string label, int group, float labelWidth = 220f)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label(label, _labelStyle ?? GUI.skin.label, GUILayout.Width(labelWidth));
		if (UiButton("On", GUILayout.Width(80f)))
		{
			if (group < 0) ToggleAllMiners(true);
			else ToggleGroupMiners(group, true);
		}
		if (UiButton("Off", GUILayout.Width(80f)))
		{
			if (group < 0) ToggleAllMiners(false);
			else ToggleGroupMiners(group, false);
		}
		GUILayout.EndHorizontal();
	}

	private void DrawSectionTitle(string title)
	{
		var style = _labelStyle ?? GUI.skin.label;
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label($"<size=112%>{title}</size>", style);
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
	}

}

