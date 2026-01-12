using Assembler.AST;

namespace Assembler.Analysis
{
    internal static class OperandValueProcessor
    {
        /// <summary>
        /// Processes string literals.
        /// </summary>
        /// <param name="input">The raw string content (without surrounding quotes)</param>
        /// <returns>The processed string with surrounding quotes removed and escape sequences replaced</returns>
        /// <remarks>Supports \\ (backslash) and \" (double quote) escape sequences.</remarks>
        public static string ProcessString(string input)
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
        public static int ParseHexNumberString(string hexString)
        {
            return Convert.ToInt32(hexString[2..], 16);
        }

        /// <summary>
        /// Parses a hex number string to a byte that has a 0x prefix.
        /// </summary>
        /// <param name="hexString">The hex string</param>
        /// <returns>The parsed byte value</returns>
        public static byte ParseHexByteString(string hexString)
        {
            return Convert.ToByte(hexString[2..], 16);
        }

        /// <summary>
        /// Parses a hex number string to a ushort that has a 0x prefix.
        /// </summary>
        /// <param name="hexString">The hex string</param>
        /// <returns>The parsed ushort value</returns>
        public static ushort ParseHexUShortString(string hexString)
        {
            return Convert.ToUInt16(hexString[2..], 16);
        }
    }

    internal class MemoryAddressValueProcessor(int memorySize)
    {
#if x16
        public ushortParseAddressValueString(HexNumberNode hexNumber)
        {
            var address = OperandValueProcessor.ParseHexNumberString(hexNumber.Value);
            if (address < 0 || address >= memorySize)
            {
                throw new AnalyserException("Address value out of range for 16-bit architecture", hexNumber.Span.Line, hexNumber.Span.StartColumn);
            }
            return (ushort)address;
        }
#else
        public byte ParseAddressValueString(ImmediateValueNode immediateOperand)
        {
            var address = OperandValueProcessor.ParseHexNumberString(immediateOperand.Value);
            if (address < 0 || address >= memorySize)
            {
                throw new AnalyserException("Address value out of range for 8-bit architecture", immediateOperand.Span.Line, immediateOperand.Span.StartColumn);
            }
            return (byte)address;
        }
#endif

        public byte[] ParseAddressValueStringAsByteArray(ImmediateValueNode immediateOperand)
        {
            var addressValue = ParseAddressValueString(immediateOperand);
#if x16
            return BitConverter.GetBytes(addressValue);
#else
            return [addressValue];
#endif
        }
    }
}
