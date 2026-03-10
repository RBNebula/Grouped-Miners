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
	private string GetGroupDisplayName(int group)
	{
		if (group <= 0) return "None";
		EnsureGroupNamesInitialized();
		var index = Mathf.Clamp(group - 1, 0, Mathf.Max(0, _groupNames.Count - 1));
		if (_groupNames.Count == 0) return BuildDefaultGroupName(group);
		return _groupNames[index];
	}

	private string GetGroupGuid(int group)
	{
		if (group <= 0) return string.Empty;
		EnsureGroupNamesInitialized();
		var index = group - 1;
		if (index < 0 || index >= _groupGuids.Count) return string.Empty;
		return _groupGuids[index];
	}

	private void EnsureGroupNamesInitialized()
	{
		if (_groupCount < MinGroupCount) _groupCount = DefaultGroupCount;
		_groupCount = Mathf.Clamp(_groupCount, MinGroupCount, MaxGroupCount);
		while (_groupNames.Count < _groupCount)
		{
			_groupNames.Add(BuildDefaultGroupName(_groupNames.Count + 1));
		}
		if (_groupNames.Count > _groupCount)
		{
			_groupNames.RemoveRange(_groupCount, _groupNames.Count - _groupCount);
		}
		while (_groupGuids.Count < _groupCount)
		{
			_groupGuids.Add(BuildNewGroupGuid());
		}
		if (_groupGuids.Count > _groupCount)
		{
			_groupGuids.RemoveRange(_groupCount, _groupGuids.Count - _groupCount);
		}
		for (var i = 0; i < _groupNames.Count; i++)
		{
			if (string.IsNullOrWhiteSpace(_groupNames[i]))
			{
				_groupNames[i] = BuildDefaultGroupName(i + 1);
			}
			else
			{
				_groupNames[i] = _groupNames[i].Trim();
			}
		}
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (var i = 0; i < _groupGuids.Count; i++)
		{
			var guid = (_groupGuids[i] ?? string.Empty).Trim();
			if (string.IsNullOrWhiteSpace(guid) || !seen.Add(guid))
			{
				guid = BuildNewGroupGuid();
				seen.Add(guid);
			}
			_groupGuids[i] = guid;
		}
		SyncLegacyGroupNameFields();
	}

	private void SetGroupCount(int next)
	{
		_groupCount = Mathf.Clamp(next, MinGroupCount, MaxGroupCount);
		EnsureGroupNamesInitialized();
		_edit.group = Mathf.Clamp(_edit.group, 0, _groupCount);
	}

	private static string BuildDefaultGroupName(int groupIndex)
	{
		if (groupIndex <= 0) return "Group";
		var alphabetIndex = groupIndex - 1;
		if (alphabetIndex >= 0 && alphabetIndex < 26)
		{
			var suffix = (char)('A' + alphabetIndex);
			return $"Group {suffix}";
		}
		return $"Group {groupIndex}";
	}

	private static string BuildNewGroupGuid()
	{
		return Guid.NewGuid().ToString("N");
	}

	private bool TryResolveGroupGuidToId(string groupGuid, out int groupId)
	{
		groupId = 0;
		EnsureGroupNamesInitialized();
		if (string.IsNullOrWhiteSpace(groupGuid)) return false;
		var target = groupGuid.Trim();
		for (var i = 0; i < _groupGuids.Count; i++)
		{
			if (!string.Equals(_groupGuids[i], target, StringComparison.OrdinalIgnoreCase)) continue;
			groupId = i + 1;
			return true;
		}
		return false;
	}

	private void SyncLegacyGroupNameFields()
	{
		_groupNameA = _groupNames.Count >= 1 ? _groupNames[0] : DefaultGroupNameA;
		_groupNameB = _groupNames.Count >= 2 ? _groupNames[1] : DefaultGroupNameB;
		_groupNameC = _groupNames.Count >= 3 ? _groupNames[2] : DefaultGroupNameC;
		_groupNameD = _groupNames.Count >= 4 ? _groupNames[3] : DefaultGroupNameD;
	}

	private string[] GetConfiguredGroupNamesSnapshot()
	{
		EnsureGroupNamesInitialized();
		return _groupNames.Take(_groupCount).ToArray();
	}

	private string[] GetConfiguredGroupGuidsSnapshot()
	{
		EnsureGroupNamesInitialized();
		return _groupGuids.Take(_groupCount).ToArray();
	}

	private bool IsValidGroupValue(int group)
	{
		return group >= 0 && group <= _groupCount;
	}

	private static int RemapGroupAfterRemoval(int value, int removedGroup)
	{
		if (value == removedGroup) return 0;
		if (value > removedGroup) return value - 1;
		return value;
	}

	private bool TryAddGroupInternal(bool showToast)
	{
		EnsureGroupNamesInitialized();
		if (_groupCount >= MaxGroupCount)
		{
			return false;
		}

		_groupCount++;
		_groupNames.Add(BuildDefaultGroupName(_groupCount));
		SyncLegacyGroupNameFields();
		SaveSettingsToDisk();
		return true;
	}

	private bool TryRemoveGroupInternal(int group, bool showToast)
	{
		EnsureGroupNamesInitialized();
		if (group <= 0 || group > _groupCount) return false;
		if (_groupCount <= MinGroupCount)
		{
			return false;
		}

		var removedName = GetGroupDisplayName(group);
		_groupNames.RemoveAt(group - 1);
		_groupGuids.RemoveAt(group - 1);
		_groupCount = Mathf.Clamp(_groupCount - 1, MinGroupCount, MaxGroupCount);

		_edit.group = Mathf.Clamp(RemapGroupAfterRemoval(_edit.group, group), 0, _groupCount);
		if (_clipboardConfig != null)
		{
			_clipboardConfig.group = Mathf.Clamp(RemapGroupAfterRemoval(_clipboardConfig.group, group), 0, _groupCount);
		}

		for (var i = 0; i < _savedEntries.Count; i++)
		{
			_savedEntries[i].group = Mathf.Clamp(RemapGroupAfterRemoval(_savedEntries[i].group, group), 0, _groupCount);
		}
		for (var i = 0; i < _presets.Count; i++)
		{
			_presets[i].group = Mathf.Clamp(RemapGroupAfterRemoval(_presets[i].group, group), 0, _groupCount);
		}

		foreach (var kv in _minerCache)
		{
			kv.Value.config.group = Mathf.Clamp(RemapGroupAfterRemoval(kv.Value.config.group, group), 0, _groupCount);
			if (kv.Value.miner is MonoBehaviour mb)
			{
				var cfg = ReadConfigForMiner(mb);
				if (cfg != null)
				{
					cfg.group = Mathf.Clamp(RemapGroupAfterRemoval(cfg.group, group), 0, _groupCount);
				}
			}
		}

		if (_groupApiValues.Count > 0)
		{
			var remapped = new Dictionary<int, Dictionary<string, string>>();
			foreach (var kv in _groupApiValues)
			{
				if (kv.Key == group) continue;
				var nextKey = kv.Key > group ? kv.Key - 1 : kv.Key;
				remapped[nextKey] = kv.Value;
			}
			_groupApiValues.Clear();
			foreach (var kv in remapped)
			{
				_groupApiValues[kv.Key] = kv.Value;
			}
		}

		SyncLegacyGroupNameFields();
		SaveSettingsToDisk();
		SaveSaveDataToDisk();
		SavePresetsToDisk();
		return true;
	}

	private bool TryAssignMinerToGroupInternal(object miner, int group, bool showToast)
	{
		if (!IsObjectAlive(miner)) return false;
		EnsureGroupNamesInitialized();
		if (!IsValidGroupValue(group)) return false;

		var pos = ReadMinerPosition(miner);
		var existing = FindSavedEntryForPosition(pos);
		if (existing == null)
		{
			existing = BuildDefaultConfigFromMiner(miner);
			existing.x = pos.x;
			existing.y = pos.y;
			existing.z = pos.z;
			_savedEntries.Add(existing);
		}
		else
		{
			existing.x = pos.x;
			existing.y = pos.y;
			existing.z = pos.z;
		}

		existing.group = group;
		if (miner is MonoBehaviour mb)
		{
			SetStateConfig(mb, existing);
			var id = mb.GetInstanceID();
			if (_minerCache.TryGetValue(id, out var runtime))
			{
				runtime.hasCustomConfig = true;
				runtime.config = CloneConfig(existing);
				runtime.position = mb.transform.position;
			}
		}

		SaveSaveDataToDisk();
		if (showToast)
		{
			var name = group <= 0 ? "None" : GetGroupDisplayName(group);
		}

		return true;
	}

	private bool TryAssignMinerAtPositionToGroupInternal(Vector3 position, int group, float maxDistance)
	{
		EnsureGroupNamesInitialized();
		if (!IsValidGroupValue(group)) return false;
		if (_minerCache.Count == 0) RefreshMinerCache();

		var bestDistance = float.MaxValue;
		object? bestMiner = null;
		foreach (var kv in _minerCache)
		{
			var distance = Vector3.Distance(kv.Value.position, position);
			if (distance > maxDistance) continue;
			if (distance >= bestDistance) continue;
			bestDistance = distance;
			bestMiner = kv.Value.miner;
		}

		return bestMiner != null && TryAssignMinerToGroupInternal(bestMiner, group, showToast: false);
	}

	private void ToggleAllMiners(bool enabled)
	{
		var count = 0;
		foreach (var kv in _minerCache)
		{
			if (!TryToggleMinerEnabled(kv.Value.miner, enabled)) continue;
			count++;
		}
	}

	private void ToggleGroupMiners(int group, bool enabled)
	{
		var count = 0;
		foreach (var kv in _minerCache)
		{
			MinerConfigEntry? cfg = null;
			if (kv.Value.miner is MonoBehaviour mb)
			{
				cfg = ReadConfigForMiner(mb);
			}
			cfg ??= FindSavedEntryForPosition(kv.Value.position);
			if (cfg == null) continue;
			if (cfg.group != group) continue;
			if (!TryToggleMinerEnabled(kv.Value.miner, enabled)) continue;
			count++;
		}
	}

	private bool TryToggleMinerEnabled(object miner, bool enabled)
	{
		if (!IsObjectAlive(miner)) return false;
		var type = miner.GetType();
		var toggleMethod = type.GetMethod("Toggle", AnyInstance, null, new[] { typeof(bool) }, null);
		if (toggleMethod != null)
		{
			toggleMethod.Invoke(miner, new object[] { enabled });
			return true;
		}
		var turnOn = type.GetMethod("TurnOn", AnyInstance);
		var turnOff = type.GetMethod("TurnOff", AnyInstance);
		if (enabled && turnOn != null)
		{
			turnOn.Invoke(miner, null);
			return true;
		}
		if (!enabled && turnOff != null)
		{
			turnOff.Invoke(miner, null);
			return true;
		}
		var enabledField = type.GetField("Enabled", AnyInstance);
		if (enabledField != null && enabledField.FieldType == typeof(bool))
		{
			enabledField.SetValue(miner, enabled);
			return true;
		}
		return false;
	}
}

