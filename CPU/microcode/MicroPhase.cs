namespace CPU.microcode
{
    public enum MicroPhase
    {
        // Bus transactions
        FetchOpcode,          // fetch the instruction opcode byte (handled by TickHandler, PC++)
        FetchOperand,         // fetch a single-byte instruction operand (immediate, 8-bit address, reg+offset encoding), PC++
        FetchOperand16Low,    // fetch low byte of a 16-bit address operand, PC++
        FetchOperand16High,   // fetch high byte of a 16-bit address operand, PC++
        MemoryRead,           // read data from memory or stack (not an instruction byte)
        MemoryWrite,          // write data to memory or stack
        JumpToInterrupt,

        // Internal operations
        AluOp,
        EffectiveAddrComputation, // compute EA = base + offset (indexed addressing)
        ValueComposition,         // compose a multi-byte value: (high << 8) | low

        Done
    }
}
