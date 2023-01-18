using System.Collections.Generic;
using GameCore;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using UltimateAFK.Handlers;
using UltimateAFK.Resources;
using Log = PluginAPI.Core.Log;

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
        [PluginEntryPoint("UltimateAFK", "6.1.0", "Checks if a player is afk for too long and if detected as afk will be replaced by a spectator.", "SrLicht")]
        void OnEnabled()
        {
            Singleton = this;
            PluginAPI.Events.EventManager.RegisterEvents(this, new Handlers.MainHandler(Singleton));
            
            if (ConfigFile.ServerConfig.GetFloat("afk_time", 90f) > 0)
            {
                Log.Warning($"You have enabled the AFK detector of the base game, please disable it by config &6afk_time = 0&r in &4config_gameplay.txt&r");
            }
            
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