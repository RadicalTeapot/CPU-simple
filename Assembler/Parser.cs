using Assembler.Lexeme;
using System.Diagnostics;

namespace Assembler
{
    public class ProgramNode(IList<StatementNode> statements)
    {
        public IList<StatementNode> Statements { get; } = statements;
    }

    public readonly struct NodeSpan(int start, int end)
    {
        public int Start { get; } = start;
        public int End { get; } = end;
    }

    public class StatementNode(LabelNode? label, DirectiveNode? directive, InstructionNode? instruction, NodeSpan nodeSpan)
    {
        public LabelNode? Label { get; } = label;
        public DirectiveNode? Directive { get; } = directive;
        public InstructionNode? Instruction { get; } = instruction;
        public NodeSpan Span { get; } = nodeSpan;
    }

    public class LabelNode(string label, NodeSpan span)
    {
        public string Label { get; } = label;
        public NodeSpan Span { get; } = span;
    }

    public class DirectiveNode(string directive, IList<OperandNode> operandNodes, NodeSpan span)
    {
        public string Directive { get; } = directive;
        public IList<OperandNode> OperandNodes { get; } = operandNodes;
        public NodeSpan Span { get; } = span;
    }

    public class InstructionNode(string mnemonic, IList<OperandNode> operandNodes, NodeSpan span)
    {
        public string Mnemonic { get; } = mnemonic;
        public IList<OperandNode> OperandNodes { get; } = operandNodes;
        public NodeSpan Span { get; } = span;
    }

    public enum OperandType
    {
        Register,
        Immediate,
        LabelReference,
        MemoryAddress
    }

    public class OperandNode(string operand, OperandType type, NodeSpan span)
    {
        public string Operand { get; } = operand;
        public OperandType Type { get; } = type;
        public NodeSpan Span { get; } = span;
    }

    public class Parser
    {
        public static ProgramNode ParseProgram(IList<Token> tokens)
        {
            var currentTokenIndex = 0;
            var statements = new List<StatementNode>();
            var parsingErrors = new List<Exception>();
            while (currentTokenIndex < tokens.Count && tokens[currentTokenIndex].Type != TokenType.EndOfFile)
            {
                try
                {
                    // Skip any EndOfLine tokens between statements
                    while (currentTokenIndex < tokens.Count && tokens[currentTokenIndex].Type == TokenType.EndOfLine)
                    {
                        currentTokenIndex++;
                    }

                    if (currentTokenIndex < tokens.Count && tokens[currentTokenIndex].Type != TokenType.EndOfFile)
                    {
                        statements.Add(ParseStatement(tokens, ref currentTokenIndex));
                    }
                }
                catch (Exception ex)
                {
                    parsingErrors.Add(ex);
                    // Attempt to recover by skipping to the next EndOfLine or EndOfFile
                    while (currentTokenIndex < tokens.Count && tokens[currentTokenIndex].Type != TokenType.EndOfLine && tokens[currentTokenIndex].Type != TokenType.EndOfFile)
                    {
                        currentTokenIndex++;
                    }
                }
            }

            if (tokens[currentTokenIndex].Type != TokenType.EndOfFile)
            {
                parsingErrors.Add(new Exception("Expected end of file token.")); // TODO Custom exception
            }

            if (parsingErrors.Count > 0)
            {
                throw new AggregateException("Parsing failed with errors.", parsingErrors);
            }

            return new ProgramNode(statements);
        }

        private static StatementNode ParseStatement(IList<Token> tokens, ref int currentTokenIndex)
        {
            var startIndex = currentTokenIndex;
            var _ = TryParseLabel(tokens, ref currentTokenIndex, out var labelNode);
            var hasDirective = TryParseDirective(tokens, ref currentTokenIndex, out var directiveNode);
            var hasInstruction = TryParseInstruction(tokens, ref currentTokenIndex, out var instructionNode);

            if (hasDirective && hasInstruction)
            {
                throw new Exception("Statement cannot contain both a directive and an instruction."); // TODO Custom exception
            }

            if (tokens[currentTokenIndex].Type != TokenType.EndOfLine)
            {
                throw new Exception("Expected end of statement (newline)."); // TODO Custom exception
            }

            return new StatementNode(labelNode, directiveNode, instructionNode, new NodeSpan(startIndex, currentTokenIndex));
        }

        private static bool TryParseLabel(IList<Token> tokens, ref int currentTokenIndex, out LabelNode? labelNode)
        {
            var startIndex = currentTokenIndex;
            if (tokens[currentTokenIndex].Type == TokenType.Identifier)
            {
                var identifierToken = tokens[currentTokenIndex];
                currentTokenIndex++;
                if (currentTokenIndex < tokens.Count && tokens[currentTokenIndex].Type == TokenType.Colon)
                {
                    currentTokenIndex++;
                    labelNode = new LabelNode(identifierToken.Lexeme, new NodeSpan(startIndex, currentTokenIndex));
                    return true;
                }
            }

            // If parsing failed, reset the index
            currentTokenIndex = startIndex;
            labelNode = default;
            return false;
        }

