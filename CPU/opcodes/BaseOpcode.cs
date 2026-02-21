using CPU.microcode;

namespace CPU.opcodes
{
    internal abstract class BaseOpcode : IOpcode
    {
        public BaseOpcode()
        {
            _phases = [Done];
        }

        public MicroPhase Tick(int phaseCount)
        {
            return _phases[phaseCount].Invoke();
        }

        protected void SetPhases(params Func<MicroPhase>[] phases)
        {
            _phases = [.. phases, Done];
        }

        private MicroPhase Done() => MicroPhase.Done;

        private Func<MicroPhase>[] _phases;
    }
}
