using System;
using System.Collections.Generic;
using UnityEngine;

namespace GroupedMiners;

public readonly struct GroupedMinerGroup
{
	public GroupedMinerGroup(int id, string groupGuid, string name, int minerCount, bool isEnabled)
	{
		Id = id;
		GroupGuid = groupGuid ?? string.Empty;
		Name = name ?? string.Empty;
		MinerCount = Mathf.Max(0, minerCount);
		IsEnabled = isEnabled;
	}

	public int Id { get; }
	public string GroupGuid { get; }
	public string Name { get; }
	public int MinerCount { get; }
	public bool IsEnabled { get; }
}

public interface IGroupedMinersApi
{
	IReadOnlyList<GroupedMinerGroup> GetGroups();
	bool TryGetGroupById(int groupId, out GroupedMinerGroup group);
	bool TryGetGroupByGuid(string groupGuid, out GroupedMinerGroup group);
	bool TryGetGroupIdByGuid(string groupGuid, out int groupId);
	bool TryGetGroupGuidById(int groupId, out string groupGuid);

	IReadOnlyList<MonoBehaviour> GetMinersInGroup(int groupId);
	IReadOnlyList<MonoBehaviour> GetMinersInGroupByGuid(string groupGuid);
	bool TryGetTargetMiner(out MonoBehaviour miner);
	bool TryGetTargetMinerGroup(out int groupId);
	bool TryGetTargetMinerGroupGuid(out string groupGuid);
	bool TryGetMinerGroup(MonoBehaviour miner, out int groupId);
	bool TryGetMinerGroupGuid(MonoBehaviour miner, out string groupGuid);
	bool TryAssignMinerToGroup(MonoBehaviour miner, int groupId);
	bool TryAssignMinerToGroupGuid(MonoBehaviour miner, string groupGuid);
	bool TryAssignTargetMinerToGroup(int groupId);
	bool TryAssignTargetMinerToGroupGuid(string groupGuid);
	bool TryUngroupMiner(MonoBehaviour miner);
	bool TryUngroupTargetMiner();

	bool SetGroupName(int groupId, string name);
	bool SetGroupNameByGuid(string groupGuid, string name);
	bool SetGroupEnabled(int groupId, bool enabled);
	bool SetGroupEnabledByGuid(string groupGuid, bool enabled);
	bool SetAllGroupsEnabled(bool enabled);

	bool TryAssignMinerPositionToGroup(Vector3 position, int groupId, float maxDistance = 0.12f);
	bool TryAssignMinerPositionToGroupByGuid(Vector3 position, string groupGuid, float maxDistance = 0.12f);

	void SetGroupValue(int groupId, string key, string value);
	bool SetGroupValueByGuid(string groupGuid, string key, string value);
	bool TryGetGroupValue(int groupId, string key, out string value);
	bool TryGetGroupValueByGuid(string groupGuid, string key, out string value);
	bool RemoveGroupValue(int groupId, string key);
	bool RemoveGroupValueByGuid(string groupGuid, string key);
}

public sealed partial class GroupedMiners
{
	public static bool TryGetApi(out IGroupedMinersApi? api)
	{
		api = Instance;
		return api != null;
	}

	public IReadOnlyList<GroupedMinerGroup> GetGroups()
	{
		EnsureGroupNamesInitialized();
		var groups = new List<GroupedMinerGroup>(_groupCount);
		for (var group = 1; group <= _groupCount; group++)
		{
			groups.Add(BuildGroupInfo(group));
		}
		return groups;
	}

	public bool TryGetGroupById(int groupId, out GroupedMinerGroup group)
	{
		group = default;
		EnsureGroupNamesInitialized();
		if (groupId <= 0 || groupId > _groupCount) return false;
		group = BuildGroupInfo(groupId);
		return true;
	}

	public bool TryGetGroupByGuid(string groupGuid, out GroupedMinerGroup group)
	{
		group = default;
		if (!TryResolveGroupGuidToId(groupGuid, out var groupId)) return false;
		return TryGetGroupById(groupId, out group);
	}

	public bool TryGetGroupIdByGuid(string groupGuid, out int groupId)
	{
		return TryResolveGroupGuidToId(groupGuid, out groupId);
	}

