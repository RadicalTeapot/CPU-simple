namespace CPU
{
    public readonly struct Config(int memorySize, int stackSize, int registerCount, int vramSize = 0)
    {
        public const int IrqSectionSize = 16;

        public readonly int MemorySize { get; } = memorySize;
        public readonly int StackSize { get; } = stackSize;
        public readonly int RegisterCount { get; } = registerCount;
        public readonly int VramSize { get; } = vramSize;
        public readonly int IrqVectorAddress { get; } = memorySize - stackSize - IrqSectionSize;

        public Config(): this(256, 16, 4) { }
    }
}