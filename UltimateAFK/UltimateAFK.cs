using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using UltimateAFK.API;
using UltimateAFK.Handlers;
using UltimateAFK.Resources;

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
        [PluginEntryPoint("UltimateAFK", "6.4.1", "Checks if a player is afk for too long and if detected as afk will be replaced by a spectator.", "SrLicht")]
        void OnEnabled()
        {
            Singleton = this;
            PluginAPI.Events.EventManager.RegisterEvents(this, new MainHandler(Singleton));
            AfkEvents.Instance.PlayerAfkDetectedEvent += OnPlayerIsDetectedAfk;
        }

        [PluginUnload]
        void OnDisable()
        {
            MainHandler.ReplacingPlayersData.Clear();
            MainHandler.ReplacingPlayersData = null;
            Extensions.AllElevators.Clear();
            AfkEvents.Instance.PlayerAfkDetectedEvent -= OnPlayerIsDetectedAfk;
        }

        public void OnPlayerIsDetectedAfk(Player player, bool isForCommand)
        {
            if (isForCommand)
            {
                Log.Info($"{player.LogName} use the command to be moved to spectator");
            }
        }
    }
}