using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EXILED;

namespace UltimateAFK
{
    internal static class Config
    {
        internal static bool replace = Plugin.Config.GetBool("uafk_try_replace", true);
        internal static int afk_time = Plugin.Config.GetInt("uafk_time", 30);
        internal static int grace_time = Plugin.Config.GetInt("uafk_grace_period", 15);
        internal static int num_before_kick = Plugin.Config.GetInt("uafk_kick_count", 2);
        internal static int max_time_for_replace = Plugin.Config.GetInt("uafk_max_replace_time", -1);
        internal static string msg_prefix = Plugin.Config.GetString("uafk_prefix", "<color=white>[</color><color=green>uAFK</color><color=white>]</color>");
        internal static string grace_message = Plugin.Config.GetString("uafk_grace_period_message", "<color=red>You will be moved to spectator in</color> <color=white>%timeleft% seconds</color><color=red> if you do not move!</color>");
        internal static string fspec_message = Plugin.Config.GetString("uafk_fspec_message", "You were detected as AFK and automatically moved to spectator!");
        internal static string kick_message = Plugin.Config.GetString("uafk_kick_message", "You were AFK for too long!");
        internal static string replace_message = Plugin.Config.GetString("uafk_replace_message", "You have replaced a player that was AFK.");
    }
}