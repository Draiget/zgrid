using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace grid_server.server.commands.list
{
    public class CommandStatus : ConCommand
    {
        public CommandStatus()
            : base("status", "Show server status (active users and jobs summary)") {
        }

        public override void Execute(params string[] arguments) {
            Console.WriteLine($"Connected workers: {Server.GetWorkersCount()}");
        }
    }
}
