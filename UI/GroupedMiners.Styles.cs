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
	private void InitStyles()
	{
		var normalizedPreset = UiStylePresets.NormalizePresetId(_stylePreset);
		var styleChanged = !string.Equals(_appliedStylePreset, normalizedPreset, StringComparison.Ordinal);
		if (_stylesInitialized && !styleChanged) return;

		_stylePreset = normalizedPreset;
		var styleSet = UiStylePresets.Build(_stylePreset, DefaultWindowOpacity, includeSliderStyles: false);
		_windowStyle = styleSet.WindowStyle;
		_labelStyle = styleSet.LabelStyle;
		_textFieldStyle = styleSet.TextFieldStyle;
		_richButtonStyle = styleSet.RichButtonStyle;

		_appliedStylePreset = _stylePreset;
		_stylesInitialized = true;
	}
}

