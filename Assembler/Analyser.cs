using Assembler.Analysis;
using Assembler.Analysis.Directives;
using Assembler.Analysis.Instructions;
using Assembler.AST;
using CPU.opcodes;

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
        private Section TextSection;
        private List<Section> DataSections;
        private Section currentSection;
        private int textSectionCounter;
        private LabelReferenceManager labelManager;

        public Analyser()
        {
            TextSection = new Section();
            DataSections = [];
            currentSection = TextSection;
            textSectionCounter = 0;
            labelManager = new LabelReferenceManager();
        }

        public IList<IAnalysisNode> Run(Parser.ProgramNode program)
        {
            TextSection = new Section();
            DataSections = [];
            currentSection = TextSection;
            textSectionCounter = 0;
            labelManager = new LabelReferenceManager();

            // First pass: analyse statements
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
            var sectionOffset = TextSection.LocationCounter;
            foreach (var dataSection in DataSections)
            {
                dataSection.StartAddress = sectionOffset;
                sectionOffset += dataSection.LocationCounter;
            }
            labelManager.ResolveLabels();

            // Collect all nodes in order
            var emitNodes = new List<IAnalysisNode>();
            emitNodes.AddRange(TextSection.Nodes);
            foreach (var dataSection in DataSections)
            {
                emitNodes.AddRange(dataSection.Nodes);
            }
            return emitNodes;
        }

        private void HandleStatement(StatementNode statement)
        {
            HandleHeaderDirective(statement);

            if (statement.HasLabel)
            {
                labelManager.LocateLabel(statement.GetLabel(), currentSection);
            }

            HandlePostDirective(statement);

            if (statement.HasInstruction)
            {
                var instruction = statement.GetInstruction();
                if (currentSection != TextSection)
                {
                    throw new AnalyserException("Instructions are only allowed in the text section", instruction.Span.Line, instruction.Span.StartColumn);
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
                        var section = new Section();
                        DataSections.Add(section);
                        currentSection = section;
                        break;
                    case "text":
                        if (textSectionCounter > 0)
                        {
                            throw new AnalyserException("Multiple text section directives are not allowed",
                                headerDirective.Span.Line, headerDirective.Span.StartColumn);
                        }
                        currentSection = TextSection;
                        textSectionCounter++;
                        break;
                    case "org":
                        currentSection.Nodes.Add(new OrgNode(headerDirective, currentSection.LocationCounter));
                        break;
                    default:
                        throw new AnalyserException($"Invalid header directive: {headerDirective.Directive}",
                                headerDirective.Span.Line, headerDirective.Span.StartColumn);
                }
            }
        }

        private void HandlePostDirective(StatementNode statement)
        {
            if (statement.HasPostDirective)
            {
                var postDirective = statement.GetPostDirective();
                if (currentSection == TextSection)
                {
                    throw new AnalyserException("Post directives are not allowed in the text section",
                        postDirective.Span.Line, postDirective.Span.StartColumn);
                }

                switch (postDirective.Directive)
                {
                    case "byte":
                        currentSection.Nodes.Add(new ByteNode(postDirective));
                        break;
                    case "short":
                        currentSection.Nodes.Add(new ShortNode(postDirective));
                        break;
                    case "zero":
                        currentSection.Nodes.Add(new ZeroNode(postDirective));
                        break;
                    case "string":
                        currentSection.Nodes.Add(new StringNode(postDirective));
                        break;
                    case "org":
                        currentSection.Nodes.Add(new OrgNode(postDirective, currentSection.LocationCounter));
                        break;
                    default:
                        throw new AnalyserException($"Invalid post-directive: {postDirective.Directive}",
                           postDirective.Span.Line, postDirective.Span.StartColumn);
                }
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
                case OpcodeBaseCode.RET:
                    currentSection.Nodes.Add(new NoOperandInstruction(instruction, opcode));
                    break;
                case OpcodeBaseCode.JMP:
                case OpcodeBaseCode.JCC:
                case OpcodeBaseCode.JCS:
                case OpcodeBaseCode.JZC:
                case OpcodeBaseCode.JZS:
                case OpcodeBaseCode.CAL:
                    currentSection.Nodes.Add(new SingleMemoryAddressInstruction(instruction, opcode, labelManager));
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
                    currentSection.Nodes.Add(new SingleRegisterInstruction(instruction, opcode));
                    break;
                case OpcodeBaseCode.LDI:
                case OpcodeBaseCode.ADI:
                case OpcodeBaseCode.SBI:
                case OpcodeBaseCode.CPI:
                case OpcodeBaseCode.ANI:
                case OpcodeBaseCode.ORI:
                case OpcodeBaseCode.XRI:
                case OpcodeBaseCode.BTI:
                    currentSection.Nodes.Add(new RegisterAndImmediate(instruction, opcode, labelManager));
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
                    currentSection.Nodes.Add(new RegisterAndMemoryAddressInstruction(instruction, opcode, labelManager));
                    break;
                case OpcodeBaseCode.MOV:
                case OpcodeBaseCode.ADD:
                case OpcodeBaseCode.SUB:
                case OpcodeBaseCode.CMP:
                case OpcodeBaseCode.AND:
                case OpcodeBaseCode.OR:
                case OpcodeBaseCode.XOR:
                    currentSection.Nodes.Add(new TwoRegisterInstruction(instruction, opcode));
                    break;
                default:
                    throw new AnalyserException($"Opcode handling not implemented for: {mnemonic}",
                        instruction.Span.Line, instruction.Span.StartColumn);
            }
        }
    }
}
