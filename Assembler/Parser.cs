using Assembler.Lexeme;
using System.Diagnostics;

namespace Assembler
{
    public class ParserException : Exception
    {
        public int Line { get; }
        public int Column { get; }
        public ParserException(string message, int line, int column)
            : base($"{message} at line {line}, column {column}")
        {
            Line = line;
            Column = column;
        }
    }

    public class ProgramNode(IList<StatementNode> statements)
    {
        public IList<StatementNode> Statements { get; } = statements;
    }

    public readonly struct NodeSpan(int start, int end, int line)
    {
        public int Start { get; } = start;
        public int End { get; } = end;
        public int Line { get; } = line;
    }

    public class StatementNode(DirectiveNode? headerDirective, LabelNode? label, DirectiveNode? postDirective, InstructionNode? instruction, NodeSpan nodeSpan)
    {
        public DirectiveNode? HeaderDirective { get; } = headerDirective;
        public LabelNode? Label { get; } = label;
        public DirectiveNode? PostDirective { get; } = postDirective;
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

    public class OperandNode(string operand, OperandType type, NodeSpan span, string? offset = null)
    {
        public string Operand { get; } = operand;
        public string? Offset { get; } = offset; // Only valid for memory operands
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
                parsingErrors.Add(new ParserException("Expected end of file token.", tokens[currentTokenIndex].Line, tokens[currentTokenIndex].Column));
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
            var _ = TryParseDirective(tokens, ref currentTokenIndex, out var headerDirective);
            _ = TryParseLabel(tokens, ref currentTokenIndex, out var labelNode);
            var hasDirective = TryParseDirective(tokens, ref currentTokenIndex, out var postDirective);
            var hasInstruction = TryParseInstruction(tokens, ref currentTokenIndex, out var instructionNode);

            if (hasDirective && hasInstruction)
            {
                throw new ParserException("Statement cannot contain both a post-directive and an instruction.", tokens[startIndex].Line, tokens[startIndex].Column);
            }

            if (tokens[currentTokenIndex].Type != TokenType.EndOfLine)
            {
                throw new ParserException("Expected end of statement (newline).", tokens[currentTokenIndex].Line, tokens[currentTokenIndex].Column);
            }

            return new StatementNode(headerDirective, labelNode, postDirective, instructionNode, new NodeSpan(
                tokens[startIndex].Column, tokens[currentTokenIndex].Column, tokens[startIndex].Line));
        }

        private static bool TryParseLabel(IList<Token> tokens, ref int currentTokenIndex, out LabelNode? labelNode)
        {
            if (IsValidLabelAtIndex(tokens, currentTokenIndex))
            {
                var startIndex = currentTokenIndex;
                var identifierToken = tokens[currentTokenIndex];
                currentTokenIndex += 2;
                labelNode = new LabelNode(identifierToken.Lexeme, new NodeSpan(
                    tokens[startIndex].Column, tokens[currentTokenIndex].Column, tokens[startIndex].Line));
                return true;
            }

            labelNode = default;
            return false;
        }

        private static bool IsValidLabelAtIndex(IList<Token> tokens, int index)
        {
            if (tokens.Count > index + 1 
                && tokens[index].Type != TokenType.Identifier 
                && tokens[index + 1].Type == TokenType.Colon)
            {
                throw new ParserException("Invalid label syntax.", tokens[index].Line, tokens[index].Column);
            }

            return tokens.Count > index + 1
                && tokens[index].Type == TokenType.Identifier
                && tokens[index + 1].Type == TokenType.Colon; 
        }

        private static bool TryParseDirective(IList<Token> tokens, ref int currentTokenIndex, out DirectiveNode? directiveNode)
        {
            if (IsValidDirectiveAtIndex(tokens, currentTokenIndex))
            {
                var startIndex = currentTokenIndex;
                var directiveToken = tokens[currentTokenIndex+1];
                currentTokenIndex += 2;
                // TODO Further parsing of directive arguments can be done here based on directiveToken.Lexeme
                directiveNode = new DirectiveNode(directiveToken.Lexeme, [], new NodeSpan(
                    tokens[startIndex].Column, tokens[currentTokenIndex].Column, tokens[startIndex].Line));
                return true;
            }

            directiveNode = default;
            return false;
        }

