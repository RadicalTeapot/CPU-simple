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
            Debug.Assert(data.Count == config.BytesPerTile * config.ChrCount, "CHR ROM data length does not match expected size based on PPU configuration.");
            _bytesPerTile = config.BytesPerTile;
            _bytesPerRow = _bytesPerTile / config.TileSize;
            _data = data;
        }

        public byte ReadRow(int tileIndex, int row)
        {
            int offset = tileIndex * _bytesPerTile + row * _bytesPerRow;
            return _data[offset];
        }

        private readonly IReadOnlyList<byte> _data;
        private readonly int _bytesPerTile;
        private readonly int _bytesPerRow;
    }
}
