using Assembler.Lexeme;

namespace Assembler.AST
{
    public class StatementNode(int tokenCount, NodeSpan span) : BaseNode(span)
    {
        public int TokenCount { get; } = tokenCount;

        public bool HasHeaderDirective { get => _headerDirective != null; }
        public DirectiveNode GetHeaderDirective()
        {
            if (!HasHeaderDirective || _headerDirective == null)
            {
                throw new ParserException("Statement has not header directive.", Span.Line, Span.StartColumn);
            }
            return _headerDirective;
        }

        public bool HasLabel { get => _label != null; }
        public LabelNode GetLabel()
        {
            if (!HasLabel || _label == null)
            {
                throw new ParserException("Statement has no label.", Span.Line, Span.StartColumn);
            }
            return _label;
        }

        public bool HasPostDirective { get => _postDirective != null; }
        public DirectiveNode GetPostDirective()
        {
            if (!HasPostDirective || _postDirective == null)
            {
                throw new ParserException("Statement has no post directive.", Span.Line, Span.StartColumn);
            }
            return _postDirective;
        }

        public bool HasInstruction { get => _instruction != null; }
        public InstructionNode GetInstruction()
        {
            if (!HasInstruction || _instruction == null)
            {
                throw new ParserException("Statement has not instruction.", Span.Line, Span.StartColumn);
            }
            return _instruction;
        }

        public static StatementNode CreateFromTokens(IList<Token> tokens, int index)
        {
            var startColumn = tokens[index].Column;
            var tokenCount = 0;

            DirectiveNode? headerDirective = null;
            if (DirectiveNode.IsValidDirectiveAtIndex(tokens, index))
            {
                headerDirective = DirectiveNode.CreateFromTokens(tokens, index);
                index += headerDirective.TokenCount;
                tokenCount += headerDirective.TokenCount;
            }

            LabelNode? label = null;
            if (LabelNode.IsValidLabelAtIndex(tokens, index))
            {
                label = LabelNode.CreateFromTokens(tokens, index);
                index += LabelNode.TokenCount;
                tokenCount += LabelNode.TokenCount;
            }

            var hasPostDirective = DirectiveNode.IsValidDirectiveAtIndex(tokens, index);
            var hasInstruction = InstructionNode.IsValidInstructionAtIndex(tokens, index);
            if (hasPostDirective && hasInstruction)
            {
                throw new ParserException("Statement cannot contain both a post-directive and an instruction.", tokens[index].Line, startColumn);
            }

            DirectiveNode? postDirective = null;
            InstructionNode? instruction = null;
            if (hasPostDirective)
            {
                postDirective = DirectiveNode.CreateFromTokens(tokens, index);
                index += postDirective.TokenCount;
                tokenCount += postDirective.TokenCount;
            }
            else if (hasInstruction)
            {
                instruction = InstructionNode.CreateFromTokens(tokens, index);
                index += instruction.TokenCount;
                tokenCount += instruction.TokenCount;
            }

            if (tokens[index].Type != TokenType.EndOfLine)
            {
                throw new ParserException("Expected end of statement (newline).", tokens[index].Line, tokens[index].Column);
            }

            var statementSpan = new NodeSpan(startColumn, tokens[index].Column, tokens[index].Line);
            return new StatementNode(tokenCount, statementSpan)
            {
                _headerDirective = headerDirective,
                _label = label,
                _postDirective = postDirective,
                _instruction = instruction,
            };
        }

        private DirectiveNode? _headerDirective;
        private LabelNode? _label;
        private DirectiveNode? _postDirective;
        private InstructionNode? _instruction;
    }
}
