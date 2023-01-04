using System;
using System.Collections.Generic;
using PluginAPI.Core.Attributes;

namespace UltimateAFK
{
    /// <summary>
    /// Main class where all the handlers are loaded.
    /// </summary>
    public class UltimateAFK
    {
        public static UltimateAFK Singleton;

        [PluginConfig] public Config Config;
        
        [PluginEntryPoint("UltimateAFK", "1.0.0", "Checks if a player is afk for too long and if detected as afk will be replaced by a spectator.", "SrLicht")]
        void OnEnabled()
        {
            Singleton = this;
            PluginAPI.Events.EventManager.RegisterEvents(this, new Handlers.MainHandler(Singleton));
        }
    }
}