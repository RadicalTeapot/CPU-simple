using Backend.IO;

namespace Backend.Commands.GlobalCommands
{
    internal class ReadStack
    {
        public const string Name = "readstack";
        public  static void Execute(CPU.CpuInspector inspector)
        {
            var sp = inspector.SP;
            var stack = inspector.StackContents;
            var length = Math.Min(16, stack.Length - sp);
            var data = new byte[length];
            Array.Copy(stack, sp, data, 0, length);
            var hexString = BitConverter.ToString(data).Replace("-", " ");
            new Output().Write($"Stack at SP=0x{sp:X} ({length} bytes): {hexString}");
        }
    }
}
