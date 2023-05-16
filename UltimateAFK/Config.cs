using PlayerRoles;
using System.Collections.Generic;
using System.ComponentModel;

namespace UltimateAFK
{
    public class Config
    {
        [Description("Setting this to false will stop the plugin from working in the next round.")]
        public bool IsEnabled { get; set; } = true;

        [Description("If you have any error in the plugin operation activate this and create an Issue in Github https://github.com/SrLicht/Ultimate-AFK/issues")]
        public bool DebugMode { get; set; } = false;

        [Description("This bool activates the logs that easily spams the console, and normally they are not required, that's why they have this separate configuration :)")]
        public bool SpamLogs { get; set; } = false;

        [Description("When a player is replaced it is called a delay, if when replacing the player the position is not updated correctly, increase this value but it must not exceed 2.5 since it would be too long.")]
        public float ReplaceDelay { get; set; } = 1.4f;

        [Description("If the number of players is less than this the plugin will not work.")]
        public int MinPlayers { get; set; } = 8;

        [Description("Tutorials should be ignored ?")]
        public bool IgnoreTut { get; set; } = true;

        [Description("Players in overwatch mode will be ignored for replace a afk player")]
        public bool IgnoreOverwatch { get; set; } = true;

        [Description("RoleTypes on this list will not be replaced by other players")]
        public List<RoleTypeId> RoleTypeBlacklist { get; set; } = new() { RoleTypeId.Scp0492 };

        [Description("The time it takes for a player to stand still before he is detected as AFK")]
        public int AfkTime { get; set; } = 80;

        [Description("After being detected as AFK a message will appear on his face and he will be given this time to move or he will be Kicked/Moved to spectator.")]
        public int GraceTime { get; set; } = 30;

        [Description("The number of times a player must be moved to spectator for a player to be kicked from the server. Use -1 to disable it")]
        public int AfkCount { get; set; } = -1;

        [Description("When the player is detected as AFK and is in grace period this message will appear on his face. {0} represents the seconds the player has to move or be moved to spectator.")]
        public string MsgGrace { get; set; } = "<color=white>[</color><color=green>Ultimate-AFK</color><color=white>]</color> <color=red>You will be moved to spectator if you do not move in less than <color=white>{0}</color> seconds.</color>";

        [Description("This message will be sent to the player who has been moved to spectator when he is detected as AFK, it is also sent to the player's console.")]
        public string MsgFspec { get; set; } = "<color=red>You were detected as AFK and were moved to spectator</color>";

        [Description("This will be the reason for the Kick, due to the VSR it is obligatory to clarify that it is a plugin with flags like [UltimateAFK] or something similar.")]
        public string MsgKick { get; set; } = "[Ultimate-AFK] You were removed from the server for being AFK for too long.!";

        [Description("When a player replaces another player, this message will appear on the player's face and on the player console. | {0} it is the name of the player who was afk")]
        public string MsgReplace { get; set; } = "<color=red> You replaced {0} who was afk.</color>";

        [Description("All configuration related with the command")]
        public CommandConfig CommandConfig { get; set; } = new();
    }

    public class CommandConfig
    {
        [Description("Is the command enabled on this server ?")]
        public bool IsEnabled { get; set; } = false;

        [Description("Players who use this command will be replaced by players who are in spectator")]
        public bool Replace { get; set; } = true;

        [Description("The coldown of the command when using it")]
        public float Cooldown { get; set; } = 40f;

        [Description("The command can only be used by players who have a group that is on the list ?")]
        public bool ExclusiveForGroups { get; set; } = false;

        [Description("List of groups.")]
        public List<string> UserGroupsAllowed { get; set; } = new()
        {
            "someGroup",
        };

        [Description("The command is disabled for certain RoleTypes?")]
        public bool DisableForCertainRole { get; set; } = false;

        [Description("List of RoleTypes that cannot use the command")]
        public List<RoleTypeId> RoleTypeIdBlackList { get; set; } = new()
        {
            RoleTypeId.None,
        };

        public Responses Responses { get; set; } = new();
    }

    public class Responses
    {
        [Description("Response given to the player when trying to use the command when it is disabled.")]
        public string OnDisable { get; set; } = "This command is disabled";

        [Description("Response given to the player when successfully executing the command.")]
        public string OnSuccess { get; set; } = "You were moved to spectator because you considered yourself AFK.";

        [Description("Response given to the player when he has no hands")]
        public string OnSevereHands { get; set; } = "You cannot use this command if you have no hands";

        [Description("Response given to the player when affected by Cardiact Arrest (Effect of SCP-049)")]
        public string OnHearthAttack { get; set; } = "You cannot use this command if you have a heart attack.";

        [Description("Response given to the player when trying to use the command when in the pocket dimension.")]
        public string OnPocketDimension { get; set; } = "There is no easy escape from the pocket dimension.";

        [Description("Response given to the player when he still has cooldown to use the command. {0} is the number of seconds the player has to wait.")]
        public string OnCooldown { get; set; } = "You cannot use the command yet, you have to wait {0} seconds.";

        [Description("Response given when a player tries to use the command with a role in the blacklist")]
        public string OnBlackListedRole { get; set; } = "You cannot use this command when you are {0}";

        [Description("Response given to the player when not in the group list")]
        public string OnGroupExclusive { get; set; } = "Your current group is not in the list of allowed groups.";

        [Description("Response given to the player when he tries to use the command when the round has not started.")]
        public string OnRoundIsNotStarted { get; set; } = "The round has not started yet, you cannot use the command.";

        [Description("Response given to the player when trying to use the command while is dead.")]
        public string OnPlayerIsDead { get; set; } = "You cannot use the command if you are dead.";
    }
}