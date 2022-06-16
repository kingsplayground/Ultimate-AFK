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
``UltimateAFK-CustomItemSupport.dll`` It is a separate plugin that makes replacing a player search the afk inventory for customitems and give it to the person replacing him, you only have to have 1 of the two versions enabled at the same time (``UltimateAFK-CustomItemSupport.dll`` or ``UltimateAFK.dll``).
