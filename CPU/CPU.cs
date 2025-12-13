using CPU.opcodes;
using System.Diagnostics;

namespace CPU
{
    public class CPU
    {
        public IState State { get => _state; }

        public CPU()
        {
            _memory = new byte[256];
            _state = new State();
            _stack = new Stack(_memory);
            _opcodeTable = new OpcodeTable();
        }

        public void Reset() => _state.Reset();

        public void LoadProgram(byte[] program)
        {
            Debug.Assert(program.Length <= _memory.Length, "Program size exceeds memory size.");

            Array.Clear(_memory, 0, _memory.Length);
            Array.Copy(program, 0, _memory, 0, program.Length);
        }

        public void Run(bool traceEnabled)
        {
            while (_state.PC < LastDataAddress)
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
            var trace = new Trace();
;
            var opcode = GetInstruction(out byte[] args, trace);
            opcode.Execute(_state, args, trace);

            trace.PcBefore = _state.PC;
            _state.IncrementPC(opcode.Size);
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
            _stack.Dump(_memory);
            DumpMemory();
            Console.WriteLine("======================");
        }
        private void DumpMemory()
        {
            Console.WriteLine("Memory Dump:");
            const int columns = 16;
            var rows = _memory.Length / columns;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    Console.Write($"{_memory[i * columns + j]:X2} ");
                }
                Console.WriteLine();
            }
        }
        private BaseOpcode GetInstruction(out byte[] args, Trace trace)
        {
            var instruction = _memory[_state.PC];
            var opcode = _opcodeTable.GetOpcode(instruction);
            
            args = new byte[opcode.Size];
            args[0] = (byte)(instruction & 0x0F);
            for (int i = 1; i < opcode.Size; i++)
                args[i] = _memory[_state.PC + i];
            
            trace.Instruction = instruction;
            return opcode;
        }

        private int LastDataAddress => _memory.Length - _stack.Size;

        private readonly State _state;
        private readonly Stack _stack;
        private readonly byte[] _memory;
        private readonly OpcodeTable _opcodeTable;
    }
}