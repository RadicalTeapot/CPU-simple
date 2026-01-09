using Assembler.Lexeme;
using System.Diagnostics;

namespace Assembler.AST
{
    public class StatementNode(int tokenCount, NodeSpan span) : BaseNode(span)
    {
        public int TokenCount { get; } = tokenCount;

        public bool HasHeaderDirective { get => _headerDirective != null; }
        public DirectiveNode GetHeaderDirective()
        {
            if (!HasHeaderDirective)
            {
                throw new ParserException("Statement has no header directive.", Span.Line, Span.StartColumn);
            }
            Debug.Assert(_headerDirective != null);
            return _headerDirective;
        }

        public bool HasLabel { get => _label != null; }
        public LabelNode GetLabel()
        {
            if (!HasLabel)
            {
                throw new ParserException("Statement has no label.", Span.Line, Span.StartColumn);
            }
            Debug.Assert(_label != null);
            return _label;
        }

        public bool HasPostDirective { get => _postDirective != null; }
        public DirectiveNode GetPostDirective()
        {
            if (!HasPostDirective)
            {
                throw new ParserException("Statement has no post directive.", Span.Line, Span.StartColumn);
            }
            Debug.Assert(_postDirective != null);
            return _postDirective;
        }

        public bool HasInstruction { get => _instruction != null; }
        public InstructionNode GetInstruction()
        {
            if (!HasInstruction)
            {
                throw new ParserException("Statement has no instruction.", Span.Line, Span.StartColumn);
            }
            Debug.Assert(_instruction != null);
            return _instruction;
        }

        public static StatementNode CreateFromTokens(IList<Token> tokens, int index)
        {
            var startColumn = tokens[index].Column;
            var tokenCount = 0;

            DirectiveNode? headerDirective = null;
            if (tokens.Count > index && DirectiveNode.IsValidDirectiveAtIndex(tokens, index))
            {
                headerDirective = DirectiveNode.CreateFromTokens(tokens, index);
                index += headerDirective.TokenCount;
                tokenCount += headerDirective.TokenCount;
            }

            LabelNode? label = null;
            if (tokens.Count > index && LabelNode.IsValidLabelAtIndex(tokens, index))
            {
                label = LabelNode.CreateFromTokens(tokens, index);
                index += LabelNode.TokenCount;
                tokenCount += LabelNode.TokenCount;
            }

            var hasPostDirective = tokens.Count > index && DirectiveNode.IsValidDirectiveAtIndex(tokens, index);
            var hasInstruction = tokens.Count > index && InstructionNode.IsValidInstructionAtIndex(tokens, index);
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

            if (tokens.Count <= index || tokens[index].Type != TokenType.EndOfLine)
            {
                var lastToken = tokens[Math.Min(index, tokens.Count - 1)];
                throw new ParserException("Expected end of statement (newline).", lastToken.Line, lastToken.Column);
            }

            var statementSpan = new NodeSpan(startColumn, tokens[index].Column, tokens[index].Line);
            tokenCount += 1; // for the EndOfLine token
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
