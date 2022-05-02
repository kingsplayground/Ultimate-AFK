namespace UltimateAFK.API.Base
{
    /// <summary>
    /// Base API for <see cref="AFK"/> allow create handlers in a "easily" way.
    /// </summary>
    public abstract class Handler
    {
        /// <summary>
        /// Plugin Instance
        /// </summary>
        protected UltimateAFK Plugin => UltimateAFK.Instance;

        /// <summary>
        /// Triggered when plugin is loaded
        /// </summary>
        public abstract void Start();

        /// <summary>
        /// Triggered when plugin is stopped or server restart.
        /// </summary>
        public abstract void Stop();
    }
}
