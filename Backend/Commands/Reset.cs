namespace Backend.Commands
{
    internal class Reset
    {
        public const string Name = "reset";

        public static void Execute(CPU.CPU cpu)
        {
            cpu.Reset();
            Logger.Log("Reset command executed");
        }
    }
}
