using Exiled.API.Features.Roles;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateAFK.Resources
{
    public struct AFKData
    {
        public Vector3 Position { get; set; }

        public RoleType Role { get; set; }

        public Dictionary<ItemType, ushort> Ammo { get; set; }

        public List<string> CustomItems { get; set; }

        public List<Exiled.API.Features.Items.Item> Items { get; set; }

        public float Health { get; set; }

        public Scp079Role SCP079Role { get; set; }
    }
}
