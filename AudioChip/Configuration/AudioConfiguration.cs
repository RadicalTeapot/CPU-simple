namespace AudioChip.Configuration
{
    public record AudioConfiguration(
        int SampleRate,
        int BufferSize,
        int CpuClockRate)
    {
    }
}
