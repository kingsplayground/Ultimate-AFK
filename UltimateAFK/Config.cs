using CustomPlayerEffects;
using PlayerRoles;
using System.Collections.Generic;
using System.ComponentModel;

namespace UltimateAFK
{
    /// <summary>
    /// Plugin config class.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Gets or sets if the plugin is enabled.
        /// </summary>
        [Description("Set if the plugin is enabled. If false the plugin will not load any events.")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets if the plugin is in debug mode.
        /// </summary>
        [Description("set if the plugin is in debug mode. enabling this will activate log debugs in the code, this is useful to identify issues and report them in Github.")]
        public bool DebugMode { get; set; } = false;

        // copy from repository


        /// <summary>
        /// Gets or sets if the replacement of afk player is disabled.
        /// </summary>
        [Description("Setting this to true will cause players who are detected afk not to be replaced but only moved to spectator/kicked of the server.")]
        public bool DisableReplacement { get; set; } = false;

        /// <summary>
        /// Gets or sets the deplay for replacing players.
        /// </summary>
        [Description("When a player is replaced it is called a delay, if when replacing the player the position is not updated correctly, increase this value but it must not exceed 2.5 since it would be too long.")]
        public float ReplaceDelay { get; set; } = 1.3f;

        /// <summary>
        /// Gets or sets the minimun player to the works.
        /// </summary>
        [Description("If the number of players is less than this the plugin will not work.")]
        public int MinPlayers { get; set; } = 8;

        /// <summary>
        /// Gets or sets if you should ignore the players in tutorial.
        /// </summary>
        [Description("Tutorials should be ignored ?")]
        public bool IgnoreTut { get; set; } = true;

        /// <summary>
        /// Gets or sets the list of RoleTypes that should be ignored for the afk check.
        /// </summary>
        [Description("RoleTypes on this list will not be replaced by other players")]
        public List<RoleTypeId> RoleTypeBlacklist { get; set; } = new() { RoleTypeId.Scp0492 };

        /// <summary>
        /// Gets or sets how long a player can be afk for.
        /// </summary>
        [Description("How long a player can remain motionless before being detected as AFK")]
        public int AfkTime { get; set; } = 80;

        /// <summary>
        /// Gets or sets the length of time the player will be on grace period
        /// </summary>
        [Description("After being detected as AFK a message will appear on his face and he will be given this time to move or he will be Kicked/Moved to spectator.")]
        public int GraceTime { get; set; } = 30;

        /// <summary>
        /// The number of times a player has to be afk before being kicked from the server.
        /// </summary>
        [Description("The number of times a player must be moved to spectator for a player to be kicked from the server. Use -1 to disable it")]
        public int AfkCount { get; set; } = -1;

        /// <summary>
        /// Gets or sets the grace time broadcast message.
        /// </summary>
        [Description("When the player is detected as AFK and is in grace period this message will appear on his face. {0} represents the seconds the player has to move or be moved to spectator.")]
        public string MsgGrace { get; set; } = "<color=white>[</color><color=green>Ultimate-AFK</color><color=white>]</color> <color=red>You will be moved to spectator if you do not move in less than <color=white>{0}</color> seconds.</color>";

        /// <summary>
        /// Gets or sets the broadcast when a player is moved to espectator.
        /// </summary>
        [Description("This message will be sent to the player who has been moved to spectator when he is detected as AFK, it is also sent to the player's console.")]
        public string MsgFspec { get; set; } = "<color=red>You were detected as AFK and were moved to spectator</color>";

        /// <summary>
        /// Gets or sets the replace message sended to the player console.
        /// </summary>
        [Description("When a player is replaced by another player, this message will be sent to his console.")]
        public string MsgReplaced { get; set; } = "\n<color=yellow>you were replaced by {0}</color>";

        /// <summary>
        /// Gets or sets the message that will be sent to the player when is kicked from the server.
        /// </summary>
        [Description("This will be the reason for the Kick, due to the VSR it is obligatory to clarify that it is a plugin with flags like [UltimateAFK] or something similar.")]
        public string MsgKick { get; set; } = "[Ultimate-AFK] You were removed from the server for being AFK for too long.!";

        /// <summary>
        /// Gets or sets the broadcast message to be sent to the replacement player
        /// </summary>
        [Description("When a player replaces another player, this message will appear on the player's face and on the player console. | {0} it is the name of the player who was afk")]
        public string MsgReplace { get; set; } = "<color=red> You replaced {0} who was afk.</color>";

        /// <summary>
        /// Gets or sets the user ids ignored by afk check.
        /// </summary>
        [Description("Similar to give the permission of \"uafk.ignore\". This is more focused on Exiled users.")]
        public List<string> UserIdIgnored { get; set; } = new()
        {
            "11111111111@steam"
        };

        /// <summary>
        /// Gets or sets all configuracion related with the commands.
        /// </summary>
        [Description("All configuration related with the command")]
        public CommandConfig CommandConfig { get; set; } = new();
    }

