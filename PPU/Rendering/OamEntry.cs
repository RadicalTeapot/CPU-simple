using System;
using System.Collections.Generic;
using System.Text;

namespace PPU.Rendering
{
    internal readonly struct OamEntry
    {
        public readonly int X;
        public readonly int Y;
        public readonly byte TileIndex;
        public readonly bool Prioritize;
        public readonly bool FlipHorizontal;
        public readonly bool FlipVertical;

        public static OamEntry Decode(byte position, byte tileIndex, byte attributes)
        {
            return new OamEntry(
                x: position & 0x0F,
                y: (position >> 4) & 0x0F,
                tileIndex: tileIndex,
                prioritize: (attributes & 0b100) != 0,
                flipHorizontal: (attributes & 0b010) != 0,
                flipVertical: (attributes & 0b001) != 0
            );
        }

        private OamEntry(int x, int y, byte tileIndex, bool prioritize, bool flipHorizontal, bool flipVertical)
        {
            X = x;
            Y = y;
            TileIndex = tileIndex;
            Prioritize = prioritize;
            FlipHorizontal = flipHorizontal;
            FlipVertical = flipVertical;
        }
    }
}
