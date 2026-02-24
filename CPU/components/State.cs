namespace CPU.components
{
    public class State
    {
        public bool Z { get; private set; }
        public bool C { get; private set; }
        public int RegisterCount { get; }
     
        public State(int registerCount)
        {
            RegisterCount = registerCount;
            _registers = new Register<byte>[RegisterCount];
            for (int i = 0; i < RegisterCount; i++)
            {
                _registers[i] = new Register<byte>(0);
            }
            _pc = new(0);
            Reset();
        }

        public void Reset()
        {
            _pc.Value = 0;
            Z = false;
            C = false;
            for (int i = 0; i < RegisterCount; i++)
            {
                _registers[i] = new Register<byte>(0);
            }
        }

        public byte GetRegister(int index) => _registers[index].Value;
        public byte SetRegister(int index, byte value) => _registers[index].Value = value;
        public void IncrementPC(byte value=1) => _pc.Value += value;
        public void SetZeroFlag(bool value) => Z = value;
        public int GetZeroFlagAsInt() => Z ? 1 : 0;
        public void SetCarryFlag(bool value) => C = value;
        public int GetCarryFlagAsInt() => C ? 1 : 0;

        private readonly Register<byte>[] _registers;

#if x16
        public ushort GetPC() => _pc.Value;
        public ushort SetPC(ushort value) => _pc.Value = value;
        private readonly Register<ushort> _pc;
#else
        public byte GetPC() => _pc.Value;
        public byte SetPC(byte value) => _pc.Value = value;
        private readonly Register<byte> _pc;
#endif
    }

    internal static class StateDebugExtensions
    {
        public static void Dump(this State state)
        {
            Console.WriteLine("State Dump:");
            Console.WriteLine($"PC: {state.GetPC():X2} Z: {state.Z} C: {state.C}");
            for (int i = 0; i < state.RegisterCount; i++)
            {
                Console.Write($"R{i}: {state.GetRegister(i):X2} ");
            }
            Console.WriteLine();
        }
    }
}