    /// <summary>
    /// All settings related to the command
    /// </summary>
    public class CommandConfig
    {
        /// <summary>
        /// Gets or sets if the command is enabled.
        /// </summary>
        [Description("Is the command enabled on this server ?")]
        public bool IsEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the amount of time a player must stand still to be replaced/sent to a spectator
        /// </summary>
        [Description("When a player uses the command, he must stand still for this amount of seconds to be moved to spectator.")]
        public int SecondsStill { get; set; } = 10;

        /// <summary>
        /// Gets or sets the limit of times a player can use this command per round
        /// </summary>
        [Description("The number of times a player can be this command per round")]
        public int UseLimitsPerRound { get; set; } = 3;

        /// <summary>
        /// 
        /// </summary>
        [Description("If this is true and afk_count is greater than 1 and the player reaches the maximum afk_count he will be kicked from the server.")]
        public bool CountForKick { get; set; } = false;

        /// <summary>
        /// Gets or sets the cooldown that the player will have when using the command
        /// </summary>
        [Description("The coldown of the command when using it")]
        public float Cooldown { get; set; } = 40f;

        /// <summary>
        /// Gets or sets if the command is exclusive to certain groups.
        /// </summary>
        [Description("The command can only be used by players who have a group that is on the list ?")]
        public bool ExclusiveForGroups { get; set; } = false;

        /// <summary>
        /// Gets or sets the list of groups that can use the command
        /// </summary>
        [Description("List of groups.")]
        public List<string> UserGroupsAllowed { get; set; } = new()
        {
            "someGroup",
        };

        /// <summary>
        /// Gets or sets if the command is disabled for certain <see cref="RoleTypeId"/> 
        /// </summary>
        [Description("The command is disabled for certain RoleTypes?")]
        public bool DisableForCertainRole { get; set; } = false;

        /// <summary>
        /// Gets or sets the list of <see cref="RoleTypeId"/> that cannot use the command
        /// </summary>
        [Description("List of RoleTypes that cannot use the command")]
        public List<RoleTypeId> RoleTypeIdBlackList { get; set; } = new()
        {
            RoleTypeId.None,
            RoleTypeId.Scp0492
        };

        /// <summary>
        /// Gets or sets command responses.
        /// </summary>
        [Description("all the responses given by the command")]
        public Responses Responses { get; set; } = new();
    }

    /// <summary>
    /// All respones for the command.
    /// </summary>
    public class Responses
    {
        /// <summary>
        /// Gets or sets the response of the command when it is deactivated.
        /// </summary>
        [Description("Response given to the player when trying to use the command when it is disabled.")]
        public string OnDisable { get; set; } = "This command is disabled";

        /// <summary>
        /// Gets or sets the response of the command when successfully executed
        /// </summary>
        [Description("Response given to the player when successfully executing the command.")]
        public string OnSuccess { get; set; } = "You will be moved to spectator in {0} seconds, stand still.";

