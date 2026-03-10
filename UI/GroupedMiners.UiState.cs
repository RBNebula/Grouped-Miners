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
	private static void ExitGuiAfterLayoutMutation()
	{
		GUIUtility.ExitGUI();
	}

	private void OpenUi()
	{
		_stylesInitialized = false;
		_appliedStylePreset = string.Empty;
		_uiOpen = true;
		_prevLockMode = Cursor.lockState;
		_prevCursorVisible = Cursor.visible;
		BeginUiManagerSuppression();
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		TogglePlayerControls(enable: false);
		SetUnityUiVisible(true);
		if (UseUnityUi)
		{
			if (_activePanel == ActivePanel.ToggleMenu)
			{
				SetUnityGroupConfigOpen(true);
			}
			else
			{
				SetUnityGroupConfigOpen(false);
				SetUnityConfigOpen(false);
			}
		}
	}

	private void CloseUi()
	{
		_uiOpen = false;
		_outputPickerSlot = -1;
		_keybindCapture.Cancel();
		EndUiManagerSuppression();
		Cursor.lockState = _prevLockMode;
		Cursor.visible = _prevCursorVisible;
		TogglePlayerControls(enable: true);
		SetUnityUiVisible(false);
	}

	private void BeginUiManagerSuppression()
	{
		UiControlUtils.BeginUiManagerSuppression(ref _suppressedUiManagerBehaviour, ref _suppressedUiManagerWasEnabled);
	}

	private void EndUiManagerSuppression()
	{
		UiControlUtils.EndUiManagerSuppression(ref _suppressedUiManagerBehaviour, ref _suppressedUiManagerWasEnabled);
	}

	private void TogglePlayerControls(bool enable)
	{
		UiControlUtils.TogglePlayerControls(enable, _disabledScripts);
	}

	private void EnsureCursor()
	{
		UiControlUtils.EnsureCursorUnlocked();
	}
}

