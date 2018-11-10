namespace grid_shared.grid.commands
{
    public interface IConsoleCommand
    {
        string GetName();

        string GetDescription();

        void Execute(params string[] arguments);
    }
}