	public bool TryGetGroupGuidById(int groupId, out string groupGuid)
	{
		groupGuid = string.Empty;
		EnsureGroupNamesInitialized();
		if (groupId <= 0 || groupId > _groupCount) return false;
		groupGuid = GetGroupGuid(groupId);
		return true;
	}

	public IReadOnlyList<MonoBehaviour> GetMinersInGroup(int groupId)
	{
		EnsureGroupNamesInitialized();
		var miners = new List<MonoBehaviour>();
		if (groupId <= 0 || groupId > _groupCount) return miners;

		foreach (var kv in _minerCache)
		{
			if (kv.Value.miner is not MonoBehaviour mb || mb == null) continue;
			var cfg = ReadConfigForMiner(mb) ?? FindSavedEntryForPosition(mb.transform.position);
			if (cfg == null || cfg.group != groupId) continue;
			miners.Add(mb);
		}

		return miners;
	}

	public IReadOnlyList<MonoBehaviour> GetMinersInGroupByGuid(string groupGuid)
	{
		if (!TryResolveGroupGuidToId(groupGuid, out var groupId))
		{
			return new List<MonoBehaviour>();
		}
		return GetMinersInGroup(groupId);
	}

	public bool TryGetTargetMiner(out MonoBehaviour miner)
	{
		miner = null!;
		if (!TrySelectMinerForInteraction(out var selectedMiner)) return false;
		if (selectedMiner is not MonoBehaviour mb || mb == null) return false;
		if (!IsAutoMinerType(mb.GetType())) return false;
		miner = mb;
		return true;
	}

	public bool TryGetTargetMinerGroup(out int groupId)
	{
		groupId = 0;
		if (!TryGetTargetMiner(out var miner)) return false;
		return TryGetMinerGroup(miner, out groupId);
	}

	public bool TryGetTargetMinerGroupGuid(out string groupGuid)
	{
		groupGuid = string.Empty;
		if (!TryGetTargetMiner(out var miner)) return false;
		return TryGetMinerGroupGuid(miner, out groupGuid);
	}

	public bool TryGetMinerGroup(MonoBehaviour miner, out int groupId)
	{
		groupId = 0;
		EnsureGroupNamesInitialized();
		if (miner == null || !IsAutoMinerType(miner.GetType())) return false;

		var cfg = ReadConfigForMiner(miner) ?? FindSavedEntryForPosition(miner.transform.position);
		groupId = Mathf.Clamp(cfg?.group ?? 0, 0, _groupCount);
		return true;
	}

	public bool TryGetMinerGroupGuid(MonoBehaviour miner, out string groupGuid)
	{
		groupGuid = string.Empty;
		if (!TryGetMinerGroup(miner, out var groupId)) return false;
		if (groupId <= 0) return true;
		return TryGetGroupGuidById(groupId, out groupGuid);
	}

	public bool TryAssignMinerToGroup(MonoBehaviour miner, int groupId)
	{
		EnsureGroupNamesInitialized();
		if (groupId < 0 || groupId > _groupCount) return false;
		return TryAssignMinerToGroupInternal(miner, groupId, showToast: false);
	}

	public bool TryAssignMinerToGroupGuid(MonoBehaviour miner, string groupGuid)
	{
		EnsureGroupNamesInitialized();
		if (miner == null || !IsAutoMinerType(miner.GetType())) return false;
		if (!TryResolveGroupGuidToId(groupGuid, out var groupId)) return false;
		return TryAssignMinerToGroup(miner, groupId);
	}

	public bool TryAssignTargetMinerToGroup(int groupId)
	{
		EnsureGroupNamesInitialized();
		if (groupId < 0 || groupId > _groupCount) return false;
		if (!TryGetTargetMiner(out var miner)) return false;
		return TryAssignMinerToGroup(miner, groupId);
	}

	public bool TryAssignTargetMinerToGroupGuid(string groupGuid)
	{
		EnsureGroupNamesInitialized();
		if (!TryResolveGroupGuidToId(groupGuid, out var groupId)) return false;
		return TryAssignTargetMinerToGroup(groupId);
	}

	public bool TryUngroupMiner(MonoBehaviour miner)
	{
		return TryAssignMinerToGroup(miner, 0);
	}

	public bool TryUngroupTargetMiner()
	{
		return TryAssignTargetMinerToGroup(0);
	}

