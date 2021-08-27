using System;
using System.Linq;
using UnityEngine;
using MEC;
using Exiled.API.Features;
using Exiled.Loader;
using PlayableScps;
using System.Reflection;
using Exiled.API.Enums;
using System.Collections.Generic;
using Exiled.API.Features.Items;

namespace UltimateAFK
{
	public class AFKComponent : MonoBehaviour
	{
		public MainClass plugin;

		public bool disabled = false;

		Player ply;

		public Vector3 AFKLastPosition;
		public Vector3 AFKLastAngle;

		public int AFKTime = 0;
		public int AFKCount = 0;
		private float timer = 0.0f;

		// Do not change this delay. It will screw up the detection
		public float delay = 1.0f;

		// Expose replacing player for plugin support
		public Player PlayerToReplace;


		void Awake()
		{
			ply = Player.Get(gameObject);
		}

		void Update()
		{
			timer += Time.deltaTime;
			if (timer > delay)
			{
				timer = 0f;
				//Log.Info(this.disabled);
				if (!this.disabled)
				{
					try
					{
						AFKChecker();
					}
					catch (Exception e)
					{
						Log.Error(e);
					}
				}
			}
		}

		// Called every 1 second according to the player's Update function. This is way more efficient than the old way of doing a forloop for every player.
		// Also, since the gameObject for the player is deleted when they disconnect, we don't need to worry about cleaning any variables :) 
		private void AFKChecker()
		{
			//Log.Info($"AFK Time: {this.AFKTime} AFK Count: {this.AFKCount}");
			if (this.ply.Team == Team.RIP || Player.List.Count() <= plugin.Config.MinPlayers || (plugin.Config.IgnoreTut && this.ply.Team == Team.TUT)) return;

			bool isScp079 = (this.ply.Role == RoleType.Scp079);
			bool scp096TryNotToCry = false;

			// When SCP096 is in the state "TryNotToCry" he cannot move or it will cancel,
			// therefore, we don't want to AFK check 096 while he's in this state.
			if (this.ply.Role == RoleType.Scp096)
			{
				PlayableScps.Scp096 scp096 = this.ply.ReferenceHub.scpsController.CurrentScp as PlayableScps.Scp096;
				scp096TryNotToCry = (scp096.PlayerState == Scp096PlayerState.TryNotToCry);
			}

			Vector3 CurrentPos = this.ply.Position;
			Vector3 CurrentAngle = (isScp079) ? this.ply.Camera.targetPosition.position : this.ply.Rotation;

			if (CurrentPos != this.AFKLastPosition || CurrentAngle != this.AFKLastAngle || scp096TryNotToCry)
			{
				this.AFKLastPosition = CurrentPos;
				this.AFKLastAngle = CurrentAngle;
				this.AFKTime = 0;
				PlayerToReplace = null;
				return;
			}

			// The player hasn't moved past this point.
			this.AFKTime++;

			// If the player hasn't reached the time yet don't continue.
			if (this.AFKTime < plugin.Config.AfkTime) return;

			// Check if we're still in the "grace" period
			int secondsuntilspec = (plugin.Config.AfkTime + plugin.Config.GraceTime) - this.AFKTime;
			if (secondsuntilspec > 0)
			{
				string warning = plugin.Config.MsgGrace;
				warning = warning.Replace("%timeleft%", secondsuntilspec.ToString());

				this.ply.ClearBroadcasts();
				this.ply.Broadcast(1, $"{plugin.Config.MsgPrefix} {warning}", Broadcast.BroadcastFlags.Normal, false);
				return;
			}

			// The player is AFK and action will be taken.
			Log.Info($"{this.ply.Nickname} ({this.ply.UserId}) was detected as AFK!");
			this.AFKTime = 0;

			// Let's make sure they are still alive before doing any replacement.
			if (this.ply.Team == Team.RIP) return;

			if (plugin.Config.TryReplace && !IsPastReplaceTime())
			{
				Assembly easyEvents = Loader.Plugins.FirstOrDefault(pl => pl.Name == "EasyEvents")?.Assembly;

				var roleEasyEvents = easyEvents?.GetType("EasyEvents.Util")?.GetMethod("GetRole")?.Invoke(null, new object[] { this.ply });

				// SCP035 Support (Credit DCReplace)
				bool is035 = this.ply.Id == TryGet035()?.Id;

				// Credit: DCReplace :)
				// I mean at this point 90% of this has been rewritten lol...
				List<ItemType> inventory = this.ply.Items.Select(item => item.Type).ToList();

				RoleType role = this.ply.Role;
				Vector3 pos = this.ply.Position;
				float health = this.ply.Health;

				// New strange ammo system because the old one was fucked.
				Dictionary<ItemType, ushort> ammo = new Dictionary<ItemType, ushort>();
				foreach (ItemType ammoType in this.ply.Ammo.Keys)
				{
					ammo.Add(ammoType, this.ply.Ammo[ammoType]);
				}

				// Stuff for 079
				byte Level079 = 0;
				float Exp079 = 0f, AP079 = 0f;
				if (isScp079)
				{
					Level079 = this.ply.Level;
					Exp079 = this.ply.Experience;
					AP079 = this.ply.Energy;
				}

				PlayerToReplace = Player.List.FirstOrDefault(x => x.Role == RoleType.Spectator && x.UserId != string.Empty && !x.IsOverwatchEnabled && x != this.ply);
				if (PlayerToReplace != null)
				{
					// Make the player a spectator first so other plugins can do things on player changing role with uAFK.
					this.ply.ClearInventory(); // Clear their items to prevent dupes.
					this.ply.SetRole(RoleType.Spectator);
					this.ply.Broadcast(30, $"{plugin.Config.MsgPrefix} {plugin.Config.MsgFspec}");

					PlayerEvents.ReplacingPlayers.Add(PlayerToReplace, new AFKData()
					{
						afkComp = this,
						spawnLocation = pos,
						ammo = ammo,
						items = inventory,
						is079 = isScp079,
						level = this.ply.Level,
						xp = this.ply.Experience,
						energy = this.ply.Energy,
						health = health,
						roleEasyEvents = roleEasyEvents
					});

					if (is035)
					{
						TrySpawn035(PlayerToReplace);
					}
					else
					{
						PlayerToReplace.SetRole(role);
					}
				}
				else
				{
					// Couldn't find a valid player to spawn, just ForceToSpec anyways.
					ForceToSpec(this.ply);
				}
			}
			else
			{
				// Replacing is disabled, just ForceToSpec
				ForceToSpec(this.ply);
			}
			// If it's -1 we won't be kicking at all.
			if (plugin.Config.NumBeforeKick != -1)
			{
				// Increment AFK Count
				this.AFKCount++;
				if (this.AFKCount >= plugin.Config.NumBeforeKick)
				{
					// Since this.AFKCount is greater than the config we're going to kick that player for being AFK too many times in one match.
					ServerConsole.Disconnect(this.gameObject, plugin.Config.MsgKick);
				}
			}
		}

