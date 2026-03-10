using UnityEngine;

namespace GroupedMiners;
public sealed partial class GroupedMiners
{
	private void SetPresetCount(int next)
	{
		_presetCount = Mathf.Clamp(next, MinPresetCount, MaxPresetCount);
		while (_presets.Count < _presetCount)
		{
			var index = _presets.Count;
			_presets.Add(new PresetEntry
			{
				name = BuildDefaultPresetName(index)
			});
		}
		if (_presets.Count > _presetCount)
		{
			_presets.RemoveRange(_presetCount, _presets.Count - _presetCount);
		}
	}

	private int GetPresetVisibleCount()
	{
		if (_presetCount < MinPresetCount) _presetCount = DefaultPresetCount;
		_presetCount = Mathf.Clamp(_presetCount, MinPresetCount, MaxPresetCount);
		if (_presets.Count != _presetCount)
		{
			SetPresetCount(_presetCount);
		}
		return _presetCount;
	}

	private static string BuildDefaultPresetName(int index)
	{
		return $"Preset {Mathf.Max(0, index) + 1}";
	}

	private string GetPresetName(int index)
	{
		if (index < 0 || index >= GetPresetVisibleCount()) return BuildDefaultPresetName(index);
		var name = _presets[index].name;
		return string.IsNullOrWhiteSpace(name) ? BuildDefaultPresetName(index) : name.Trim();
	}

	private void SetPresetName(int index, string value)
	{
		if (index < 0 || index >= GetPresetVisibleCount()) return;
		var next = string.IsNullOrWhiteSpace(value) ? BuildDefaultPresetName(index) : value.Trim();
		if (string.Equals(_presets[index].name, next, System.StringComparison.Ordinal)) return;
		_presets[index].name = next;
		SavePresetsToDisk();
	}
}
