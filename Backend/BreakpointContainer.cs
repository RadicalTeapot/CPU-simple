namespace Backend
{
    internal readonly struct Breakpoint(int address)
    {
        public int Address { get; } = address;
    }

    internal class BreakpointContainer
    {
        public bool Contains(int address) => _breakpoints.ContainsKey(address);
        public void Add(int address) => _breakpoints.Add(address, new Breakpoint(address));
        public void Remove(int address) => _breakpoints.Remove(address);
        public void Clear() => _breakpoints.Clear();
        public Breakpoint[] GetAll() => [.._breakpoints.Values];

        private readonly Dictionary<int, Breakpoint> _breakpoints = [];
    }
}
