using CommandSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateAFK.Handlers.Commands.SubCommands
{
    internal class CustomKit : ICommand
    {
        public string Command => UltimateAFK.Instance.Config.CustomCommandPrefix;

        public string[] Aliases => UltimateAFK.Instance.Config.CustomCommandAliases;

        public string Description => UltimateAFK.Instance.Config.CustomCommandDescription;

        public static CustomKit Instance => new CustomKit();

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = "Gwa gwa";
            return true;
        }
    }
}