        private static bool TryParseDirective(IList<Token> tokens, ref int currentTokenIndex, out DirectiveNode? directiveNode)
        {
            var startIndex = currentTokenIndex;
            if (tokens[currentTokenIndex].Type == TokenType.Dot)
            {
                currentTokenIndex++;
                if (currentTokenIndex < tokens.Count && tokens[currentTokenIndex].Type == TokenType.Identifier)
                {
                    var directiveToken = tokens[currentTokenIndex];
                    currentTokenIndex++;
                    // TODO Further parsing of directive arguments can be done here based on directiveToken.Lexeme
                    directiveNode = new DirectiveNode(directiveToken.Lexeme, [], new NodeSpan(startIndex, currentTokenIndex));
                    return true;
                }
            }

            // If parsing failed, reset the index
            currentTokenIndex = startIndex;
            directiveNode = default;
            return false;
        }

        private static bool TryParseInstruction(IList<Token> tokens, ref int currentTokenIndex, out InstructionNode? instructionNode)
        {
            var startIndex = currentTokenIndex;
            if (tokens[currentTokenIndex].Type == TokenType.Identifier)
            {
                var mnemonicToken = tokens[currentTokenIndex];
                var operands = new List<OperandNode>();
                currentTokenIndex++;
                if (TryParseOperand(tokens, ref currentTokenIndex, out var firstArgumentToken))
                {
                    Debug.Assert(firstArgumentToken != null, "firstArgumentToken should not be null here.");
                    operands.Add(firstArgumentToken);

                    // Check for a comma, indicating a second operand
                    if (tokens[currentTokenIndex].Type == TokenType.Comma)
                    {
                        currentTokenIndex++;
                        if (TryParseOperand(tokens, ref currentTokenIndex, out var secondArgumentToken))
                        {
                            Debug.Assert(secondArgumentToken != null, "secondArgumentToken should not be null here.");
                            operands.Add(secondArgumentToken);
                        }
                        else
                        {
                            throw new Exception("Expected second operand after comma."); // TODO Custom exception
                        }
                    }
                }
                instructionNode = new InstructionNode(mnemonicToken.Lexeme, operands, new NodeSpan(startIndex, currentTokenIndex));
                return true;
            }

            // If parsing failed, reset the index
            currentTokenIndex = startIndex;
            instructionNode = default;
            return false;
        }

        private static bool TryParseOperand(IList<Token> tokens, ref int currentTokenIndex, out OperandNode? operandToken)
        {
            // Either register, immediate value, label ref or memory address
            var startIndex = currentTokenIndex;
            if (tokens[currentTokenIndex].Type == TokenType.Register)
            {
                var registerToken = tokens[currentTokenIndex];
                currentTokenIndex++;
                operandToken = new OperandNode(registerToken.Lexeme, OperandType.Register, new NodeSpan(startIndex, currentTokenIndex));
                return true;
            }
            else if (tokens[currentTokenIndex].Type == TokenType.Hash)
            {
                currentTokenIndex++;
                if (currentTokenIndex >= tokens.Count || tokens[currentTokenIndex].Type != TokenType.HexNumber)
                {
                    throw new Exception("Expected immediate value after '#' symbol."); // TODO Custom exception
                }
                var immediateToken = tokens[currentTokenIndex];
                currentTokenIndex++;
                operandToken = new OperandNode(immediateToken.Lexeme, OperandType.Immediate, new NodeSpan(startIndex, currentTokenIndex));
                return true;
            }
            else if (tokens[currentTokenIndex].Type == TokenType.Identifier)
            {
                var labelRefToken = tokens[currentTokenIndex];
                currentTokenIndex++;
                operandToken = new OperandNode(labelRefToken.Lexeme, OperandType.LabelReference, new NodeSpan(startIndex, currentTokenIndex));
                return true;
            }
            else if (tokens[currentTokenIndex].Type == TokenType.LeftSquareBracket)
            {
                currentTokenIndex++;
                if (currentTokenIndex < tokens.Count && tokens[currentTokenIndex].Type == TokenType.Identifier)
                {
                    var addressToken = tokens[currentTokenIndex];
                    currentTokenIndex++;
                    if (currentTokenIndex < tokens.Count && tokens[currentTokenIndex].Type == TokenType.RightSquareBracket)
                    {
                        // TODO Offset parsing can be added here
                        currentTokenIndex++;
                        operandToken = new OperandNode(addressToken.Lexeme, OperandType.MemoryAddress, new NodeSpan(startIndex, currentTokenIndex));
                        return true;
                    }
                }
            }
            // If parsing failed, reset the index
            currentTokenIndex = startIndex;
            operandToken = default;
            return false;
        }
    }
}
