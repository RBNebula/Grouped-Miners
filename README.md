# Grouped Miners

Shared miner grouping mod for MineMogul, with a public API designed for low-boilerplate integration.

## Behavior

- Players can assign miners to groups.
- Groups can be renamed and toggled on/off.
- Each group has:
  - numeric `Id` (`1..N`) for UI ordering
  - stable `GroupGuid` for cross-mod references

## Get API

Namespace: `GroupedMiners`  
Type: `IGroupedMinersApi`  
Host accessor: `GroupedMiners.GroupedMiners.TryGetApi(out IGroupedMinersApi? api)`

```csharp
using GroupedMiners;
using GroupedMinersHost = GroupedMiners.GroupedMiners;

if (!GroupedMinersHost.TryGetApi(out var api) || api == null)
{
    // Host mod not ready/loaded.
    return;
}
```

## API Surface

```csharp
readonly struct GroupedMinerGroup
{
    int Id { get; }
    string GroupGuid { get; }
    string Name { get; }
    int MinerCount { get; }
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
    int GetTargetMinersGroup();
    bool TryGetTargetMinerGroupGuid(out string groupGuid);
    bool TryGetMinerGroup(MonoBehaviour miner, out int groupId);
    bool TryGetMinerGroupGuid(MonoBehaviour miner, out string groupGuid);

    // Group assignment
    bool ChangeMinerGroup(MonoBehaviour miner, string groupGuid);
    bool ChangeTargetMinerGroup(string groupGuid);
    bool TryAssignMinerToGroup(MonoBehaviour miner, int groupId);
    bool TryAssignMinerPositionToGroup(Vector3 position, int groupId, float maxDistance = 0.12f);
    bool TryAssignMinerPositionToGroupByGuid(Vector3 position, string groupGuid, float maxDistance = 0.12f);

    // Group naming / toggles
    bool SetGroupName(int groupId, string name);
    bool SetGroupNameByGuid(string groupGuid, string name);
    bool SetGroupEnabled(int groupId, bool enabled);
    bool SetGroupEnabledByGuid(string groupGuid, bool enabled);
    bool SetAllGroupsEnabled(bool enabled);

    // Group metadata for other mods
    void SetGroupValue(int groupId, string key, string value);
    bool SetGroupValueByGuid(string groupGuid, string key, string value);
    bool TryGetGroupValue(int groupId, string key, out string value);
    bool TryGetGroupValueByGuid(string groupGuid, string key, out string value);
    bool RemoveGroupValue(int groupId, string key);
    bool RemoveGroupValueByGuid(string groupGuid, string key);
}
```

## Usage Examples (Every API)

### Group discovery / lookup

```csharp
var groups = api.GetGroups();

if (api.TryGetGroupById(2, out var byId))
{
    Logger.LogInfo($"ID 2 => {byId.Name} [{byId.GroupGuid}]");
}

if (groups.Count > 0 && api.TryGetGroupByGuid(groups[0].GroupGuid, out var byGuid))
{
    Logger.LogInfo($"GUID => {byGuid.Id}:{byGuid.Name}");
}

if (groups.Count > 0 && api.TryGetGroupIdByGuid(groups[0].GroupGuid, out var gid))
{
    Logger.LogInfo($"Resolved ID = {gid}");
}

if (api.TryGetGroupGuidById(1, out var guid1))
{
    Logger.LogInfo($"Group 1 GUID = {guid1}");
}
```

### Miner targeting / membership lookup

```csharp
// By ID
foreach (var miner in api.GetMinersInGroup(2))
{
    var p = miner.transform.position;
    Logger.LogInfo($"Group 2 miner at {p.x:0.##}, {p.y:0.##}, {p.z:0.##}");
}

// By GUID
if (api.TryGetGroupGuidById(2, out var group2Guid))
{
    foreach (var miner in api.GetMinersInGroupByGuid(group2Guid))
    {
        Logger.LogInfo($"GUID group miner: {miner.name}");
    }
}

// Target miner object
if (api.TryGetTargetMiner(out var targetMiner))
{
    Logger.LogInfo($"Target miner: {targetMiner.name}");
}

// Target group as ID
var targetGroupId = api.GetTargetMinersGroup(); // 0 means none/ungrouped/no target

// Target group as GUID
if (api.TryGetTargetMinerGroupGuid(out var targetGroupGuid))
{
    Logger.LogInfo($"Target group GUID: {targetGroupGuid}");
}

// Any miner -> group ID/GUID
if (api.TryGetTargetMiner(out var m))
{
    api.TryGetMinerGroup(m, out var mGroupId);
    api.TryGetMinerGroupGuid(m, out var mGroupGuid);
    Logger.LogInfo($"Miner group: id={mGroupId}, guid={mGroupGuid}");
}
```

### Group assignment

```csharp
// Assign specific miner by GUID
if (api.TryGetTargetMiner(out var miner) && api.TryGetGroupGuidById(1, out var g1))
{
    api.ChangeMinerGroup(miner, g1);
}

// Assign currently targeted miner by GUID
if (api.TryGetGroupGuidById(3, out var g3))
{
    api.ChangeTargetMinerGroup(g3);
}

// Assign specific miner by ID
if (api.TryGetTargetMiner(out var miner2))
{
    api.TryAssignMinerToGroup(miner2, 2);
}

// Assign nearest miner to a world position by ID
api.TryAssignMinerPositionToGroup(new Vector3(10f, 1f, -5f), 2, 0.2f);

// Assign nearest miner to a world position by GUID
if (api.TryGetGroupGuidById(2, out var g2))
{
    api.TryAssignMinerPositionToGroupByGuid(new Vector3(10f, 1f, -5f), g2, 0.2f);
}
```

### Group naming / toggles

```csharp
// Rename by ID
api.SetGroupName(2, "Smelter Line");

// Rename by GUID
if (api.TryGetGroupGuidById(2, out var g2))
{
    api.SetGroupNameByGuid(g2, "Smelter Line");
}

// Toggle by ID
api.SetGroupEnabled(2, true);

// Toggle by GUID
if (api.TryGetGroupGuidById(2, out var groupGuid))
{
    api.SetGroupEnabledByGuid(groupGuid, false);
}

// Toggle all
api.SetAllGroupsEnabled(true);
```

### Group metadata for custom mod features

```csharp
// Set by ID
api.SetGroupValue(2, "my_mod.mode", "aggressive");

// Set by GUID
if (api.TryGetGroupGuidById(2, out var g2))
{
    api.SetGroupValueByGuid(g2, "my_mod.color", "#22ccff");
}

// Read by ID
if (api.TryGetGroupValue(2, "my_mod.mode", out var mode))
{
    Logger.LogInfo($"Mode = {mode}");
}

// Read by GUID
if (api.TryGetGroupGuidById(2, out var guid) &&
    api.TryGetGroupValueByGuid(guid, "my_mod.color", out var color))
{
    Logger.LogInfo($"Color = {color}");
}

// Remove by ID
api.RemoveGroupValue(2, "my_mod.mode");

// Remove by GUID
if (api.TryGetGroupGuidById(2, out var guid2))
{
    api.RemoveGroupValueByGuid(guid2, "my_mod.color");
}
```

## Practical Guidance

- Prefer GUID-based methods for persisted integrations.
- Use ID-based methods for quick UI actions where order matters.
- `groupId == 0` means ungrouped/no group.
- `Try*` methods return `false` when target/group is invalid or unavailable.

