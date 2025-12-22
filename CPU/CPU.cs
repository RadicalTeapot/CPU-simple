using CPU.components;
using CPU.opcodes;
using System.Diagnostics;

namespace CPU
{
    public class CPU
    {
        public CPU(Config config) :
            this(new State(config.RegisterCount),
                 new Stack(config.StackSize),
                 new Memory(config.MemorySize - config.StackSize))
        { }

        public CPU(State state, Stack stack, Memory memory)
        {
            _state = state;
            _stack = stack;
            _memory = memory;
            _opcodeFactory = new OpcodeFactory();
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
            while (_state.GetPC() - AddressSize <= _memory.Size)
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

        /// <summary>
        /// Executes a single instruction cycle: Fetch → Decode → Execute.
        /// </summary>
        public void Step(bool traceEnabled)
        {
            // Fetch
            var pcBefore = _state.GetPC();
            var instruction = _memory.ReadByte(_state.GetPC());
            var instructionSize = _opcodeFactory.GetInstructionSize(instruction);
            var instructionBytes = _memory.ReadBytes(_state.GetPC(), instructionSize);
            _state.IncrementPC(instructionSize); // Move to next instruction byte

            // Decode
            var decoded = _opcodeFactory.Decode(instructionBytes);
            var opcodeInstance = (IOpcode)decoded.OpcodeConstructor.Invoke([ _state, _memory, _stack ]);

            // Execute
            opcodeInstance.Execute(decoded.Args);

            if (traceEnabled)
            {
                var trace = new Trace(pcBefore, _state.GetPC(), decoded);
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
#if x16
        public const int AddressSize = 2;
#else
        public const int AddressSize = 1;
#endif
    }
}