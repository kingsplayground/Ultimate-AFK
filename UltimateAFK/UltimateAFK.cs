using System.Collections.Generic;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
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
        
        [PluginEntryPoint("UltimateAFK", "6.0.0", "Checks if a player is afk for too long and if detected as afk will be replaced by a spectator.", "SrLicht")]
        void OnEnabled()
        {
            Singleton = this;
            PluginAPI.Events.EventManager.RegisterEvents(this, new Handlers.MainHandler(Singleton));
            //PluginAPI.Events.EventManager.RegisterEvents<Handlers.Components.AfkComponent>(this);
        }

        [PluginUnload]
        void OnDisable()
        {
            MainHandler.ReplacingPlayers.Clear();
            MainHandler.ReplacingPlayers = null;
        }
    }
}