        /// <summary>
        /// Gets or sets the response of the command when the player has no hands
        /// </summary>
        [Description("Response given to the player when he has no hands")]
        public string OnSevereHands { get; set; } = "You cannot use this command if you have no hands";

        /// <summary>
        /// Gets or sets the response of the command when the player has the effect of <see cref="CardiacArrest"/>
        /// </summary>
        [Description("Response given to the player when affected by Cardiac Arrest (Effect of SCP-049)")]
        public string OnHearthAttack { get; set; } = "You cannot use this command if you have a heart attack.";

        /// <summary>
        /// Gets or sets the response of the command when the player is in the pocket dimension
        /// </summary>
        [Description("Response given to the player when trying to use the command when in the pocket dimension.")]
        public string OnPocketDimension { get; set; } = "There is no easy escape from the pocket dimension.";

        /// <summary>
        /// Gets or sets the response of the command when the player is in cooldown
        /// </summary>
        [Description("Response given to the player when he still has cooldown to use the command. {0} is the number of seconds the player has to wait.")]
        public string OnCooldown { get; set; } = "You cannot use the command yet, you have to wait {0} seconds.";

        /// <summary>
        /// Gets or sets the response of the command when the player has a roletype that cannot use the command
        /// </summary>
        [Description("Response given when a player tries to use the command with a role in the blacklist")]
        public string OnBlackListedRole { get; set; } = "You cannot use this command when you are {0}";

        /// <summary>
        /// Gets or sets the response of the command when the player does not have the group required to use the command
        /// </summary>
        [Description("Response given to the player when not in the group list")]
        public string OnGroupExclusive { get; set; } = "Your current group is not in the list of allowed groups.";

        /// <summary>
        /// Gets or sets the command response when the round is not started
        /// </summary>
        [Description("Response given to the player when he tries to use the command when the round has not started.")]
        public string OnRoundIsNotStarted { get; set; } = "The round has not started yet, you cannot use the command.";

        /// <summary>
        /// Gets or sets the response of the command when the player is dead
        /// </summary>
        [Description("Response given to the player when trying to use the command while is dead.")]
        public string OnPlayerIsDead { get; set; } = "You cannot use the command if you are dead.";

        /// <summary>
        /// Gets or sets the response of the command when the player has the command disabled due to "afk disable".
        /// </summary>
        [Description("If a player has the value \"afk disable\" (bool) set to true in his Temporary Storage, trying to use the command will give him this message")]
        public string OnUafkDisable { get; set; } = "You can't use this command because you have a subclass or some plugin temporarily removed your access to the command";

        /// <summary>
        /// Gets or sets the command response when the player is in a moving elevator
        /// </summary>
        [Description("If a player is inside an elevator he will not be able to use the command because his replacement will fall into the void if the elevator is moving.")]
        public string OnElevatorMoving { get; set; } = " You cannot use this command while in a moving elevator.";

        /// <summary>
        /// Gets or sets the command response when the player is being processed to go afk
        /// </summary>
        [Description("When a player uses the command he will have to wait X seconds to be spectator.")]
        public string OnWaitingForAfk { get; set; } = "You will be moved to spectator in {0} seconds.";

        /// <summary>
        /// Gets or sets the response of the command when the player moved when processed to go afk
        /// </summary>
        [Description("If a player moves within the time limit this message will be sent to the player's console.")]
        public string OnMoving { get; set; } = "You moved, you have to be still for {0} seconds to be moved to spectator.";

        /// <summary>
        /// Gets or sets the response of the command when the player reached the maximum number of times the command can be used per round
        /// </summary>
        [Description("If a player tries to execute the command when he has already reached his limit per round this message will be sent to the console.")]
        public string OnLimit { get; set; } = "You have reached the limit of uses of the command per round.";

        /// <summary>
        /// Gets or sets the response of the command when the player tries to use the command 2 or more times in a row
        /// </summary>
        [Description("If a player tries to execute the command while it is being processed to move to spectator he will get this message")]
        public string OnTryToExecute { get; set; } = "You are already being processed to move to spectator, you have to stand still for {0} seconds.";
    }
}
