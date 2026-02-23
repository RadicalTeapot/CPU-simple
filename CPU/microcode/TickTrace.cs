namespace CPU.microcode
{
    public enum TickType { BusRead, BusWrite, Internal }
    public enum BusDirection { Read, Write }
    public record BusAccess(int Address, byte Data, BusDirection Direction);
    public record RegisterChange(int Index, byte OldValue, byte NewValue);
    public record TickTrace(
        ulong TickNumber,
        TickType Type,
        MicroPhase Phase,
        int PcBefore, int PcAfter,
        int SpBefore, int SpAfter,
        string Instruction,
        RegisterChange[] RegisterChanges,
        bool ZeroFlagBefore, bool ZeroFlagAfter,
        bool CarryFlagBefore, bool CarryFlagAfter,
        BusAccess? Bus
    );
}
