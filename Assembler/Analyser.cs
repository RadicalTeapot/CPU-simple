using Assembler.Analysis;
using Assembler.Analysis.Directives;
using Assembler.Analysis.EmitNode;
using Assembler.Analysis.Instructions;
using Assembler.AST;
using CPU.opcodes;
using System.Diagnostics;

namespace Assembler
{
    public class AnalyserException : Exception
    {
        public int Line { get; }
        public int Column { get; }
        public AnalyserException(string message, int line, int column)
            : base($"{message} at line {line}, column {column}")
        {
            Line = line;
            Column = column;
        }
    }

    public class Analyser
    {
        public Analyser(int memorySize = 0)
        {
#if x16
            memorySize = memorySize == 0 || memorySize > 65535 ? 65535 : (memorySize - 1);
#else
            memorySize = memorySize == 0 || memorySize > 256 ? 255 : (memorySize - 1);
#endif
            _memoryAddressValueProcessor = new MemoryAddressValueProcessor(memorySize);
            // IRQ vector address: main memory ends at (memorySize - stackSize), IRQ section is placed
            // IrqSectionSize bytes before that. Default: 256 - 16 (stack) - 16 (irq) = 224 = 0xE0.
            // NOTE: future upgrade to a vector table would replace this fixed address calculation.
            _irqVectorAddress = memorySize + 1 - DefaultStackSize - CPU.Config.IrqSectionSize;
        }

        public IList<IEmitNode> Run(Parser.ProgramNode program)
        {
            Initialize();

            // First pass: analyse statements
            Debug.Assert(
                _sections.Count >= 1 && _sections[TextSectionIndex].SectionType == Section.Type.Text,
                "Initial section setup is incorrect");

            var analysisExceptions = new List<AnalyserException>();
            foreach (var statement in program.Statements)
            {
                try
                {
                    HandleStatement(statement);
                }
                catch (AnalyserException ex)
                {
                    // Attempt to recover by skipping to the next statement
                    analysisExceptions.Add(ex);
                }
            }

            if (analysisExceptions.Count > 0)
            {
                throw new AggregateException("Analysis failed", analysisExceptions);
            }

            // Second pass: place sections and resolve labels
            // Place sequential sections (Text, Data) first
            var sectionOffset = 0;
            foreach (var section in _sections)
            {
                if (section.SectionType == Section.Type.Irq)
                    continue;
                section.StartAddress = sectionOffset;
                sectionOffset += section.LocationCounter;
            }

            // Place IRQ section at fixed address with fill gap
            if (_irqSectionIndex >= 0)
            {
                var irqSection = _sections[_irqSectionIndex];
                irqSection.StartAddress = _irqVectorAddress;
            }
            _labelManager.ResolveLabels();

            // Collect all nodes in order, inserting fill gap before IRQ section
            var emitNodes = new List<IEmitNode>();
            foreach (var section in _sections)
            {
                if (section.SectionType == Section.Type.Irq)
                    continue;
                foreach (var analysisNode in section.Nodes)
                {
                    emitNodes.AddRange(analysisNode.EmitNodes);
                }
            }

            if (_irqSectionIndex >= 0)
            {
                var irqSection = _sections[_irqSectionIndex];
                var gap = irqSection.StartAddress - sectionOffset;
                if (gap > 0)
                {
                    emitNodes.Add(new FillEmitNode(gap, 0x00, new AST.NodeSpan(0, 0, 0)));
                }
                foreach (var analysisNode in irqSection.Nodes)
                {
                    emitNodes.AddRange(analysisNode.EmitNodes);
                }
            }
            _analysisRan = true;
            return emitNodes;
        }

        public IList<Symbol> GetSymbols()
        {
            // Emit
            // - a symbol table (name, address, kind (label, section, variable, function))
            // - a mapping of source lines to addresses
            if (!_analysisRan)
            {
                throw new InvalidOperationException("Analysis must be run before building debug info.");
            }

            return _labelManager.GetAllSymbols();
        }

        private void Initialize()
        {
            _sections = [new Section(Section.Type.Text)]; // Text section should always be first
            _currentSectionIndex = TextSectionIndex;
            _irqSectionIndex = -1;
            _labelManager = new LabelReferenceManager();
            _analysisRan = false;
        }

        private void HandleStatement(StatementNode statement)
        {
            HandleHeaderDirective(statement);

            if (statement.HasLabel)
            {
                _labelManager.LocateLabel(statement.GetLabel(), CurrentSection);
            }

            if (statement.HasPostDirective)
            {
                HandleDirective(statement.GetPostDirective());
            }

            if (statement.HasInstruction)
            {
                var instruction = statement.GetInstruction();
                if (CurrentSection.SectionType == Section.Type.Data)
                {
                    throw new AnalyserException("Instructions are not allowed in data sections",
                        instruction.Span.Line, instruction.Span.StartColumn);
                }
                HandleInstruction(instruction);
            }
        }
        
        private void HandleHeaderDirective(StatementNode statement)
        {
            if (statement.HasHeaderDirective)
            {
                var headerDirective = statement.GetHeaderDirective();
                switch (headerDirective.Directive)
                {
                    case "data":
                        var dataSection = new Section(Section.Type.Data);
                        _sections.Add(dataSection);
                        _currentSectionIndex = _sections.Count - 1;
                        break;
                    case "irq":
                        if (_irqSectionIndex >= 0)
                        {
                            throw new AnalyserException("Duplicate .irq directive; only one IRQ section is allowed",
                                headerDirective.Span.Line, headerDirective.Span.StartColumn);
                        }
                        var irqSection = new Section(Section.Type.Irq);
                        _sections.Add(irqSection);
                        _irqSectionIndex = _sections.Count - 1;
                        _currentSectionIndex = _irqSectionIndex;
                        break;
                    case "text":
                        _currentSectionIndex = TextSectionIndex; // Switch (back) to the text section
                        break;
                    case "org":
                    case "byte":
                    case "short":
                    case "zero":
                    case "string":
                        HandleDirective(headerDirective);
                        break;
                    default:
                        throw new AnalyserException($"Invalid header directive: {headerDirective.Directive}",
                                headerDirective.Span.Line, headerDirective.Span.StartColumn);
                }
            }
        }

