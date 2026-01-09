using Assembler.Lexeme;
using System.Diagnostics;

namespace Assembler.AST
{
    public abstract record MemoryAddress
    {
        public record Immediate(HexNumberNode Address) : MemoryAddress;
        public record Label(LabelReferenceNode LabelRef) : MemoryAddress;
        public record LabelWithPositiveOffset(LabelReferenceNode LabelRef, HexNumberNode Offset) : MemoryAddress;
        public record LabelWithNegativeOffset(LabelReferenceNode LabelRef, HexNumberNode Offset) : MemoryAddress;
    }

    public class MemoryAddressNode(int tokenCount, NodeSpan span)
        : BaseNode(span)
    {
        private enum AddressType
        {
            Label,
            Immediate,
            PositiveOffset,
            NegativeOffset
        }

        public int TokenCount { get; } = tokenCount;

        public MemoryAddress GetAddress()
        {
            switch (_addressOffsetType)
            {
                case AddressType.Immediate:
                    Debug.Assert(_immediateAddress != null, "Memory address has no immediate value");
                    return new MemoryAddress.Immediate(_immediateAddress!);
                case AddressType.Label:
                    Debug.Assert(_labelAddress != null, "Memory address has no label reference");
                    return new MemoryAddress.Label(_labelAddress!);
                case AddressType.PositiveOffset:
                    Debug.Assert(_labelAddress != null && _offset != null && _addressOffsetType == AddressType.PositiveOffset, "Memory address has no label reference with positive offset");
                    return new MemoryAddress.LabelWithPositiveOffset(_labelAddress!, _offset!);
                case AddressType.NegativeOffset:
                    Debug.Assert(_labelAddress != null && _offset != null && _addressOffsetType == AddressType.NegativeOffset, "Memory address has no label reference with negative offset");
                    return new MemoryAddress.LabelWithNegativeOffset(_labelAddress!, _offset!);
                default:
                    throw new ParserException("Unknown memory address type", Span.Line, Span.StartColumn);
            };
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
                _addressOffsetType = addressType,
            };
        }

        private HexNumberNode? _immediateAddress;
        private LabelReferenceNode? _labelAddress;
        private HexNumberNode? _offset;
        private AddressType _addressOffsetType;

        private const int minTokenCount = 3; // [ label ]
    }
}
