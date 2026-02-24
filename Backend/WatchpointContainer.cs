using CPU.microcode;

namespace Backend
{
    public interface IWatchpoint
    {
        int Id { get; }
        bool Matches(TickTrace trace);
        string Description { get; }
    }

    internal class AddressWatchpoint(int id, BusDirection direction, int address) : IWatchpoint
    {
        public int Id { get; } = id;
        public string Description { get; } = $"on-{direction.ToString().ToLower()} 0x{address:X4}";

        public bool Matches(TickTrace trace)
        {
            return trace.Bus != null
                && trace.Bus.Direction == direction
                && trace.Bus.Type == BusType.Memory
                && trace.Bus.Address == address;
        }
    }

    internal class PhaseWatchpoint(int id, MicroPhase phase) : IWatchpoint
    {
        public int Id { get; } = id;
        public string Description { get; } = $"on-phase {phase}";

        public bool Matches(TickTrace trace)
        {
            return trace.NextPhase == phase;
        }
    }

    internal class WatchpointContainer
    {
        public int Add(IWatchpoint watchpoint)
        {
            _watchpoints[watchpoint.Id] = watchpoint;
            return watchpoint.Id;
        }

        public void Remove(int id) => _watchpoints.Remove(id);

        public void Clear() => _watchpoints.Clear();

        public IWatchpoint[] GetAll() => [.. _watchpoints.Values];

        public int Count => _watchpoints.Count;

        public int NextId() => _nextId++;

        public IWatchpoint? Check(TickTrace[] traces)
        {
            foreach (var trace in traces)
            {
                foreach (var watchpoint in _watchpoints.Values)
                {
                    if (watchpoint.Matches(trace))
                        return watchpoint;
                }
            }
            return null;
        }

        private int _nextId = 1;
        private readonly Dictionary<int, IWatchpoint> _watchpoints = [];
    }
}