		private void ForceToSpec(Player hub)
		{
			hub.SetRole(RoleType.Spectator);
			hub.Broadcast(30, $"{plugin.Config.MsgPrefix} {plugin.Config.MsgFspec}");
		}

		private Player TryGet035()
		{
			Player scp035 = null;
			if (Loader.Plugins.FirstOrDefault(pl => pl.Name == "scp035") != null)
				scp035 = (Player)Loader.Plugins.First(pl => pl.Name == "scp035").Assembly.GetType("scp035.API.Scp035Data").GetMethod("GetScp035", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
			return scp035;
		}
		private void TrySpawn035(Player player)
		{
			if (Loader.Plugins.FirstOrDefault(pl => pl.Name == "scp035") != null)
			{
				Loader.Plugins.First(pl => pl.Name == "scp035").Assembly.GetType("scp035.API.Scp035Data").GetMethod("GetScp035", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { player });
			}
		}

		private bool IsPastReplaceTime()
		{
			if (plugin.Config.MaxReplaceTime != -1)
			{
				if (Round.ElapsedTime.TotalSeconds > plugin.Config.MaxReplaceTime)
				{
					Log.Info("Since we are past the allowed replace time, we will not look for replacement player.");
					return true;
				}
			}
			return false;
		}
	}
}
