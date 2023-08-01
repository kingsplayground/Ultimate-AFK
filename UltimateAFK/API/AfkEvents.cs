using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateAFK.API
{
    public class AfkEvents
    {
        // Lazy event system... Sorry i dont know well doing events in a simple way.
        private static AfkEvents _instance;

        public static AfkEvents Instance
        {
            get
            {
                _instance ??= new AfkEvents();
                return _instance;
            }
        }

        public void InvokePlayerAfkDetected(Player player, bool isForCommand)
        {
            PlayerAfkDetectedEvent?.Invoke(player, isForCommand);
        }

        public delegate void PlayerAfkDetected(Player player, bool isForCommand);

        public event PlayerAfkDetected PlayerAfkDetectedEvent;


        // Prevent extarnal instances.
        private AfkEvents() { }
       
    }
}
