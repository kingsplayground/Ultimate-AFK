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

using System;
using Handlers = Exiled.Events.Handlers;
using Exiled.API.Enums;
using Exiled.API.Features;
using HarmonyLib;

namespace UltimateAFK
{
	public class MainClass : Plugin<Config>
	{
		public override string Author { get; } = "Thomasjosif";
		public override string Name { get; } = "Ultimate AFK";
		public override string Prefix { get; } = "uAFK";
		public override Version Version { get; } = new Version(3, 1, 6);
		public PlayerEvents PlayerEvents;

		public override PluginPriority Priority { get; } = PluginPriority.Medium;

		public override void OnEnabled()
		{
			base.OnEnabled();
			try
			{
				PlayerEvents = new PlayerEvents(this);

				Handlers.Player.Verified += PlayerEvents.OnPlayerVerified;
				Handlers.Player.ChangingRole += PlayerEvents.OnSetClass;
				Handlers.Player.Shooting += PlayerEvents.OnPlayerShoot;
				Handlers.Player.InteractingDoor += PlayerEvents.OnDoorInteract;
				Handlers.Scp914.Activating += PlayerEvents.On914Activate;
				Handlers.Scp914.ChangingKnobSetting += PlayerEvents.On914Change;
				Handlers.Player.InteractingLocker += PlayerEvents.OnLockerInteract;
				Handlers.Player.DroppingItem += PlayerEvents.OnDropItem;
				Handlers.Scp079.GainingExperience += PlayerEvents.OnSCP079Exp;
				Handlers.Player.Spawning += PlayerEvents.OnSpawning;
				Handlers.Server.RoundStarted += PlayerEvents.OnRoundStarted;

				Log.Info($"UltimateAFK plugin loaded.\n Written by Thomasjosif for King's Playground");
			}
			catch (Exception e)
			{
				Log.Error($"There was an error loading the plugin: {e}");
			}

		}
		public override void OnDisabled()
		{
			base.OnDisabled();
			Handlers.Player.Verified -= PlayerEvents.OnPlayerVerified;
			Handlers.Player.ChangingRole -= PlayerEvents.OnSetClass;
			Handlers.Player.Shooting -= PlayerEvents.OnPlayerShoot;
			Handlers.Player.InteractingDoor -= PlayerEvents.OnDoorInteract;
			Handlers.Scp914.Activating -= PlayerEvents.On914Activate;
			Handlers.Scp914.ChangingKnobSetting -= PlayerEvents.On914Change;
			Handlers.Player.InteractingLocker -= PlayerEvents.OnLockerInteract;
			Handlers.Player.DroppingItem -= PlayerEvents.OnDropItem;
			Handlers.Scp079.GainingExperience -= PlayerEvents.OnSCP079Exp;
			Handlers.Player.Spawning -= PlayerEvents.OnSpawning;
			Handlers.Server.RoundStarted -= PlayerEvents.OnRoundStarted;

			PlayerEvents = null;
		}
	}
}