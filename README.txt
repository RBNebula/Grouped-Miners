GROUPED MINERS - 1.0.0

Grouped Miners is a standalone MineMogul mod for assigning AutoMiners into named groups,
toggling groups on/off, and exposing a simple API for other mods.

:: REQUIREMENTS ::
- MineMogul (up to date)
- BepInEx 5

:: OPTIONAL ::
- Rebind (com.rebind) to change the Open/Close key from in-game keybind UI

:: INSTALLATION ::
1. Install BepInEx 5 for MineMogul.
2. Copy GroupedMiners.dll into:
   BepInEx\plugins\
3. Launch game once to generate config/save files.

:: USAGE ::
- Default open key: N
- Look at a miner and press N: opens Assign Miner Group panel
- Not looking at a miner and press N: opens Grouped Miners panel

In Grouped Miners panel you can:
- Rename groups
- Toggle each group On/Off
- Toggle all miners On/Off
- Add/remove groups

Status dot colors:
- Green: all miners in that scope are enabled
- Orange: mixed enabled/disabled
- Red: all miners in that scope are disabled

:: API FOR OTHER MODS ::
Namespace: GroupedMiners
Interface: IGroupedMinersApi
Host accessor:
GroupedMiners.GroupedMiners.TryGetApi(out IGroupedMinersApi? api)

Key points:
- Group Id is UI/order only (not persistence-safe)
- GroupGuid is persistence-safe
- groupId 0 means ungrouped
- Miner lists returned are snapshots

Main API groups:
- Group lookup: GetGroups, TryGetGroupById, TryGetGroupByGuid, Guid/Id conversion
- Miner lookup: GetMinersInGroup, TryGetTargetMiner, TryGetMinerGroup, TryGetMinerGroupGuid
- Assignment: TryAssignMinerToGroup, TryAssignTargetMinerToGroup, TryUngroupMiner
- Group controls: SetGroupName, SetGroupEnabled, SetAllGroupsEnabled
- Metadata: SetGroupValue / TryGetGroupValue / RemoveGroupValue

Full API details and examples are in README.md.

:: NOTES ::
- This mod is standalone (no ModLib dependency).
- Group metadata key/value storage is runtime-only.

:: TROUBLESHOOTING ::
- If keybind does not open UI, check for key conflicts.
- If using Rebind, configure keybinds under:
  Settings > Keybinds > MODS > Grouped Miners
