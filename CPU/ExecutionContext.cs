namespace CPU
{
    public class ExecutionContext
    {
        public List<KeyValuePair<int, byte>> MemoryChanges { get; } = [];
        public List<KeyValuePair<int, byte>> StackChanges { get; } = [];
        public string[] LastInstruction { get; private set; } = [];

        public void RecordMemoryChange(int address, byte value)
            => MemoryChanges.Add(new KeyValuePair<int, byte>(address, value));

        public void RecordStackChange(int address, byte value)
            => StackChanges.Add(new KeyValuePair<int, byte>(address, value));

        public void SetLastInstruction(string[] instruction)
            => LastInstruction = instruction;
    }
}
