namespace CPU.microcode
{
    public enum TickType { Bus, Internal }
    public enum BusDirection { Read, Write }
    public enum BusType { Memory, Stack }
    public record BusAccess(int Address, byte Data, BusDirection Direction, BusType Type);
    public record RegisterChange(int Index, byte OldValue, byte NewValue);
    public record TickTrace(
        ulong TickNumber,
        TickType Type,
        MicroPhase NextPhase,
        int PcBefore, int PcAfter,
        int SpBefore, int SpAfter,
        string Instruction,
        RegisterChange[] RegisterChanges,
        bool ZeroFlagBefore, bool ZeroFlagAfter,
        bool CarryFlagBefore, bool CarryFlagAfter,
        BusAccess? Bus
    );
}
