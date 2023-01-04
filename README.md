| ![Github All Releases](https://img.shields.io/github/downloads/SrLicht/Ultimate-AFK/total.svg) | <a href="https://github.com/SrLicht/Ultimate-AFK/releases"><img src="https://img.shields.io/github/v/release/SrLicht/Ultimate-AFK?include_prereleases&label=Last Release" alt="Releases"></a> 

# Ultimate-AFK
This plugin allows AFK players to be replaced by players who are in spectator, they can also be kicked from the server after a certain number of being detected as AFK (configurable).

# Features
- Detects AFK Players via in-game movement, camera movement
- Moves players to spectator after a determined AFK Time and grace period
- (Optional) Kick players from the server after repeated AFK detections!
- Custom broadcasts to AFK Players to indicate to them if they are AFK. 
- Works with SCP-079

# Permission
If you give a group the `uafk.ignore` permission the player will be ignored to replace a player and will also never be detected as AFK. Useful for administrators

# Installation
**This plugin only works in [NwPluginAPI](https://github.com/northwood-studios/NwPluginAPI)**

**You need [NWAPIPermissionSystem](https://github.com/CedModV2/NWAPIPermissionSystem) for this plugin to work.**

You can install this plugin using the command ``p install SrLicht/Ultimate-AFK`` on the console or by downloading the .dll file and placing it in ``SCP Secret Laboratory/PluginAPI/plugins/global or your port``

