using CommandSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UltimateAFK.Handlers.Commands.SubCommands;

namespace UltimateAFK.Handlers.Commands
{
    internal class KitCommand : ParentCommand
    {
        public override string Command => "kit";

        public override string[] Aliases => new string[] { };

        public override string Description => "Comando de ejemplo por que soy horrible explicandome";

        public override void LoadGeneratedCommands()
        {
            if(CustomKit.Instance.Command != UltimateAFK.Instance.Config.CustomCommandPrefix)
            {
                RegisterCommand(CustomKit.Instance);
            }
        }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            throw new NotImplementedException();
        }
    }
}
