using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UltimateAFK.Handlers.Components;
using UnityEngine;

namespace UltimateAFK.Resources
{
    public struct AFKData
    {
        public AFKComponent AfkComp { get; set; }

        public Vector3 SpawnLocation { get; set; }

        public Dictionary<ItemType, ushort> Ammo { get; set; }

        public List<ItemType> Items { get; set; }

        public float Health { get; set; }

        public bool Is079 { get; set; }

        public byte Level { get; set; }

        public float Xp { get; set; }

        public float Energy { get; set; }
    }
}
