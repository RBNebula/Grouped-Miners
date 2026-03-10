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
	private static string F(float value)
	{
		return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
	}

	private static string ReadNestedRaw(string json, string key, char open, char close)
	{
		return JsonSettingsUtils.ReadNestedRaw(json, key, open, close);
	}

	private static List<string> SplitTopLevelObjects(string rawArray)
	{
		return JsonSettingsUtils.SplitTopLevelObjects(rawArray);
	}

	private static string[] ParseStringArray(string raw)
	{
		return JsonSettingsUtils.ParseStringArray(raw);
	}

}

