using CPU.components;
using CPU.microcode;
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
            _cycle = 0;
            _opcodeFactory = new OpcodeFactory();
            _tickHandler = new TickHandler(new TickHandlerConfig(_state, _memory, _stack, _opcodeFactory));
            _tracer = new TickTracer(_state, _stack, _memory);
            _programLoaded = false;
        }

        public CpuInspector GetInspector()
            => new CpuInspector(_cycle, _state, _stack, _memory, _programLoaded, _tracer);

        public void Reset()
        {
            _state.Reset();
            _stack.Reset();
            _cycle = 0;
            _tracer.Clear();
            // Note: Memory is not cleared on reset
        }

        public void LoadProgram(byte[] program)
        {
            Debug.Assert(program.Length <= _memory.Size, "Program size exceeds memory size.");

            _memory.Clear();
            _memory.LoadBytes(0, program);
            _programLoaded = true;
            Reset();
        }

        public void Run()
        {
            Reset();
            while (_state.GetPC() - AddressSize <= _memory.Size)
            {
                try
                {
                    _tickHandler.Tick();
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
        /// Executes a single instruction cycle, which may involve multiple micro-operations depending on the instruction's complexity.
        /// </summary>
        /// <throws>OpcodeException.HaltException when a HALT instruction is executed.</throws>
        public void Step()
        {
            _tracer.Clear();
            MicrocodeTickResult result;
            do
            {
                _tracer.Prepare();
                result = _tickHandler.Tick();
                _tracer.Record(result);
            } while (!result.IsInstructionComplete);
        }

        /// <summary>
        /// Advances the timer or scheduler by one tick, triggering any actions scheduled for this interval.
        /// </summary>
        public MicrocodeTickResult Tick()
        {
            _tracer.Clear();
            _tracer.Prepare();
            var result = _tickHandler.Tick();
            _tracer.Record(result);
            return result;
        }

        private void Dump()
        {
            Console.WriteLine("=== CPU DUMP ===");
            _state.Dump();
            _stack.Dump();
            _memory.Dump();
            Console.WriteLine("======================");
        }

        private int _cycle;
        private bool _programLoaded;
        private readonly TickTracer _tracer;
        private readonly State _state;
        private readonly Stack _stack;
        private readonly Memory _memory;
        private readonly OpcodeFactory _opcodeFactory;
        private readonly TickHandler _tickHandler;
#if x16
        public const int AddressSize = 2;
#else
        public const int AddressSize = 1;
#endif
    }
}
