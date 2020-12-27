using System;
using ZXing;

namespace Vltk.Interpreter.Pipe
{
    /// <summary>
    /// Sample starts with 1 byte per pixel of luminance data.
    /// This is followed by some other data that we just ignore.
    /// </summary>
    public sealed class Nv12LuminanceSource : LuminanceSource
    {
        public override byte[] Matrix => _sample;

        public override byte[] getRow(int y, byte[] row)
        {
            if (row.Length < Width)
                row = new byte[Width];

            Buffer.BlockCopy(_sample, y * Width, row, 0, Width);
            return row;
        }

        private readonly byte[] _sample;

        public Nv12LuminanceSource(byte[] sample, int width, int height)
            : base(width, height)
        {
            _sample = sample;
        }
    }
}
