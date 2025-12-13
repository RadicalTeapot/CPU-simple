using System.Diagnostics;

namespace CPU
{
    public interface IState
    {
        byte PC { get; }
        bool Z { get; }
        bool C { get; }
        byte GetRegister(int index);
        void SetRegister(int index, byte value);
        void Reset();
    }

    internal class State : IState
    {
        public byte PC { get; private set; }
        public bool Z { get; private set; }
        public bool C { get; private set; }
        public int RegisterCount { get; }

        public State() : this(DEFAULT_REGISTER_COUNT) { }

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
            Array.Clear(_registers, 0, DEFAULT_REGISTER_COUNT);
        }

        public byte GetRegister(int index) => _registers[index];
        public void SetRegister(int index, byte value) => _registers[index] = value;
        public void IncrementPC(byte value = 1) => PC += value;

        private readonly byte[] _registers;
        private const int DEFAULT_REGISTER_COUNT = 4;
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