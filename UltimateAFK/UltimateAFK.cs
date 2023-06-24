using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using UltimateAFK.Handlers;

namespace UltimateAFK
{
    /// <summary>
    /// Main class where all the handlers are loaded.
    /// </summary>
    public class UltimateAFK
    {
        public static UltimateAFK Singleton;

        [PluginConfig] public Config Config;

        [PluginPriority(LoadPriority.High)]
        [PluginEntryPoint("UltimateAFK", "6.3.0", "Checks if a player is afk for too long and if detected as afk will be replaced by a spectator.", "SrLicht")]
        void OnEnabled()
        {
            Singleton = this;
            PluginAPI.Events.EventManager.RegisterEvents(this, new MainHandler(Singleton));
        }

        [PluginUnload]
        void OnDisable()
        {
            MainHandler.ReplacingPlayersData.Clear();
            MainHandler.ReplacingPlayersData = null;
        }
    }
}