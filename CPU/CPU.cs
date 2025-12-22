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
            var pcBefore = _state.GetPC();

            var instructionBytes = Fetch();
            var opcodeInstance = Decode(instructionBytes, out DecodedInstruction decoded);
            opcodeInstance.Execute();

            if (traceEnabled)
            {
                var trace = new Trace(pcBefore, _state.GetPC(), decoded);
                trace.Dump();
            }
        }

        private byte[] Fetch()
        {
            var instruction = _memory.ReadByte(_state.GetPC());
            var instructionSize = _opcodeFactory.GetInstructionSize(instruction);
            var instructionBytes = _memory.ReadBytes(_state.GetPC(), instructionSize);
            _state.IncrementPC(instructionSize); // Move to next instruction byte
            return instructionBytes;
        }

        private IOpcode Decode(byte[] instructionBytes, out DecodedInstruction decoded)
        {
            decoded = _opcodeFactory.Decode(instructionBytes);
            Debug.Assert(
                decoded.Metadata.OpcodeConstructor != null,
                "Opcode constructor should not be null after decoding.");
            Debug.Assert(
                typeof(IOpcode).IsAssignableFrom(decoded.Metadata.OpcodeConstructor.DeclaringType),
                "Decoded opcode constructor must belong to a type implementing IOpcode.");
            return (IOpcode)decoded.OpcodeConstructor.Invoke([_state, _memory, _stack, decoded.Args]);
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