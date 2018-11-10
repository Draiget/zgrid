using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace grid_server.server.commands.list
{
    public class CommandExit : ConCommand
    {
        public CommandExit()
            : base("exit", "Closing server and disconnect clients") {
        }

        public override void Execute(params string[] arguments) {
            Logger.Info("Closing server by user command");
            ProgramGridServer.RequestShutdown();
        }
    }
}
