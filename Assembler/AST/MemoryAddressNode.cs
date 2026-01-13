using Assembler.Lexeme;
using System.Diagnostics;

namespace Assembler.AST
{
    public abstract record MemoryAddress
    {
        public record Immediate(ImmediateValueNode Address) : MemoryAddress;
        public record Label(LabelReferenceNode LabelRef) : MemoryAddress;
        public record LabelWithPositiveOffset(LabelReferenceNode LabelRef, ImmediateValueNode Offset) : MemoryAddress;
        public record LabelWithNegativeOffset(LabelReferenceNode LabelRef, ImmediateValueNode Offset) : MemoryAddress;
        public record Register(RegisterNode RegisterNode) : MemoryAddress;
        public record RegisterWithPositiveOffset(RegisterNode RegisterNode, ImmediateValueNode Offset) : MemoryAddress;
    }

    public class MemoryAddressNode(int tokenCount, NodeSpan span)
        : BaseNode(span)
    {
        private enum AddressType
        {
            Label,
            Immediate,
            PositiveOffset,
            NegativeOffset,
            Register,
            RegisterPositiveOffset
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
                    return new MemoryAddress.LabelWithPositiveOffset(_labelAddress, _offset);
                case AddressType.NegativeOffset:
                    Debug.Assert(_labelAddress != null && _offset != null && _addressOffsetType == AddressType.NegativeOffset, "Memory address has no label reference with negative offset");
                    return new MemoryAddress.LabelWithNegativeOffset(_labelAddress, _offset);
                case AddressType.Register:
                    Debug.Assert(_registerAddress != null, "Memory address has no register reference");
                    return new MemoryAddress.Register(_registerAddress);
                case AddressType.RegisterPositiveOffset:
                    Debug.Assert(_registerAddress != null && _offset != null && _addressOffsetType == AddressType.RegisterPositiveOffset, "Memory address has no register reference with positive offset");
                    return new MemoryAddress.RegisterWithPositiveOffset(_registerAddress, _offset);
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
            ImmediateValueNode? immediateAddress = null;
            LabelReferenceNode? labelAddress = null;
            RegisterNode? registerAddress = null;
            ImmediateValueNode? offset = null;
            AddressType addressType;

            if (tokens.Count > index && ImmediateValueNode.IsValidImmediateValueNodeAtIndex(tokens, index))
            {
                addressType = AddressType.Immediate;
                immediateAddress = ImmediateValueNode.CreateFromTokens(tokens, index);
                tokenCount += ImmediateValueNode.TokenCount;
                index += ImmediateValueNode.TokenCount;
            }
            else if (tokens.Count > index && LabelReferenceNode.IsValidLabelReferenceAtIndex(tokens, index))
            {
                addressType = AddressType.Label;
                labelAddress = LabelReferenceNode.CreateFromTokens(tokens, index);
                tokenCount += LabelReferenceNode.TokenCount;
                index += LabelReferenceNode.TokenCount;

                
                if (tokens.Count > index && (tokens[index].Type == TokenType.Plus || tokens[index].Type == TokenType.Minus))
                {
                    var offsetToken = tokens[index];
                    addressType = offsetToken.Type == TokenType.Plus
                        ? AddressType.PositiveOffset
                        : AddressType.NegativeOffset;
                    tokenCount++; // For the offset sign
                    index++; // Skip the offset sign

                    if (tokens.Count > index && ImmediateValueNode.IsValidImmediateValueNodeAtIndex(tokens, index))
                    {
                        offset = ImmediateValueNode.CreateFromTokens(tokens, index);
                        tokenCount += ImmediateValueNode.TokenCount;
                        index += ImmediateValueNode.TokenCount;
                    }
                    else
                    {
                        throw new ParserException($"Unexpected token {tokens[index].ToString()} for offset value", tokens[index].Line, tokens[index].Column);
                    }
                }
            }
            else if (tokens.Count > index && RegisterNode.IsValidRegisterNodeAtIndex(tokens, index))
            {
                addressType = AddressType.Register;
                registerAddress = RegisterNode.CreateFromTokens(tokens, index);
                tokenCount += RegisterNode.TokenCount;
                index += RegisterNode.TokenCount;
                if (tokens.Count > index && (tokens[index].Type == TokenType.Plus))
                {
                    addressType = AddressType.RegisterPositiveOffset;
                    tokenCount++; // For the offset sign
                    index++; // Skip the offset sign
                    if (tokens.Count > index && ImmediateValueNode.IsValidImmediateValueNodeAtIndex(tokens, index))
                    {
                        offset = ImmediateValueNode.CreateFromTokens(tokens, index);
                        tokenCount += ImmediateValueNode.TokenCount;
                        index += ImmediateValueNode.TokenCount;
                    }
                    else
                    {
                        throw new ParserException($"Unexpected token {tokens[index]} for offset value", tokens[index].Line, tokens[index].Column);
                    }
                }
            }
            else
            {
                throw new ParserException($"Unexpected token {tokens[index]} for memory address", tokens[index].Line, tokens[index].Column);
            }

            if (tokens.Count <= index || tokens[index].Type != TokenType.RightSquareBracket)
            {
                var lastToken = tokens[Math.Min(index, tokens.Count - 1)];
                throw new ParserException("Expected closing square bracket for memory address.", lastToken.Line, lastToken.Column);
            }

            var nodeSpan = new NodeSpan(startColumn, tokens[index].Column + 1, tokens[index].Line);
            return new MemoryAddressNode(tokenCount, nodeSpan)
            {
                _immediateAddress = immediateAddress,
                _labelAddress = labelAddress,
                _registerAddress = registerAddress,
                _offset = offset,
                _addressOffsetType = addressType,
            };
        }

        private ImmediateValueNode? _immediateAddress;
        private LabelReferenceNode? _labelAddress;
        private RegisterNode? _registerAddress;
        private ImmediateValueNode? _offset;
        private AddressType _addressOffsetType;

        private const int minTokenCount = 3; // [ label ]
    }
}
