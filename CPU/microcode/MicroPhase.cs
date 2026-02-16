namespace CPU.microcode
{
    internal enum MicroPhase
    {
        // Bus transactions

        /// <summary>
        /// Fetch operand byte
        /// </summary>
        FetchOp8,
        /// <summary>
        /// Jump to interrupt handler
        /// </summary>
        JumpToInterrupt,

        // Internal operations

        /// <summary>
        /// Perform ALU operation
        /// </summary>
        AluOp, 

        Done
    }
}