	public bool SetGroupName(int groupId, string name)
	{
		EnsureGroupNamesInitialized();
		if (groupId <= 0 || groupId > _groupCount) return false;

		var nextName = string.IsNullOrWhiteSpace(name) ? BuildDefaultGroupName(groupId) : name.Trim();
		var index = groupId - 1;
		if (string.Equals(_groupNames[index], nextName, StringComparison.Ordinal)) return true;

		_groupNames[index] = nextName;
		SyncLegacyGroupNameFields();
		SaveSettingsToDisk();
		return true;
	}

	public bool SetGroupNameByGuid(string groupGuid, string name)
	{
		if (!TryResolveGroupGuidToId(groupGuid, out var groupId)) return false;
		return SetGroupName(groupId, name);
	}

	public bool SetGroupEnabled(int groupId, bool enabled)
	{
		EnsureGroupNamesInitialized();
		if (groupId <= 0 || groupId > _groupCount) return false;
		ToggleGroupMiners(groupId, enabled);
		return true;
	}

	public bool SetGroupEnabledByGuid(string groupGuid, bool enabled)
	{
		if (!TryResolveGroupGuidToId(groupGuid, out var groupId)) return false;
		return SetGroupEnabled(groupId, enabled);
	}

	public bool SetAllGroupsEnabled(bool enabled)
	{
		ToggleAllMiners(enabled);
		return true;
	}

	public bool TryAssignMinerPositionToGroup(Vector3 position, int groupId, float maxDistance = 0.12f)
	{
		EnsureGroupNamesInitialized();
		if (groupId < 0 || groupId > _groupCount) return false;
		var effectiveDistance = maxDistance <= 0f ? _positionMatchEpsilon : maxDistance;
		return TryAssignMinerAtPositionToGroupInternal(position, groupId, effectiveDistance);
	}

	public bool TryAssignMinerPositionToGroupByGuid(Vector3 position, string groupGuid, float maxDistance = 0.12f)
	{
		if (!TryResolveGroupGuidToId(groupGuid, out var groupId)) return false;
		return TryAssignMinerPositionToGroup(position, groupId, maxDistance);
	}

	public void SetGroupValue(int groupId, string key, string value)
	{
		EnsureGroupNamesInitialized();
		if (groupId <= 0 || groupId > _groupCount) return;
		if (string.IsNullOrWhiteSpace(key)) return;

		if (!_groupApiValues.TryGetValue(groupId, out var groupValues))
		{
			groupValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			_groupApiValues[groupId] = groupValues;
		}

		groupValues[key.Trim()] = value ?? string.Empty;
	}

	public bool SetGroupValueByGuid(string groupGuid, string key, string value)
	{
		if (!TryResolveGroupGuidToId(groupGuid, out var groupId)) return false;
		SetGroupValue(groupId, key, value);
		return true;
	}

	public bool TryGetGroupValue(int groupId, string key, out string value)
	{
		value = string.Empty;
		if (groupId <= 0) return false;
		if (string.IsNullOrWhiteSpace(key)) return false;
		if (!_groupApiValues.TryGetValue(groupId, out var groupValues)) return false;
		if (!groupValues.TryGetValue(key.Trim(), out value!)) return false;
		return true;
	}

	public bool TryGetGroupValueByGuid(string groupGuid, string key, out string value)
	{
		value = string.Empty;
		if (!TryResolveGroupGuidToId(groupGuid, out var groupId)) return false;
		return TryGetGroupValue(groupId, key, out value);
	}

	public bool RemoveGroupValue(int groupId, string key)
	{
		if (groupId <= 0) return false;
		if (string.IsNullOrWhiteSpace(key)) return false;
		if (!_groupApiValues.TryGetValue(groupId, out var groupValues)) return false;
		return groupValues.Remove(key.Trim());
	}

	public bool RemoveGroupValueByGuid(string groupGuid, string key)
	{
		if (!TryResolveGroupGuidToId(groupGuid, out var groupId)) return false;
		return RemoveGroupValue(groupId, key);
	}

	private GroupedMinerGroup BuildGroupInfo(int groupId)
	{
		return new GroupedMinerGroup(
			groupId,
			GetGroupGuid(groupId),
			GetGroupDisplayName(groupId),
			CountGroupMiners(groupId),
			IsGroupEnabled(groupId));
	}

	private int CountGroupMiners(int groupId)
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
			if (cfg.group != groupId) continue;
			count++;
		}
		return count;
	}
}
