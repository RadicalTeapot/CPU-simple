using PPU.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PPU.Storage
{
    internal class ChrRom
    {
        public ChrRom(PpuConfig config, IReadOnlyList<byte> data) 
        {
            if (data.Count != config.BytesPerTile * config.ChrCount)
                throw new ArgumentException("CHR ROM data length does not match expected size based on PPU configuration.");

            _bytesPerTile = config.BytesPerTile;
            _bytesPerRow = _bytesPerTile / config.TileSize;
            _data = data;
        }

        public byte ReadTileRow(int tileIndex, int tileRow, bool vFlip)
        {
            var rowOffset = vFlip ? (7 - tileRow) : tileRow;
            var offset = tileIndex * _bytesPerTile + rowOffset * _bytesPerRow;
            return _data[offset];
        }

        private readonly IReadOnlyList<byte> _data;
        private readonly int _bytesPerTile;
        private readonly int _bytesPerRow;
    }
}
