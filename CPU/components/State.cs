namespace CPU.components
{
    public class State
    {
        public byte PC { get; private set; }
        public bool Z { get; private set; }
        public bool C { get; private set; }
        public int RegisterCount { get; }

        
        public State(int registerCount)
        {
            RegisterCount = registerCount;
            _registers = new byte[RegisterCount];
            Reset();
        }

        public void Reset()
        {
            PC = 0;
            Z = false;
            C = false;
            Array.Clear(_registers);
        }

        public byte GetRegister(int index) => _registers[index];
        public void SetRegister(int index, byte value) => _registers[index] = value;
        public void SetPC(byte address) => PC = address;
        public void IncrementPC(byte value = 1) => SetPC((byte)(PC + value));
        public void SetZeroFlag(bool value) => Z = value;
        public int GetZeroFlagAsInt() => Z ? 1 : 0;
        public void SetCarryFlag(bool value) => C = value;
        public int GetCarryFlagAsInt() => C ? 1 : 0;

        private readonly byte[] _registers;
    }

    internal static class StateDebugExtensions
    {
        public static void Dump(this State state)
        {
            Console.WriteLine("State Dump:");
            Console.WriteLine($"PC: {state.PC:X2} Z: {state.Z} C: {state.C}");
            for (int i = 0; i < state.RegisterCount; i++)
            {
                Console.Write($"R{i}: {state.GetRegister(i):X2} ");
            }
            Console.WriteLine();
        }
    }
}