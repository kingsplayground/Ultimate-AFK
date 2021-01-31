namespace UltimateAFK
{
    using System.ComponentModel;
    using Exiled.API.Interfaces;

    public sealed class Config : IConfig
    {
        [Description("Is the plugin enabled?")]
        public bool IsEnabled { get; set; } = true;
        [Description("Minimum required players for uAFK to be active.")]
        public int MinPlayers { get; set; } = 2;
        [Description("Do AFK players get replaced by spectators?")]
        public bool TryReplace { get; private set; } = true;
        [Description("Should Tutorials be ignored?")]
        public bool IgnoreTut { get; private set; } = false;
        [Description("How long can player not move?")]
        public int AfkTime { get; private set; } = 30;
        [Description("How long to wait before player gets kicked after getting a warning for not moving?")]
        public int GraceTime { get; private set; } = 15;
        [Description("After how many changes to spectator for AFK should player get kicked?")]
        public int NumBeforeKick { get; private set; } = 2;
        [Description("Maximum replace time, if the round time is past this value it will not replace the player (Set to -1 to disable)")]
        public int MaxReplaceTime { get; private set; } = -1;
        [Description("Don't touch this if you do not understand the repercussions! - Ignore Perm and IP Checks.")]
        public bool IgnorePermissionsAndIP { get; private set; } = false;
        public string MsgPrefix { get; private set; } = "<color=white>[</color><color=green>uAFK</color><color=white>]</color>";
        public string MsgGrace { get; private set; } = "<color=red>You will be moved to spectator in</color> <color=white>%timeleft% seconds</color><color=red> if you do not move!</color>";
        public string MsgFspec { get; private set; } = "You were detected as AFK and automatically moved to spectator!";
        public string MsgKick { get; private set; } = "[Kicked by uAFK] You were AFK for too long!";
        public string MsgReplace { get; private set; } = "You have replaced a player that was AFK.";
    }
}
