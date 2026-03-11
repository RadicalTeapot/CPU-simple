namespace CPU
{
    public readonly struct Config(int memorySize, int stackSize, int registerCount, int vramSize = 0)
    {
        public const int IrqSectionSize = 16;
        public const int MmioRegionSize = components.BusDecoder.MmioRegionSize;

        public readonly int MemorySize { get; } = memorySize;
        public readonly int StackSize { get; } = stackSize;
        public readonly int RegisterCount { get; } = registerCount;
        public readonly int VramSize { get; } = vramSize;
        public readonly int IrqVectorAddress { get; } = memorySize - stackSize - components.BusDecoder.MmioRegionSize - IrqSectionSize;

#if x16
        public Config(): this(65536, 16, 4) { }
#else
        public Config(): this(256, 16, 4) { }
#endif
    }
}