        private static bool IsValidDirectiveAtIndex(IList<Token> tokens, int index)
        {
            if (tokens[index].Type == TokenType.Dot 
                && (index + 1 >= tokens.Count ||
                tokens[index + 1].Type != TokenType.Identifier)
            )
            {
                throw new ParserException("Invalid directive syntax.", tokens[index].Line, tokens[index].Column);
            }

            return tokens.Count > index + 1
                && tokens[index].Type == TokenType.Dot
                && tokens[index + 1].Type == TokenType.Identifier;
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
                            throw new ParserException("Expected second operand after comma.", tokens[currentTokenIndex].Line, tokens[currentTokenIndex].Column);
                        }
                    }
                }
                instructionNode = new InstructionNode(mnemonicToken.Lexeme, operands, new NodeSpan(
                    tokens[startIndex].Column, tokens[currentTokenIndex].Column, tokens[startIndex].Line));
                return true;
            }

            // If parsing failed, reset the index
            currentTokenIndex = startIndex;
            instructionNode = default;
            return false;
        }

        private static bool TryParseOperand(IList<Token> tokens, ref int currentTokenIndex, out OperandNode? operandToken)
        {
            if (TryParseRegister(tokens, ref currentTokenIndex, out operandToken))
            {
                return true;
            }
            else if (TryParseImmediateValue(tokens, ref currentTokenIndex, out operandToken))
            {
                return true;
            }
            else if (TryParseLabelReference(tokens, ref currentTokenIndex, out operandToken))
            {
                return true;
            }
            else if (TryParseMemoryOperand(tokens, ref currentTokenIndex, out operandToken))
            {
                return true;
            }
            
            operandToken = default;
            return false;
        }

        private static bool TryParseRegister(IList<Token> tokens, ref int currentTokenIndex, out OperandNode? operandToken)
        {
            if (tokens[currentTokenIndex].Type == TokenType.Register)
            {
                var startIndex = currentTokenIndex;
                var registerToken = tokens[currentTokenIndex];
                currentTokenIndex++;
                operandToken = new OperandNode(registerToken.Lexeme, OperandType.Register, new NodeSpan(
                    tokens[startIndex].Column, tokens[currentTokenIndex].Column, tokens[startIndex].Line));
                return true;
            }

            operandToken = default;
            return false;
        }

        private static bool TryParseImmediateValue(IList<Token> tokens, ref int currentTokenIndex, out OperandNode? operandToken)
        {
            if (IsValidHexNumberAtIndex(tokens, currentTokenIndex))
            {
                var startIndex = currentTokenIndex;
                var immediateToken = tokens[currentTokenIndex + 1];
                currentTokenIndex += 2;
                operandToken = new OperandNode(immediateToken.Lexeme, OperandType.Immediate, new NodeSpan(
                    tokens[startIndex].Column, tokens[currentTokenIndex].Column, tokens[startIndex].Line));
                return true;
            }

            operandToken = default;
            return false;
        }

        private static bool IsValidHexNumberAtIndex(IList<Token> tokens, int index)
        {
            if (tokens[index].Type == TokenType.Hash 
                && (index + 1 >= tokens.Count ||
                tokens[index + 1].Type != TokenType.HexNumber)
            )
            {
                throw new ParserException("Invalid immediate value syntax.", tokens[index].Line, tokens[index].Column);
            }

            return tokens.Count > index + 1
                && tokens[index].Type == TokenType.Hash
                && tokens[index + 1].Type == TokenType.HexNumber;
        }

        private static bool TryParseLabelReference(IList<Token> tokens, ref int currentTokenIndex, out OperandNode? operandToken)
        {
            if (tokens[currentTokenIndex].Type == TokenType.Identifier)
            {
                var startIndex = currentTokenIndex;
                var identifierToken = tokens[currentTokenIndex];
                currentTokenIndex++;
                operandToken = new OperandNode(identifierToken.Lexeme, OperandType.LabelReference, new NodeSpan(
                    tokens[startIndex].Column, tokens[currentTokenIndex].Column, tokens[startIndex].Line));
                return true;
            }
            operandToken = default;
            return false;
        }

        private static bool TryParseMemoryOperand(IList<Token> tokens, ref int currentTokenIndex, out OperandNode? operandToken)
        {
            if (tokens[currentTokenIndex].Type == TokenType.LeftSquareBracket)
            {
                currentTokenIndex++;
                if (IsValidHexNumberAtIndex(tokens, currentTokenIndex))
                {
                    var startIndex = currentTokenIndex;
                    var immediateToken = tokens[currentTokenIndex + 1];
                    currentTokenIndex += 2;
                    operandToken = new OperandNode(string.Empty, OperandType.MemoryAddress, new NodeSpan(
                        tokens[startIndex].Column, tokens[currentTokenIndex].Column, tokens[startIndex].Line), immediateToken.Lexeme);
                }
                else if (tokens[currentTokenIndex].Type == TokenType.Identifier)
                {
                    var startIndex = currentTokenIndex;
                    var identifierToken = tokens[currentTokenIndex];
                    currentTokenIndex++;
                    if (tokens[currentTokenIndex].Type == TokenType.Plus || tokens[currentTokenIndex].Type == TokenType.Minus)
                    {
                        currentTokenIndex++;
                        if (IsValidHexNumberAtIndex(tokens, currentTokenIndex))
                        {
                            var immediateToken = tokens[currentTokenIndex + 1];
                            currentTokenIndex += 2;
                            operandToken = new OperandNode(identifierToken.Lexeme, OperandType.MemoryAddress, new NodeSpan(
                                tokens[startIndex].Column, tokens[currentTokenIndex].Column, tokens[startIndex].Line), immediateToken.Lexeme);
                        }
                        else
                        {
                            throw new ParserException($"Expected token {tokens[currentTokenIndex]} for offset value", tokens[currentTokenIndex].Line, tokens[currentTokenIndex].Column);
                        }
                    }
                    else
                    {
                        operandToken = new OperandNode(identifierToken.Lexeme, OperandType.MemoryAddress, new NodeSpan(
                            tokens[startIndex].Column, tokens[currentTokenIndex].Column, tokens[startIndex].Line));
                    }
                }
                else
                {
                    throw new ParserException($"Unexpected token {tokens[currentTokenIndex]} for memory address", tokens[currentTokenIndex].Line, tokens[currentTokenIndex].Column);
                }

                if (tokens[currentTokenIndex].Type == TokenType.RightSquareBracket)
                {
                    return true;
                }
            }

            operandToken = default;
            return false;
        }
    }
}