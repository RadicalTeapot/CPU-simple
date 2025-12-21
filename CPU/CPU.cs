using CPU.components;
using CPU.opcodes;
using System.Diagnostics;

namespace CPU
{
    public class CPU
    {
        public CPU(Config config)
        {
            _memory = new Memory(config.MemorySize - config.StackSize);
            _state = new State(config.RegisterCount);
            _stack = new Stack(config.StackSize);
            _opcodeFactory = new OpcodeFactory(_state, _stack, _memory);
        }

        public CPU(State state, Stack stack, Memory memory)
        {
            _state = state;
            _stack = stack;
            _memory = memory;
            _opcodeFactory = new OpcodeFactory(_state, _stack, _memory);
        }

        public void Reset()
        {
            _state.Reset();
            _stack.Reset();
            // Note: Memory is not cleared on reset
        }

        public void LoadProgram(byte[] program)
        {
            Debug.Assert(program.Length <= _memory.Size, "Program size exceeds memory size.");

            _memory.Clear();
            _memory.LoadBytes(0, program);
        }

        public void Run(bool traceEnabled)
        {
            while (_state.GetPC() < _memory.Size) // TODO Fix for 16-bit architecture
            {
                try
                {
                    Step(traceEnabled);
                }
                catch (OpcodeException.HaltException)
                {
                    // Handle HALT exception gracefully
                    Console.WriteLine("Program halted.");
                    Dump();
                    break;
                }
                catch
                {
                    Dump();
                    throw;
                }
            }
        }

        /// <summary>Executes a single instruction cycle.</summary>
        public void Step(bool traceEnabled)
        {
            // TODO: Implement proper instruction fetching with PC incrementing after fetch (currently done in opcodes)
            var instruction = _memory.ReadByte(_state.GetPC()); // Fetch (partial)
            var opcode = _opcodeFactory.GetOpcodeFromInstruction(instruction); // Decode
            opcode.Execute(out var trace); // Execute (some fetching happens here too)

            if (traceEnabled)
            {
                trace.Dump();
            }
        }
        
        private void Dump()
        {
            Console.WriteLine("=== CPU DUMP ===");
            _state.Dump();
            _stack.Dump();
            _memory.Dump();
            Console.WriteLine("======================");
        }

        private readonly State _state;
        private readonly Stack _stack;
        private readonly Memory _memory;
        private readonly OpcodeFactory _opcodeFactory;
    }
}