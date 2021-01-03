using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateAFK
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using Exiled.API.Interfaces;

    public sealed class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool TryReplace { get; private set; } = true;
        [Description("Should Tutorials be ignored?")]
        public bool IgnoreTut { get; private set; } = true;
        public int AfkTime { get; private set; } = 30;
        public int GraceTime { get; private set; } = 15;
        public int NumBeforeKick { get; private set; } = 2;
        public int MaxReplaceTime { get; private set; } = -1;
        public string MsgPrefix { get; private set; } = "<color=white>[</color><color=green>uAFK</color><color=white>]</color>";
        public string MsgGrace { get; private set; } = "<color=red>You will be moved to spectator in</color> <color=white>%timeleft% seconds</color><color=red> if you do not move!</color>";
        public string MsgFspec { get; private set; } = "You were detected as AFK and automatically moved to spectator!";
        public string MsgKick { get; private set; } = "You were AFK for too long!";
        public string MsgReplace { get; private set; } = "You have replaced a player that was AFK.";
    }
}
