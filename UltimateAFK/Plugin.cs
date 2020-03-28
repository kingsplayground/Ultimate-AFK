/*
* +==================================================================================+
* |  _  ___                   ____  _                                             _  |
* | | |/  (_) _ __ __ _ ___  |  _ \| | __ _ _   _  __ _ _ __ ___  _   _ _ __   __| | |
* | | ' /| | '_ \ / _` / __| | |_) | |/ _` | | | |/ _` | '__/ _ \| | | | '_ \ / _` | |
* | | . \| | | | | (_| \__ \ |  __/| | (_| | |_| | (_| | | | (_) | |_| | | | | (_| | |
* | |_|\_\_|_| |_|\__, |___/ |_|   |_|\__,_|\__, |\__, |_|  \___/ \__,_|_| |_|\__,_| |
* |               |___/                     |___/ |___/                              |
* |                                                                                  |
* +==================================================================================+
* | SCP:SL Ultimate AFK Checker                                                      |
* | by Thomasjosif                                                                   |
* |                                                                                  |
* | Special thanks to iopietro for his awesome suggestions :)                        |
* | https://kingsplayground.fun                                                      |
* +==================================================================================+
* | MIT License                                                                      |
* |                                                                                  |
* | Copyright (C) 2020 Thomas Dick                                                   |
* | Copyright (C) 2020 King's Playground                                             |
* |                                                                                  |
* | Permission is hereby granted, free of charge, to any person obtaining a copy     |
* | of this software and associated documentation files (the "Software"), to deal    |
* | in the Software without restriction, including without limitation the rights     |
* | to use, copy, modify, merge, publish, distribute, sublicense, and/or sell        |
* | copies of the Software, and to permit persons to whom the Software is            |
* | furnished to do so, subject to the following conditions:                         |
* |                                                                                  |
* | The above copyright notice and this permission notice shall be included in all   |
* | copies or substantial portions of the Software.                                  |
* |                                                                                  |
* | THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR       |
* | IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,         |
* | FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE      |
* | AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER           |
* | LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,    |
* | OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE    |
* | SOFTWARE.                                                                        |
* +==================================================================================+
*/

using EXILED;
using System;

namespace UltimateAFK
{
    public class Plugin : EXILED.Plugin
    {
        public EventHandlers EventHandlers;

        public override void OnEnable()
        {
            this.enabled = Plugin.Config.GetBool("uafk_enabled", true);
            if (!this.enabled)
            {
                Log.Info("Ultimate AFK is disabled via EXILED CONFIG, stopping plugin load now.");
                return;
            }
            try
            {
                EventHandlers = new EventHandlers(this);

                Events.PlayerJoinEvent += EventHandlers.OnPlayerJoin;
                Events.ShootEvent += EventHandlers.OnPlayerShoot;
                Events.DoorInteractEvent += EventHandlers.OnDoorInteract;
                Events.Scp914ActivationEvent += EventHandlers.On914Activate;
                Events.Scp914KnobChangeEvent += EventHandlers.On914Change;
                Events.LockerInteractEvent += EventHandlers.OnLockerInteract;
                Events.DropItemEvent += EventHandlers.OnDropItem;
                Events.Scp079ExpGainEvent += EventHandlers.OnSCP079Exp;

                Log.Info($"UltimateAFK plugin loaded.\nWritten by Thomasjosif for King's Playground");
            }
            catch (Exception e)
            {
                Log.Error($"There was an error loading the plugin: {e}");
            }

        }

        public override void OnDisable()
        {
            Events.PlayerJoinEvent -= EventHandlers.OnPlayerJoin;
            Events.ShootEvent -= EventHandlers.OnPlayerShoot;
            Events.DoorInteractEvent -= EventHandlers.OnDoorInteract;
            Events.Scp914ActivationEvent -= EventHandlers.On914Activate;
            Events.Scp914KnobChangeEvent -= EventHandlers.On914Change;
            Events.LockerInteractEvent -= EventHandlers.OnLockerInteract;
            Events.DropItemEvent -= EventHandlers.OnDropItem;
            Events.Scp079ExpGainEvent -= EventHandlers.OnSCP079Exp;

            EventHandlers = null;
        }

        public override void OnReload()
        {
            // Not used
        }

        public override string getName { get; } = "UltimateAFK";
        public bool enabled;
    }
}