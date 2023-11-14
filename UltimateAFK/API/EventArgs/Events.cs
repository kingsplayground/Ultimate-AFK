using System;

namespace UltimateAFK.API.EventArgs
{
    /// <summary>
    /// A static class that defines events related to detecting AFK players.
    /// </summary>
    public class Events
    {
#nullable disable

        /// <summary>
        /// Event triggered when an AFK player is detected.
        /// </summary>
        public static event Action<DetectedAfkPlayerEventArgs> DetectedAfkPlayer;

        /// <summary>
        /// Invokes the <see cref="DetectedAfkPlayer"/> event.
        /// </summary>
        /// <param name="ev">The event arguments containing information about the detected AFK player.</param>
        public static void OnDetectedAfkPlayer(DetectedAfkPlayerEventArgs ev) => DetectedAfkPlayer?.Invoke(ev);

#nullable enable
    }

}
