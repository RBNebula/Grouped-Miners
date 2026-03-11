# Grouped Miners

Shared miner grouping mod for MineMogul, with a public API intended for low-boilerplate host-mod integration.

## API Access

Namespace: `GroupedMiners`  
Type: `IGroupedMinersApi`  
Host accessor: `GroupedMiners.GroupedMiners.TryGetApi(out IGroupedMinersApi? api)`

```csharp
using GroupedMiners;
using GroupedMinersHost = GroupedMiners.GroupedMiners;

if (!GroupedMinersHost.TryGetApi(out var api) || api == null)
{
    return; // Host mod not loaded/ready.
}
```

## Contract Guarantees

- Group `Id` is for UI ordering only.
- Group `GroupGuid` is the persistence-safe identifier for cross-mod references.
- IDs may be reassigned after group removal to keep UI ordering contiguous.
- GUIDs are generated once per group and are not reused after group deletion.
- `groupId == 0` means ungrouped.
- Miner lookup lists are snapshots (new list per call), not live views.
- API calls are expected on the Unity main thread.

## Enabled Semantics

- Group enabled state represents miner operational state for that group.
- Setting a group enabled/disabled toggles current miners in that group.
- New or reassigned miners follow the current enabled state of their assigned group.

## Group Deletion Semantics

- Removing a group ungroups miners in that group (`groupId = 0`).
- Higher group IDs shift down by one to remain contiguous.
- Metadata stored for the removed group is deleted.
- Metadata for shifted groups follows the shifted ID.

## Miner Identity

Returned `MonoBehaviour` miner instances are the miner components recognized by Grouped Miners.  
They are safe for `transform`/`name` access and for passing back into Grouped Miners API methods.

## Metadata Rules

- Use namespaced keys: `my_mod.someSetting`.
- Do not overwrite keys owned by other mods.
- Metadata values are string-only (serialize structured data yourself).
- Group metadata is runtime-only and is not persisted to disk.

## API Surface

```csharp
readonly struct GroupedMinerGroup
{
    int Id { get; }
    string GroupGuid { get; }
    string Name { get; }
    int MinerCount { get; }
    bool IsEnabled { get; }
}

interface IGroupedMinersApi
{
    // Group discovery / lookup
    IReadOnlyList<GroupedMinerGroup> GetGroups();
    bool TryGetGroupById(int groupId, out GroupedMinerGroup group);
    bool TryGetGroupByGuid(string groupGuid, out GroupedMinerGroup group);
    bool TryGetGroupIdByGuid(string groupGuid, out int groupId);
    bool TryGetGroupGuidById(int groupId, out string groupGuid);

    // Miner targeting / membership lookup
    IReadOnlyList<MonoBehaviour> GetMinersInGroup(int groupId);
    IReadOnlyList<MonoBehaviour> GetMinersInGroupByGuid(string groupGuid);
    bool TryGetTargetMiner(out MonoBehaviour miner);
    bool TryGetTargetMinerGroup(out int groupId);
    bool TryGetTargetMinerGroupGuid(out string groupGuid);
    bool TryGetMinerGroup(MonoBehaviour miner, out int groupId);
    bool TryGetMinerGroupGuid(MonoBehaviour miner, out string groupGuid);

    // Assignment
    bool TryAssignMinerToGroup(MonoBehaviour miner, int groupId);
    bool TryAssignMinerToGroupGuid(MonoBehaviour miner, string groupGuid);
    bool TryAssignTargetMinerToGroup(int groupId);
    bool TryAssignTargetMinerToGroupGuid(string groupGuid);
    bool TryUngroupMiner(MonoBehaviour miner);
    bool TryUngroupTargetMiner();
    bool TryAssignMinerPositionToGroup(Vector3 position, int groupId, float maxDistance = 0.12f);
    bool TryAssignMinerPositionToGroupByGuid(Vector3 position, string groupGuid, float maxDistance = 0.12f);

    // Group properties
    bool SetGroupName(int groupId, string name);
    bool SetGroupNameByGuid(string groupGuid, string name);
    bool SetGroupEnabled(int groupId, bool enabled);
    bool SetGroupEnabledByGuid(string groupGuid, bool enabled);
    bool SetAllGroupsEnabled(bool enabled);

    // Metadata
    void SetGroupValue(int groupId, string key, string value);
    bool SetGroupValueByGuid(string groupGuid, string key, string value);
    bool TryGetGroupValue(int groupId, string key, out string value);
    bool TryGetGroupValueByGuid(string groupGuid, string key, out string value);
    bool RemoveGroupValue(int groupId, string key);
    bool RemoveGroupValueByGuid(string groupGuid, string key);
}
```

## Usage Examples

### Group discovery / lookup

```csharp
var groups = api.GetGroups();

foreach (var g in groups)
{
    Logger.LogInfo($"[{g.Id}] {g.Name} guid={g.GroupGuid} miners={g.MinerCount} enabled={g.IsEnabled}");
}

if (api.TryGetGroupById(2, out var byId))
{
    Logger.LogInfo($"ID 2 => {byId.Name} ({byId.GroupGuid}) enabled={byId.IsEnabled}");
}
```

### Miner targeting / membership lookup

```csharp
if (api.TryGetTargetMiner(out var targetMiner))
{
    Logger.LogInfo($"Target miner: {targetMiner.name}");
}

if (api.TryGetTargetMinerGroup(out var targetGroupId))
{
    Logger.LogInfo($"Target group ID: {targetGroupId}");
}

if (api.TryGetTargetMinerGroupGuid(out var targetGroupGuid))
{
    Logger.LogInfo($"Target group GUID: {targetGroupGuid}");
}

foreach (var miner in api.GetMinersInGroup(2))
{
    var p = miner.transform.position;
    Logger.LogInfo($"Group 2 miner at {p.x:0.##}, {p.y:0.##}, {p.z:0.##}");
}
```

### Assignment

```csharp
// Assign by ID
if (api.TryGetTargetMiner(out var miner))
{
    api.TryAssignMinerToGroup(miner, 2);
}

// Assign by GUID
if (api.TryGetTargetMiner(out var miner2) &&
    api.TryGetGroupGuidById(3, out var g3))
{
    api.TryAssignMinerToGroupGuid(miner2, g3);
}

// Assign targeted miner
api.TryAssignTargetMinerToGroup(1);

if (api.TryGetGroupGuidById(4, out var g4))
{
    api.TryAssignTargetMinerToGroupGuid(g4);
}

// Ungroup
if (api.TryGetTargetMiner(out var miner3))
{
    api.TryUngroupMiner(miner3);
}
api.TryUngroupTargetMiner();

// Assign nearest miner to world position
api.TryAssignMinerPositionToGroup(new Vector3(10f, 1f, -5f), 2, 0.2f);
```

### Group properties

```csharp
api.SetGroupName(2, "Smelter Line");
api.SetGroupEnabled(2, false);
api.SetAllGroupsEnabled(true);
```

### Metadata

```csharp
api.SetGroupValue(2, "my_mod.mode", "aggressive");

if (api.TryGetGroupValue(2, "my_mod.mode", out var mode))
{
    Logger.LogInfo($"Mode = {mode}");
}

api.RemoveGroupValue(2, "my_mod.mode");
```

