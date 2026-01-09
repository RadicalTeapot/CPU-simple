using Assembler.Lexeme;

namespace Assembler.AST
{
    public enum AddressType
    {
        Label,
        Immediate,
        PositiveOffset,
        NegativeOffset
    }

    public class MemoryAddressNode(int tokenCount, NodeSpan span)
        : BaseNode(span)
    {
        public int TokenCount { get; } = tokenCount;

        public AddressType AddressOffsetType { get; private set; } = AddressType.Label;

        public void GetAddress(out HexNumberNode hexNumberNode)
        {
            if (AddressOffsetType != AddressType.Immediate || _immediateAddress == null) 
            {
                throw new ParserException("Memory address has no immediate value", Span.Line, Span.StartColumn);
            }
            hexNumberNode = _immediateAddress;
        }

        public void GetAddress(out LabelReferenceNode labelRefNode)
        {
            if (AddressOffsetType != AddressType.Label || _labelAddress == null)
            {
                throw new ParserException("Memory address has no label reference", Span.Line, Span.StartColumn);
            }
            labelRefNode = _labelAddress;
        }

        public void GetAddress(out LabelReferenceNode labelRefNode, out HexNumberNode offsetNode)
        {
            if ((AddressOffsetType != AddressType.PositiveOffset || AddressOffsetType != AddressType.NegativeOffset) || _labelAddress == null || _offset == null)
            {
                throw new ParserException("Memory address has no label reference with offset", Span.Line, Span.StartColumn);
            }
            labelRefNode = _labelAddress;
            offsetNode = _offset;
        }

        public static bool IsValidMemoryAddressAtIndex(IList<Lexeme.Token> tokens, int index)
        {
            if (tokens[index].Type != TokenType.LeftSquareBracket)
            {
                return false;
            }

            var tokensRemaining = tokens.Count - index;
            if (tokensRemaining < minTokenCount) // Not enough tokens
            {
                throw new ParserException("Invalid memory address syntax.", tokens[index].Line, tokens[index].Column);
            }

            // Further validation will be done in CreateFromTokens
            return true;
        }

        public static MemoryAddressNode CreateFromTokens(IList<Token> tokens, int index)
        {
            if (!IsValidMemoryAddressAtIndex(tokens, index))
            {
                throw new ParserException("Invalid memory address syntax.", tokens[index].Line, tokens[index].Column);
            }

            var startColumn = tokens[index].Column;
            index++; // Skip '['

            var tokenCount = 2; // For the square brackets
            HexNumberNode? immediateAddress = null;
            LabelReferenceNode? labelAddress = null;
            HexNumberNode? offset = null;
            AddressType addressType;

            if (HexNumberNode.IsValidHexNodeAtIndex(tokens, index))
            {
                addressType = AddressType.Immediate;
                immediateAddress = HexNumberNode.CreateFromTokens(tokens, index);
                tokenCount += HexNumberNode.TokenCount;
                index += HexNumberNode.TokenCount;
            }
            else if (LabelReferenceNode.IsValidLabelReferenceAtIndex(tokens, index))
            {
                addressType = AddressType.Label;
                labelAddress = LabelReferenceNode.CreateFromTokens(tokens, index);
                index += LabelReferenceNode.TokenCount;

                var offsetToken = tokens[index];
                if (offsetToken.Type == TokenType.Plus || offsetToken.Type == TokenType.Minus)
                {
                    addressType = offsetToken.Type == TokenType.Plus
                        ? AddressType.PositiveOffset
                        : AddressType.NegativeOffset;
                    index++; // Skip the offset sign

                    if (HexNumberNode.IsValidHexNodeAtIndex(tokens, index))
                    {
                        offset = HexNumberNode.CreateFromTokens(tokens, index);
                        tokenCount += HexNumberNode.TokenCount;
                        index += HexNumberNode.TokenCount;
                    }
                    else
                    {
                        throw new ParserException($"Unexpected token {tokens[index].ToString()} for offset value", tokens[index].Line, tokens[index].Column);
                    }
                }
            }
            else
            {
                throw new ParserException($"Unexpected token {tokens[index]} for memory address", tokens[index].Line, tokens[index].Column);
            }

            if (tokens[index].Type != TokenType.RightSquareBracket)
            {
                throw new ParserException("Expected closing square bracket for memory address.", tokens[index].Line, tokens[index].Column);
            }

            var nodeSpan = new NodeSpan(startColumn, tokens[index].Column + 1, tokens[index].Line);
            return new MemoryAddressNode(tokenCount, nodeSpan)
            {
                _immediateAddress = immediateAddress,
                _labelAddress = labelAddress,
                _offset = offset,
                AddressOffsetType = addressType,
            };
        }

        private HexNumberNode? _immediateAddress;
        private LabelReferenceNode? _labelAddress;
        private HexNumberNode? _offset;

        private const int minTokenCount = 3; // [ label ]
    }
}
