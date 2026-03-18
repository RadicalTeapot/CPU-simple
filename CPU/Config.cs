namespace CPU
{
    public readonly struct Config(int memorySize, int stackSize, int registerCount, int vramSize = 0)
    {
        public const int MmioRegionSize = components.BusDecoder.MmioRegionSize;
#if x16
        public const int VectorTableSize = 2;
#else
        public const int VectorTableSize = 1;
#endif

        public int MemorySize { get; } = memorySize;
        public int StackSize { get; } = stackSize;
        public int RegisterCount { get; } = registerCount;
        public int VramSize { get; } = vramSize;
        public int IrqVectorTableAddress { get; } = memorySize - stackSize - components.BusDecoder.MmioRegionSize - VectorTableSize;

#if x16
        public Config(): this(65536, 256, 4) { }
#else
        public Config(): this(256, 16, 4) { }
#endif
    }
}