        private void HandleDirective(DirectiveNode directive)
        {
            if (CurrentSection.SectionType != Section.Type.Data)
            {
                var sectionName = CurrentSection.SectionType == Section.Type.Irq ? "IRQ" : "text";
                throw new AnalyserException($"Data directives are not allowed in the {sectionName} section",
                    directive.Span.Line, directive.Span.StartColumn);
            }

            switch (directive.Directive)
            {
                case "byte":
                    CurrentSection.Nodes.Add(new ByteNode(directive));
                    break;
                case "short":
                    CurrentSection.Nodes.Add(new ShortNode(directive));
                    break;
                case "zero":
                    CurrentSection.Nodes.Add(new ZeroNode(directive));
                    break;
                case "string":
                    CurrentSection.Nodes.Add(new StringNode(directive));
                    break;
                case "org":
                    CurrentSection.Nodes.Add(new OrgNode(directive, CurrentSection.LocationCounter, _memoryAddressValueProcessor));
                    break;
                default:
                    throw new AnalyserException($"Invalid directive: {directive.Directive}",
                        directive.Span.Line, directive.Span.StartColumn);
            }
        }

        private void HandleInstruction(InstructionNode instruction)
        {
            var mnemonic = instruction.Mnemonic;
            if (!Enum.TryParse<OpcodeBaseCode>(mnemonic, true, out var opcode))
            {
                throw new AnalyserException($"Invalid instruction mnemonic: {mnemonic}",
                    instruction.Span.Line, instruction.Span.StartColumn);
            }

            switch (opcode)
            {
                case OpcodeBaseCode.NOP:
                case OpcodeBaseCode.HLT:
                case OpcodeBaseCode.CLC:
                case OpcodeBaseCode.SEC:
                case OpcodeBaseCode.CLZ:
                case OpcodeBaseCode.SEZ:
                case OpcodeBaseCode.SEI:
                case OpcodeBaseCode.CLI:
                case OpcodeBaseCode.RET:
                case OpcodeBaseCode.RTI:
                    CurrentSection.Nodes.Add(new NoOperandNode(instruction, opcode));
                    break;
                case OpcodeBaseCode.JMP:
                case OpcodeBaseCode.JCC:
                case OpcodeBaseCode.JCS:
                case OpcodeBaseCode.JZC:
                case OpcodeBaseCode.JZS:
                case OpcodeBaseCode.CAL:
                    CurrentSection.Nodes.Add(new SingleMemoryAddressNode(instruction, opcode, _labelManager, _memoryAddressValueProcessor));
                    break;
                case OpcodeBaseCode.POP:
                case OpcodeBaseCode.PEK:
                case OpcodeBaseCode.PSH:
                case OpcodeBaseCode.LSH:
                case OpcodeBaseCode.RSH:
                case OpcodeBaseCode.LRT:
                case OpcodeBaseCode.RRT:
                case OpcodeBaseCode.INC:
                case OpcodeBaseCode.DEC:
                    CurrentSection.Nodes.Add(new SingleRegisterNode(instruction, opcode));
                    break;
                case OpcodeBaseCode.LDI:
                case OpcodeBaseCode.ADI:
                case OpcodeBaseCode.SBI:
                case OpcodeBaseCode.CPI:
                case OpcodeBaseCode.ANI:
                case OpcodeBaseCode.ORI:
                case OpcodeBaseCode.XRI:
                case OpcodeBaseCode.BTI:
                    CurrentSection.Nodes.Add(new RegisterAndImmediateNode(instruction, opcode, _labelManager));
                    break;
                case OpcodeBaseCode.LDA:
                case OpcodeBaseCode.STA:
                case OpcodeBaseCode.ADA:
                case OpcodeBaseCode.SBA:
                case OpcodeBaseCode.CPA:
                case OpcodeBaseCode.ANA:
                case OpcodeBaseCode.ORA:
                case OpcodeBaseCode.XRA:
                case OpcodeBaseCode.BTA:
                case OpcodeBaseCode.LDX:
                case OpcodeBaseCode.STX:
                    CurrentSection.Nodes.Add(new RegisterAndMemoryAddressNode(instruction, opcode, _labelManager, _memoryAddressValueProcessor));
                    break;
                case OpcodeBaseCode.MOV:
                case OpcodeBaseCode.ADD:
                case OpcodeBaseCode.SUB:
                case OpcodeBaseCode.CMP:
                case OpcodeBaseCode.AND:
                case OpcodeBaseCode.OR:
                case OpcodeBaseCode.XOR:
                    CurrentSection.Nodes.Add(new TwoRegisterNode(instruction, opcode));
                    break;
                default:
                    throw new AnalyserException($"Opcode handling not implemented for: {mnemonic}",
                        instruction.Span.Line, instruction.Span.StartColumn);
            }
        }

        private Section CurrentSection { get => _sections[_currentSectionIndex]; }

        private List<Section> _sections = [];
        private int _currentSectionIndex = 0;
        private int _irqSectionIndex = -1;
        private LabelReferenceManager _labelManager = new();
        private bool _analysisRan = false;
        private readonly MemoryAddressValueProcessor _memoryAddressValueProcessor;
        private readonly int _irqVectorAddress;
        private const int TextSectionIndex = 0;
        private const int DefaultStackSize = 16;
    }
}
