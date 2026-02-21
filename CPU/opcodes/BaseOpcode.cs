using CPU.microcode;

namespace CPU.opcodes
{
    internal abstract class BaseOpcode : IOpcode
    {
        /// <summary>
        /// A single phase of microcode execution. Each phase corresponds to a specific step in the instruction execution process, 
        /// such as fetching operands, performing ALU operations, or writing results back to registers or memory.
        /// </summary>
        /// <returns>The type of next microphase to execute.</returns>
        public delegate MicroPhase MicroPhaseDelegate();

        public BaseOpcode()
        {
            _phases = [Done];
        }

        public MicroPhase GetStartPhaseType() => _startPhaseType;

        public MicroPhase Tick(int phaseCount)
        {
            return _phases[phaseCount].Invoke();
        }

        protected void SetPhases(MicroPhase startPhaseType, params MicroPhaseDelegate[] phases)
        {
            _startPhaseType = startPhaseType;
            _phases = [.. phases, Done];
        }

        private MicroPhase Done() => MicroPhase.Done;

        private MicroPhase _startPhaseType = MicroPhase.Done;
        private MicroPhaseDelegate[] _phases;
    }
}
