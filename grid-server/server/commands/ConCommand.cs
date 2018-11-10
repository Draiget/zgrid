using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using grid_shared.grid.commands;
using log4net;

namespace grid_server.server.commands
{
    public abstract class ConCommand : IConsoleCommand
    {
        protected static readonly ILog Logger = LogManager.GetLogger(typeof(ProgramGridServer));

        protected string CommandName;
        protected string CommandDescription;

        protected GridServer Server;

        protected ConCommand(string name, string description = null) {
            CommandName = name;
            CommandDescription = description;
        }

        public string GetName() {
            return CommandName;
        }

        public string GetDescription() {
            return CommandDescription;
        }

        public abstract void Execute(params string[] arguments);

        private static readonly Dictionary<string,ConCommand> CommandToObjMap = new Dictionary<string, ConCommand>();

        public static void Register(GridServer server) {
            var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.BaseType == typeof(ConCommand));
            foreach (var type in types) {
                if (type == typeof(ConCommand)) {
                    continue;
                }

                var cmd = (ConCommand)Activator.CreateInstance(type);
                if (CommandToObjMap.ContainsKey(cmd.CommandName)) {
                    throw new Exception($"Command {cmd.CommandName} is already registered, trying to register twice");
                }

                cmd.Server = server;
                CommandToObjMap[cmd.CommandName] = cmd;
            }
        }

        public static ConCommand SearchByName(string name) {
            if (CommandToObjMap.ContainsKey(name)) {
                return CommandToObjMap[name];
            }

            return null;
        }
    }
}
