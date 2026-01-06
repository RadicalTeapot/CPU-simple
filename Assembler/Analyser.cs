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

    internal class LabelReference(string labelName)
    {
        public string LabelName { get; } = labelName;
        public List<LabelReferenceEmitNode> EmitNodes { get; } = [];
        public void Locate(Section section, int sectionLocationCounter)
        {
            if (isLocated)
            {
                throw new InvalidOperationException($"Label '{LabelName}' location counter has already been set.");
            }

            locationCounter = sectionLocationCounter;
            this.section = section;
            isLocated = true;
        }

        public void ResolveEmitNodes()
        {
            if (EmitNodes.Count == 0)
            {
                return; // No emit nodes to resolve
            }

            if (!isLocated)
            {
                var firstEmitNode = EmitNodes[0];
                throw new ParserException($"Label '{LabelName}' location counter has not been set.", 
                    firstEmitNode.LabelOperand.Span.Line, firstEmitNode.LabelOperand.Span.Start); // Show location of first reference
            }

            foreach (var emitNode in EmitNodes)
            {
                emitNode.Resolve((byte)(locationCounter + section?.StartAddress ?? 0));
            }
        }

        private int locationCounter;
        private Section? section;
        private bool isLocated = false;
    }

    public interface IEmitNode
    {
        int Count { get; }
        byte[] Emit();
    }

    internal class FillEmitNode(int count, byte fillValue) : IEmitNode
    {
        public int Count { get; } = count;
        public byte FillValue { get; } = fillValue;
        public byte[] Emit() => [.. Enumerable.Repeat(FillValue, Count)];
    }

    internal class LabelReferenceEmitNode(OperandNode labelOperand, byte offset = 0) : IEmitNode
    {
        public OperandNode LabelOperand { get; } = labelOperand;
        public byte Offset { get; } = offset;
        public int Count { get; } = 1;
        public void Resolve(byte value)
        {
            resolvedValue = (byte)(value + Offset);
            isResolved = true;
        }

        public byte[] Emit()
        {
            if (!isResolved)
            {
                throw new ParserException($"Label '{LabelOperand.Operand}' has not been resolved yet.", LabelOperand.Span.Line, LabelOperand.Span.Start);
            }
            return [resolvedValue];
        }

        private bool isResolved = false;
        private byte resolvedValue;
    }

    internal class DataEmitNode(byte[] data) : IEmitNode
    {
        public byte[] Data { get; } = data;
        public int Count { get; } = data.Length;
        public byte[] Emit() => Data;
    }

    internal class Section
    {
        public int LocationCounter => EmitNodes.Sum(node => node.Count);
        public int StartAddress { get; set; } = 0;
        public IList<IEmitNode> EmitNodes { get; } = [];
    }

    public class Analyser
    {
        private Section TextSection;
        private List<Section> DataSections;
        private Section currentSection;
        private int textSectionCounter;
        /// <summary>
        /// A mapping of label names to their corresponding label references.
        /// </summary>
        private Dictionary<string, LabelReference> labels;

        public Analyser()
        {
            TextSection = new Section();
            DataSections = [];
            currentSection = TextSection;
            textSectionCounter = 0;
            labels = [];
        }

        public IList<IEmitNode> Run(ProgramNode program)
        {
            TextSection = new Section();
            DataSections = [];
            currentSection = TextSection;
            textSectionCounter = 0;
            labels = [];

            // First pass: analyse statements
            var analysisExceptions = new List<AnalyserException>();
            foreach (var statement in program.Statements)
            {
                try
                {
                    AnalyseStatement(statement);
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
            foreach (var labelRef in labels.Values)
            {
                labelRef.ResolveEmitNodes();
            }

            // Collect all emit nodes in order
            var emitNodes = new List<IEmitNode>();
            emitNodes.AddRange(TextSection.EmitNodes);
            foreach (var dataSection in DataSections)
            {
                emitNodes.AddRange(dataSection.EmitNodes);
            }
            return emitNodes;
        }

        private void AnalyseStatement(StatementNode statement)
        {
            AnalyseHeaderDirective(statement);

            if (statement.Label != null)
            {
                if (!labels.TryGetValue(statement.Label.Label, out LabelReference? value))
                {
                    value = new LabelReference(statement.Label.Label);
                    labels[statement.Label.Label] = value;
                }

                try
                {
                    value.Locate(currentSection, currentSection.LocationCounter);
                }
                catch (InvalidOperationException ex)
                {
                    throw new AnalyserException(ex.Message, statement.Label.Span.Line, statement.Label.Span.Start);
                }
            }

            AnalysePostDirective(statement);

            if (statement.Instruction != null)
            {
                if (currentSection != TextSection)
                {
                    throw new AnalyserException("Instructions are only allowed in the text section", statement.Instruction.Span.Line, statement.Instruction.Span.Start);
                }
                HandleInstruction(statement.Instruction);
            }
        }
        
        private void AnalyseHeaderDirective(StatementNode statement)
        {
            if (statement.HeaderDirective != null)
            {
                switch (statement.HeaderDirective.Directive)
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
                                statement.HeaderDirective.Span.Line, statement.HeaderDirective.Span.Start);
                        }
                        currentSection = TextSection;
                        textSectionCounter++;
                        break;
                    case "org":
                        HandleOrgDirective(statement.HeaderDirective);
                        break;
                    default:
                        throw new AnalyserException($"Invalid header directive: {statement.HeaderDirective.Directive}",
                                statement.HeaderDirective.Span.Line, statement.HeaderDirective.Span.Start);
                }
            }
        }

        private void AnalysePostDirective(StatementNode statement)
        {
            if (statement.PostDirective != null)
            {
                if (currentSection == TextSection)
                {
                    throw new AnalyserException("Post directives are not allowed in the text section",
                        statement.PostDirective.Span.Line, statement.PostDirective.Span.Start);
                }

                var operands = statement.PostDirective.OperandNodes;
                switch (statement.PostDirective.Directive)
                {
                    case "byte":
                        if (operands.Count != 1 || operands[0].Type != OperandType.Immediate)
                        {
                            throw new AnalyserException("'byte' directive requires a single numeric operand",
                                statement.PostDirective.Span.Line, statement.PostDirective.Span.Start);
                        }
                        currentSection.EmitNodes.Add(new DataEmitNode([Convert.ToByte(operands[0].Operand)]));
                        break;
                    case "short":
                        if (operands.Count != 1 || operands[0].Type != OperandType.Immediate)
                        {
                            throw new AnalyserException("'byte' directive requires a single numeric operand",
                                statement.PostDirective.Span.Line, statement.PostDirective.Span.Start);
                        }
                        currentSection.EmitNodes.Add(new DataEmitNode(BitConverter.GetBytes(Convert.ToUInt16(operands[0].Operand))));
                        break;
                    case "zero":
                        if (operands.Count != 1 || operands[0].Type != OperandType.Immediate)
                        {
                            throw new AnalyserException("'zero' directive requires a single numeric operand",
                                statement.PostDirective.Span.Line, statement.PostDirective.Span.Start);
                        }
                        var zeroCount = Convert.ToInt32(operands[0].Operand);
                        currentSection.EmitNodes.Add(new FillEmitNode(zeroCount, 0x00));
                        break;
                    case "string":
                        if (operands.Count != 1 || operands[0].Type != OperandType.StringLiteral)
                        {
                            throw new AnalyserException("'string' directive requires a single string literal operand",
                                statement.PostDirective.Span.Line, statement.PostDirective.Span.Start);
                        }
                        var trimmedStr = operands[0].Operand.Trim('"'); // Remove surrounding quotes
                        var strBytes = System.Text.Encoding.ASCII.GetBytes(trimmedStr);
                        currentSection.EmitNodes.Add(new DataEmitNode([..strBytes, 0])); // Null-terminated
                        break;
                    case "org":
                        HandleOrgDirective(statement.PostDirective);
                        break;
                    case "data":
                    case "text":
                    default:
                        throw new AnalyserException($"Invalid post-directive: {statement.PostDirective.Directive}",
                           statement.PostDirective.Span.Line, statement.PostDirective.Span.Start);
                }
            }
        }
    
        private static int GetValidatedAddressValue(OperandNode operand)
        {
            var address = Convert.ToInt32(operand.Operand);
#if x16
            if (address < 0 || address > 0xFFFF)
            {
                throw new AnalyserException("Address value out of range for 16-bit architecture", operand.Span.Line, operand.Span.Start);
            }
#else
            if (address < 0 || address > 0xFF)
            {
                throw new AnalyserException("Address value out of range for 8-bit architecture", operand.Span.Line, operand.Span.Start);
            }
#endif
            return address;
        }

        private void HandleOrgDirective(DirectiveNode directiveNode)
        {
            var operands = directiveNode.OperandNodes;
            if (operands.Count == 0)
            {
                throw new AnalyserException("'org' directive requires an address operand",
                    directiveNode.Span.Line, directiveNode.Span.Start);
            }

            var addressOperand = operands.First();
            if (addressOperand.Type != OperandType.Immediate)
            {
                throw new AnalyserException("'org' directive requires a numeric address operand",
                    addressOperand.Span.Line, addressOperand.Span.Start);
            }

            var fillValueOperand = operands.Skip(1).FirstOrDefault();
            if (fillValueOperand != null && fillValueOperand.Type != OperandType.Immediate)
            {
                throw new AnalyserException("Optional fill value operand for 'org' directive must be a numeric immediate",
                    fillValueOperand.Span.Line, fillValueOperand.Span.Start);
            }

            var address = GetValidatedAddressValue(addressOperand);
            if (address < currentSection.LocationCounter)
            {
                throw new AnalyserException("'org' address cannot be less than the current location counter",
                    addressOperand.Span.Line, addressOperand.Span.Start);
            }

            var fillValue = fillValueOperand != null ? Convert.ToByte(fillValueOperand.Operand) : (byte)0x00;
            var bytesToFill = address - currentSection.LocationCounter;
            currentSection.EmitNodes.Add(new FillEmitNode(bytesToFill, fillValue));
        }

        private void HandleInstruction(InstructionNode instruction)
        {
            // Implementation for instruction handling goes here
            var mnemonic = instruction.Mnemonic;
            if (!Enum.TryParse<OpcodeBaseCode>(mnemonic, true, out var opcode))
            {
                throw new AnalyserException($"Invalid instruction mnemonic: {mnemonic}",
                    instruction.Span.Line, instruction.Span.Start);
            }

            var operands = instruction.OperandNodes;
            switch (opcode)
            {
                case OpcodeBaseCode.NOP:
                    currentSection.EmitNodes.Add(new DataEmitNode([(byte)OpcodeBaseCode.NOP]));
                    break;
                case OpcodeBaseCode.HLT:
                    currentSection.EmitNodes.Add(new DataEmitNode([(byte)OpcodeBaseCode.HLT]));
                    break;
                case OpcodeBaseCode.ADD:
                    if (operands.Count != 2 ||
                        operands[0].Type != OperandType.Register ||
                        operands[1].Type != OperandType.Register)
                    {
                        throw new AnalyserException("'ADD' instruction requires two register operands",
                            instruction.Span.Line, instruction.Span.Start);
                    }
                    var destReg = Convert.ToByte(operands[0].Operand) & 0x03;
                    var srcReg = Convert.ToByte(operands[1].Operand) & 0x03;
                    var opcodeValue = (byte)((byte)OpcodeBaseCode.ADD | (srcReg << 2) | destReg);
                    currentSection.EmitNodes.Add(new DataEmitNode([opcodeValue]));
                    break;
                case OpcodeBaseCode.ADI:
                    if (operands.Count != 2 ||
                        operands[0].Type != OperandType.Register ||
                        (operands[1].Type != OperandType.Immediate || operands[1].Type != OperandType.LabelReference))
                    {
                        throw new AnalyserException("'ADI' instruction requires a register and either an immediate or label operand",
                            instruction.Span.Line, instruction.Span.Start);
                    }
                    var regIdx = Convert.ToByte(operands[0].Operand) & 0x03;
                    var adiOpcodeValue = (byte)((byte)OpcodeBaseCode.ADI | regIdx);
                    if (operands[1].Type == OperandType.LabelReference)
                    {
                        var labelName = operands[1].Operand;
                        if (!labels.TryGetValue(labelName, out LabelReference? labelRef))
                        {
                            labelRef = new LabelReference(labelName);
                            labels[labelName] = labelRef;
                        }

                        var labelRefNode = new LabelReferenceEmitNode(operands[1]);
                        labelRef.EmitNodes.Add(labelRefNode);

                        currentSection.EmitNodes.Add(new DataEmitNode([adiOpcodeValue]));
                        currentSection.EmitNodes.Add(labelRefNode);
                    }
                    else
                    {
                        var immediateValue = Convert.ToByte(operands[1].Operand);
                        currentSection.EmitNodes.Add(new DataEmitNode([adiOpcodeValue, immediateValue]));
                    }
                    break;
                // Handle other opcodes similarly
                default:
                    throw new AnalyserException($"Opcode handling not implemented for: {mnemonic}",
                        instruction.Span.Line, instruction.Span.Start);
            }
        }
    }
}
