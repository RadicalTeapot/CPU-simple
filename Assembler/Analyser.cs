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
                    firstEmitNode.LabelRefNode.Span.Line, firstEmitNode.LabelRefNode.Span.StartColumn); // Show location of first reference
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

    internal class LabelReferenceEmitNode(LabelReferenceNode labelRefNode, byte offset = 0) : IEmitNode
    {
        public LabelReferenceNode LabelRefNode { get; } = labelRefNode;
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
                throw new ParserException($"Label '{LabelRefNode.Label}' has not been resolved yet.", LabelRefNode.Span.Line, LabelRefNode.Span.StartColumn);
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

        public IList<IEmitNode> Run(Parser.ProgramNode program)
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

            if (statement.HasLabel)
            {
                var labelNode = statement.GetLabel();
                if (!labels.TryGetValue(labelNode.Label, out LabelReference? value))
                {
                    value = new LabelReference(labelNode.Label);
                    labels[labelNode.Label] = value;
                }

                try
                {
                    value.Locate(currentSection, currentSection.LocationCounter);
                }
                catch (InvalidOperationException ex)
                {
                    throw new AnalyserException(ex.Message, labelNode.Span.Line, labelNode.Span.StartColumn);
                }
            }

            AnalysePostDirective(statement);

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
        
        private void AnalyseHeaderDirective(StatementNode statement)
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
                        HandleOrgDirective(headerDirective);
                        break;
                    default:
                        throw new AnalyserException($"Invalid header directive: {headerDirective.Directive}",
                                headerDirective.Span.Line, headerDirective.Span.StartColumn);
                }
            }
        }

        private void AnalysePostDirective(StatementNode statement)
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
                        if (!postDirective.HasSignature([OperandType.Immediate]))
                        {
                            throw new AnalyserException("'byte' directive requires a single numeric operand",
                                postDirective.Span.Line, postDirective.Span.StartColumn);
                        }
                        postDirective.GetOperands(out HexNumberNode byteOperand);
                        currentSection.EmitNodes.Add(new DataEmitNode([ParseHexByte(byteOperand.Value)]));
                        break;
                    case "short":
                        if (!postDirective.HasSignature([OperandType.Immediate]))
                        {
                            throw new AnalyserException("'byte' directive requires a single numeric operand",
                                postDirective.Span.Line, postDirective.Span.StartColumn);
                        }
                        postDirective.GetOperands(out HexNumberNode shortOperand);
                        currentSection.EmitNodes.Add(new DataEmitNode(BitConverter.GetBytes(ParseHexUShort(shortOperand.Value)))); // Little-endian
                        break;
                    case "zero":
                        if (!postDirective.HasSignature([OperandType.Immediate]))
                        {
                            throw new AnalyserException("'zero' directive requires a single numeric operand",
                                postDirective.Span.Line, postDirective.Span.StartColumn);
                        }
                        postDirective.GetOperands(out HexNumberNode zeroCountOperand);
                        var zeroCount = ParseHexNumber(zeroCountOperand.Value);
                        currentSection.EmitNodes.Add(new FillEmitNode(zeroCount, 0x00));
                        break;
                    case "string":
                        if (!postDirective.HasSignature([OperandType.StringLiteral]))
                        {
                            throw new AnalyserException("'string' directive requires a single string literal operand",
                                postDirective.Span.Line, postDirective.Span.StartColumn);
                        }
                        postDirective.GetOperands(out StringLiteralNode stringLiteral);
                        var processedStr = ProcessString(stringLiteral.Value);
                        var strBytes = System.Text.Encoding.ASCII.GetBytes(processedStr);
                        currentSection.EmitNodes.Add(new DataEmitNode([..strBytes, 0x00])); // Null-terminated
                        break;
                    case "org":
                        HandleOrgDirective(postDirective);
                        break;
                    case "data":
                    case "text":
                    default:
                        throw new AnalyserException($"Invalid post-directive: {postDirective.Directive}",
                           postDirective.Span.Line, postDirective.Span.StartColumn);
                }
            }
        }
    
        private static int GetValidatedAddressValue(HexNumberNode hexNumber)
        {
            var address = ParseHexNumber(hexNumber.Value);
#if x16
            if (address < 0 || address > 0xFFFF)
            {
                throw new AnalyserException("Address value out of range for 16-bit architecture", hexNumber.Span.Line, hexNumber.Span.StartColumn);
            }
#else
            if (address < 0 || address > 0xFF)
            {
                throw new AnalyserException("Address value out of range for 8-bit architecture", hexNumber.Span.Line, hexNumber.Span.StartColumn);
            }
#endif
            return address;
        }

        private void HandleOrgDirective(DirectiveNode directiveNode)
        {
            byte fillValue;
            int address;
            if (directiveNode.HasSignature([OperandType.Immediate]))
            {
                directiveNode.GetOperands(out HexNumberNode addressOperand);
                address = GetValidatedAddressValue(addressOperand);
                fillValue = 0x00;
            }
            else if (directiveNode.HasSignature([OperandType.Immediate, OperandType.Immediate]))
            {
                directiveNode.GetOperands(out HexNumberNode addressOperand, out HexNumberNode fillValueOperand);
                address = GetValidatedAddressValue(addressOperand);
                fillValue = ParseHexByte(fillValueOperand.Value);
            }
            else
            {
                throw new AnalyserException("'org' directive requires an address operand",
                    directiveNode.Span.Line, directiveNode.Span.StartColumn);
            }

            var bytesToFill = address - currentSection.LocationCounter;
            currentSection.EmitNodes.Add(new FillEmitNode(bytesToFill, fillValue));
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
                    if (!instruction.HasSignature([]))
                    {
                        throw new AnalyserException("'NOP' instruction does not take any operands",
                            instruction.Span.Line, instruction.Span.StartColumn);
                    }
                    currentSection.EmitNodes.Add(new DataEmitNode([(byte)OpcodeBaseCode.NOP]));
                    break;
                case OpcodeBaseCode.HLT:
                    if (!instruction.HasSignature([]))
                    {
                        throw new AnalyserException("'HLT' instruction does not take any operands",
                            instruction.Span.Line, instruction.Span.StartColumn);
                    }
                    currentSection.EmitNodes.Add(new DataEmitNode([(byte)OpcodeBaseCode.HLT]));
                    break;
                case OpcodeBaseCode.ADD:
                    if (!instruction.HasSignature([OperandType.Register, OperandType.Register]))
                    {
                        throw new AnalyserException("'ADD' instruction requires two register operands",
                            instruction.Span.Line, instruction.Span.StartColumn);
                    }
                    instruction.GetOperands(out RegisterNode firstOperand, out RegisterNode secondOperand);
                    var destReg = Convert.ToByte(firstOperand.RegisterName) & 0x03;
                    var srcReg = Convert.ToByte(secondOperand.RegisterName) & 0x03;
                    var opcodeValue = (byte)((byte)OpcodeBaseCode.ADD | (srcReg << 2) | destReg);
                    currentSection.EmitNodes.Add(new DataEmitNode([opcodeValue]));
                    break;
                case OpcodeBaseCode.ADI:
                    if (instruction.HasSignature([OperandType.Register, OperandType.Immediate])) 
                    {
                        instruction.GetOperands(out RegisterNode registerOperand, out HexNumberNode immediateOperand);
                        var regIdx = Convert.ToByte(registerOperand.RegisterName) & 0x03;
                        var adiOpcodeValue = (byte)((byte)OpcodeBaseCode.ADI | regIdx);
                        var immediateValue = ParseHexByte(immediateOperand.Value);
                        currentSection.EmitNodes.Add(new DataEmitNode([adiOpcodeValue, immediateValue]));
                    }
                    else if (instruction.HasSignature([OperandType.Register, OperandType.LabelReference])) 
                    {
                        instruction.GetOperands(out RegisterNode registerOperand, out LabelReferenceNode labelReferenceOperand);
                        var regIdx = Convert.ToByte(registerOperand.RegisterName) & 0x03;
                        var adiOpcodeValue = (byte)((byte)OpcodeBaseCode.ADI | regIdx);
                        var labelName = labelReferenceOperand.Label;
                        if (!labels.TryGetValue(labelName, out LabelReference? labelRef))
                        {
                            labelRef = new LabelReference(labelName);
                            labels[labelName] = labelRef;
                        }

                        var labelRefNode = new LabelReferenceEmitNode(labelReferenceOperand);
                        labelRef.EmitNodes.Add(labelRefNode);

                        currentSection.EmitNodes.Add(new DataEmitNode([adiOpcodeValue]));
                        currentSection.EmitNodes.Add(labelRefNode);
                    }
                    else
                    {
                        throw new AnalyserException("'ADI' instruction requires a register and either an immediate or label operand",
                            instruction.Span.Line, instruction.Span.StartColumn);
                    }
                    break;
                // Handle other opcodes similarly
                default:
                    throw new AnalyserException($"Opcode handling not implemented for: {mnemonic}",
                        instruction.Span.Line, instruction.Span.StartColumn);
            }
        }

        /// <summary>
        /// Processes string literals.
        /// </summary>
        /// <param name="input">The raw string content (without surrounding quotes)</param>
        /// <returns>The processed string with surrounding quotes removed and escape sequences replaced</returns>
        /// <remarks>Supports \\ (backslash) and \" (double quote) escape sequences.</remarks>
        private static string ProcessString(string input)
        {
            var trimmedInput = input.Trim('"');
            var result = new System.Text.StringBuilder(trimmedInput.Length);
            for (int i = 0; i < trimmedInput.Length; i++)
            {
                if (trimmedInput[i] == '\\' && i + 1 < trimmedInput.Length)
                {
                    var nextChar = trimmedInput[i + 1];
                    if (nextChar == '\\')
                    {
                        result.Append('\\');
                        i++; // Skip next character
                        continue;
                    }
                    else if (nextChar == '"')
                    {
                        result.Append('"');
                        i++; // Skip next character
                        continue;
                    }
                }
                result.Append(trimmedInput[i]);
            }
            return result.ToString();
        }

        /// <summary>
        /// Parses a hex number string that has a 0x prefix.
        /// </summary>
        /// <param name="hexString">The hex string</param>
        /// <returns>The parsed integer value</returns>
        private static int ParseHexNumber(string hexString)
        {
            return Convert.ToInt32(hexString[2..], 16);
        }

        /// <summary>
        /// Parses a hex number string to a byte that has a 0x prefix.
        /// </summary>
        /// <param name="hexString">The hex string</param>
        /// <returns>The parsed byte value</returns>
        private static byte ParseHexByte(string hexString)
        {
            return Convert.ToByte(hexString[2..], 16);
        }

        /// <summary>
        /// Parses a hex number string to a ushort that has a 0x prefix.
        /// </summary>
        /// <param name="hexString">The hex string</param>
        /// <returns>The parsed ushort value</returns>
        private static ushort ParseHexUShort(string hexString)
        {
            return Convert.ToUInt16(hexString[2..], 16);
        }
    }
}
