| ![Github All Releases](https://img.shields.io/github/downloads/SrLicht/Ultimate-AFK/total.svg) | <a href="https://github.com/SrLicht/Ultimate-AFK/releases"><img src="https://img.shields.io/github/v/release/SrLicht/Ultimate-AFK?include_prereleases&label=Last Release" alt="Releases"></a> | <a href="https://discord.gg/PyUkWTg"><img src="https://img.shields.io/discord/656673194693885975?color=%23aa0000&label=EXILED" alt="Support"></a> |

# Ultimate-AFK
This is an updated version of the original Ultimate AFK plugin from https://github.com/kingsplayground/Ultimate-AFK.

# Features
- Detects AFK Players via in-game movement, camera movement, and in-game interactions
- Moves players to spectator after a determined AFK Time and grace period
- (Optional) Kick players from the server after repeated AFK detections!
- Custom broadcasts to AFK Players to indicate to them if they are AFK. 
- Works with SCP-079 by checking camera angle, and experience interactions

# Permission
If you give a role the `uafk.ignore` permission it will be ignored by the plugin and will never be set to afk, useful for administrators.

# Installation
**[EXILED](https://github.com/galaxy119/EXILED) must be installed for this to work.**

Place the "UltimateAFK.dll" file in your Plugins folder.

# IMPORTANT

For technical and Lazy reasons you have to have ``Exiled.CustomItems.dll`` in your plugins folder, this file is included in any current version of Exiled, and it doesn't matter if you have the CustomItemsSupport setting to false, you need ``Exiled.CustomItems``

![image](https://user-images.githubusercontent.com/36207738/173796583-8f1a3287-3ab9-4d36-9aad-efbf142cb1e0.png)

``Exiled.CustomItems.dll`` does not contain any CustomItems and is only an API used for spawning and creating CustomItems, if you want CustomItems you need to download this plugin https://github.com/Exiled-Team/CustomItems
