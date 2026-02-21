namespace CPU.microcode
{
    internal enum MicroPhase
    {
        // Bus transactions
        FetchOp,
        MemoryRead,
        MemoryWrite,
        JumpToInterrupt,

        // Internal operations
        AluOp, 

        Done
    }
}
