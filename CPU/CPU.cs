using CPU.components;
using CPU.opcodes;
using System.Diagnostics;

namespace CPU
{
    public class CPU
    {
        public CPU() : this(256, 16, 4) { }

        public CPU(int memorySize, int stackSize, int registerCount)
        {
            _memory = new Memory(memorySize - stackSize);
            _state = new State(registerCount);
            _stack = new Stack(stackSize, 0xF);
            _opcodeTable = new OpcodeTable(_state, _stack, _memory);
        }

        public CPU(State state, Stack stack, Memory memory)
        {
            _state = state;
            _stack = stack;
            _memory = memory;
            _opcodeTable = new OpcodeTable(_state, _stack, _memory);
        }

        public void Reset() => _state.Reset();

        public void LoadProgram(byte[] program)
        {
            Debug.Assert(program.Length <= _memory.Size, "Program size exceeds memory size.");

            _memory.Clear();
            _memory.LoadBytes(0, program);
        }

        public void Run(bool traceEnabled)
        {
            while (_state.PC < _memory.Size)
            {
                try
                {
                    Step(traceEnabled);
                }
                catch (OpcodeException.HaltException)
                {
                    // Handle HALT exception
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
            var instruction = _memory.ReadByte(_state.PC);
            var opcode = _opcodeTable.GetOpcode(instruction);
            var size = opcode.Execute(out var trace);

            trace.PcBefore = _state.PC;
            _state.IncrementPC(size);
            trace.PcAfter = _state.PC;

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
        private readonly OpcodeTable _opcodeTable;
    }
}