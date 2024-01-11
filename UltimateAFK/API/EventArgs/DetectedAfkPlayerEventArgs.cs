using PluginAPI.Core;

namespace UltimateAFK.API.EventArgs
{
    /// <summary>
    /// Represents the event arguments for detecting an AFK player.
    /// </summary>
    public sealed class DetectedAfkPlayerEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DetectedAfkPlayerEventArgs"/>.
        /// </summary>
        /// <param name="player">The AFK player that has been detected.</param>
        /// <param name="isForCommand">Indicates whether the detection is associated with a specific command.</param>
        public DetectedAfkPlayerEventArgs(Player player, bool isForCommand)
        {
            Player = player;
            IsForCommand = isForCommand;
        }

        /// <summary>
        /// Gets the player who is detected as AFK player.
        /// </summary>
        public Player Player { get; }

        /// <summary>
        /// Gets if the detection if for the command.
        /// </summary>
        public bool IsForCommand { get; }
    }
}
