using PPU.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace PPU.Rendering
{
    internal class Framebuffer(PpuConfig config)
    {
        /// <summary>
        /// Writes a pixel value to the framebuffer at the specified row and column.
        /// </summary>
        /// <param name="row">The row index of the pixel to write.</param>
        /// <param name="col">The column index of the pixel to write.</param>
        /// <param name="value">The value of the pixel to write. Typically represents color information.</param>
        public void Write(int row, int col, byte value)
        {
            var index = row * config.ScreenWidth + col;
            _pixels[index] = value;
        }

        public void Clear()
        {
            Array.Clear(_pixels, 0, _pixels.Length);
        }

        public IReadOnlyList<byte> AsReadOnly()
        {
            return _pixels;
        }

        private byte[] _pixels = new byte[config.ScreenHeight * config.ScreenWidth];
    }
}
