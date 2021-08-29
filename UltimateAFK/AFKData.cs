using Exiled.API.Features.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UltimateAFK
{
	struct AFKData
	{
		public AFKComponent afkComp;

		public Vector3 spawnLocation;
		public Dictionary<ItemType, ushort> ammo;
		public List<ItemType> items;

		public float health;

		public bool is079;
		public byte level;
		public float xp;
		public float energy;

		public object roleEasyEvents;
	}
}
