using NUnit.Framework;
using CPU.opcodes;
using CPU.microcode;

namespace CPU.Tests
{
    [TestFixture]
    public class Opcodes_tick_tests
    {
        [TestCase((byte)OpcodeBaseCode.NOP)]
        [TestCase((byte)OpcodeBaseCode.MOV)]
        [TestCase((byte)OpcodeBaseCode.CLC)]
        [TestCase((byte)OpcodeBaseCode.SEC)]
        [TestCase((byte)OpcodeBaseCode.CLZ)]
        [TestCase((byte)OpcodeBaseCode.SEZ)]
        public void ZeroExecute(byte opcodeByte)
        {
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [opcodeByte],
                out _,
                out _,
                out _);

            MicroPhase[] expected = [MicroPhase.FetchOpcode];
            Assert.That(TickSequence(cpu), Is.EqualTo(expected));
        }

        [Test]
        public void Hlt_TickThrows()
        {
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.HLT],
                out _,
                out _,
                out _);

            Assert.Throws<OpcodeException.HaltException>(() => cpu.Tick());
        }

        [TestCase((byte)OpcodeBaseCode.ADD)]
        [TestCase((byte)OpcodeBaseCode.SUB)]
        [TestCase((byte)OpcodeBaseCode.AND)]
        [TestCase((byte)OpcodeBaseCode.OR)]
        [TestCase((byte)OpcodeBaseCode.XOR)]
        [TestCase((byte)OpcodeBaseCode.CMP)]
        [TestCase((byte)OpcodeBaseCode.LSH)]
        [TestCase((byte)OpcodeBaseCode.RSH)]
        [TestCase((byte)OpcodeBaseCode.LRT)]
        [TestCase((byte)OpcodeBaseCode.RRT)]
        [TestCase((byte)OpcodeBaseCode.INC)]
        [TestCase((byte)OpcodeBaseCode.DEC)]
        public void RegisterAlu(byte opcodeByte)
        {
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [opcodeByte],
                out _,
                out _,
                out _);

            MicroPhase[] expected = [MicroPhase.AluOp, MicroPhase.FetchOpcode];
            Assert.That(TickSequence(cpu), Is.EqualTo(expected));
        }

        [Test]
        public void Ldi()
        {
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.LDI, 0x00],
                out _,
                out _,
                out _);

            MicroPhase[] expected = [MicroPhase.FetchOperand, MicroPhase.FetchOpcode];
            Assert.That(TickSequence(cpu), Is.EqualTo(expected));
        }

        [TestCase((byte)OpcodeBaseCode.ADI)]
        [TestCase((byte)OpcodeBaseCode.SBI)]
        [TestCase((byte)OpcodeBaseCode.CPI)]
        [TestCase((byte)OpcodeBaseCode.ANI)]
        [TestCase((byte)OpcodeBaseCode.ORI)]
        [TestCase((byte)OpcodeBaseCode.XRI)]
        [TestCase((byte)OpcodeBaseCode.BTI)]
        public void ImmediateAlu(byte opcodeByte)
        {
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [opcodeByte, 0x00],
                out _,
                out _,
                out _);

            MicroPhase[] expected = [MicroPhase.FetchOperand, MicroPhase.AluOp, MicroPhase.FetchOpcode];
            Assert.That(TickSequence(cpu), Is.EqualTo(expected));
        }

        [Test]
        public void Psh()
        {
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.PSH],
                out _,
                out _,
                out _);

            MicroPhase[] expected = [MicroPhase.MemoryWrite, MicroPhase.FetchOpcode];
            Assert.That(TickSequence(cpu), Is.EqualTo(expected));
        }

        [TestCase((byte)OpcodeBaseCode.POP)]
        [TestCase((byte)OpcodeBaseCode.PEK)]
        public void PopAndPeek(byte opcodeByte)
        {
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [opcodeByte],
                out _,
                out var stack,
                out _);
            stack.PushByte(0x00);

            MicroPhase[] expected = [MicroPhase.MemoryRead, MicroPhase.FetchOpcode];
            Assert.That(TickSequence(cpu), Is.EqualTo(expected));
        }

        [TestCase((byte)OpcodeBaseCode.JMP)]
        [TestCase((byte)OpcodeBaseCode.JCC)]
        [TestCase((byte)OpcodeBaseCode.JCS)]
        [TestCase((byte)OpcodeBaseCode.JZC)]
        [TestCase((byte)OpcodeBaseCode.JZS)]
        public void Jumps(byte opcodeByte)
        {
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [opcodeByte, .. OpcodeTestHelpers.GetAddress(0x00)],
                out _,
                out _,
                out _);

#if x16
            MicroPhase[] expected = [MicroPhase.FetchOperand16Low, MicroPhase.FetchOperand16High, MicroPhase.FetchOpcode];
#else
            MicroPhase[] expected = [MicroPhase.FetchOperand, MicroPhase.FetchOpcode];
#endif
            Assert.That(TickSequence(cpu), Is.EqualTo(expected));
        }

        [Test]
        public void Lda()
        {
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.LDA, .. OpcodeTestHelpers.GetAddress(0x00)],
                out _,
                out _,
                out _);

#if x16
            MicroPhase[] expected = [MicroPhase.FetchOperand16Low, MicroPhase.FetchOperand16High, MicroPhase.MemoryRead, MicroPhase.FetchOpcode];
#else
            MicroPhase[] expected = [MicroPhase.FetchOperand, MicroPhase.MemoryRead, MicroPhase.FetchOpcode];
#endif
            Assert.That(TickSequence(cpu), Is.EqualTo(expected));
        }

        [Test]
        public void Sta()
        {
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.STA, .. OpcodeTestHelpers.GetAddress(0x00)],
                out _,
                out _,
                out _);

#if x16
            MicroPhase[] expected = [MicroPhase.FetchOperand16Low, MicroPhase.FetchOperand16High, MicroPhase.MemoryWrite, MicroPhase.FetchOpcode];
#else
            MicroPhase[] expected = [MicroPhase.FetchOperand, MicroPhase.MemoryWrite, MicroPhase.FetchOpcode];
#endif
            Assert.That(TickSequence(cpu), Is.EqualTo(expected));
        }

        [Test]
        public void Ldx()
        {
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.LDX, 0x00],
                out _,
                out _,
                out _);

            MicroPhase[] expected = [MicroPhase.FetchOperand, MicroPhase.AluOp, MicroPhase.MemoryRead, MicroPhase.FetchOpcode];
            Assert.That(TickSequence(cpu), Is.EqualTo(expected));
        }

        [Test]
        public void Stx()
        {
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.STX, 0x00],
                out _,
                out _,
                out _);

            MicroPhase[] expected = [MicroPhase.FetchOperand, MicroPhase.AluOp, MicroPhase.MemoryWrite, MicroPhase.FetchOpcode];
            Assert.That(TickSequence(cpu), Is.EqualTo(expected));
        }

        [TestCase((byte)OpcodeBaseCode.ADA)]
        [TestCase((byte)OpcodeBaseCode.SBA)]
        [TestCase((byte)OpcodeBaseCode.CPA)]
        [TestCase((byte)OpcodeBaseCode.ANA)]
        [TestCase((byte)OpcodeBaseCode.ORA)]
        [TestCase((byte)OpcodeBaseCode.XRA)]
        [TestCase((byte)OpcodeBaseCode.BTA)]
        public void MemoryAlu(byte opcodeByte)
        {
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [opcodeByte, .. OpcodeTestHelpers.GetAddress(0x00)],
                out _,
                out _,
                out _);

#if x16
            MicroPhase[] expected = [MicroPhase.FetchOperand16Low, MicroPhase.FetchOperand16High, MicroPhase.MemoryRead, MicroPhase.AluOp, MicroPhase.FetchOpcode];
#else
            MicroPhase[] expected = [MicroPhase.FetchOperand, MicroPhase.MemoryRead, MicroPhase.AluOp, MicroPhase.FetchOpcode];
#endif
            Assert.That(TickSequence(cpu), Is.EqualTo(expected));
        }

        [Test]
        public void Cal()
        {
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.CAL, .. OpcodeTestHelpers.GetAddress(0x00)],
                out _,
                out _,
                out _);

#if x16
            MicroPhase[] expected = [MicroPhase.FetchOperand16Low, MicroPhase.FetchOperand16High, MicroPhase.MemoryWrite, MicroPhase.MemoryWrite, MicroPhase.FetchOpcode];
#else
            MicroPhase[] expected = [MicroPhase.FetchOperand, MicroPhase.MemoryWrite, MicroPhase.FetchOpcode];
#endif
            Assert.That(TickSequence(cpu), Is.EqualTo(expected));
        }

        [Test]
        public void Ret()
        {
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.RET],
                out _,
                out var stack,
                out _);
            stack.PushAddress(0x00);

#if x16
            MicroPhase[] expected = [MicroPhase.MemoryRead, MicroPhase.MemoryRead, MicroPhase.FetchOpcode];
#else
            MicroPhase[] expected = [MicroPhase.MemoryRead, MicroPhase.FetchOpcode];
#endif
            Assert.That(TickSequence(cpu), Is.EqualTo(expected));
        }

        [Test]
        public void ZeroExecute_TraceHasBusRead()
        {
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.NOP],
                out _,
                out _,
                out _);

            var result = cpu.Tick();

            Assert.That(result.Trace, Is.Not.Null);
            Assert.That(result.Trace!.Phase, Is.EqualTo(MicroPhase.FetchOpcode));
            Assert.That(result.Trace.Type, Is.EqualTo(TickType.Bus));
            Assert.That(result.Trace.Bus, Is.Not.Null);
            Assert.That(result.Trace.Bus!.Address, Is.EqualTo(0));
            Assert.That(result.Trace.Bus.Data, Is.EqualTo((byte)OpcodeBaseCode.NOP));
            Assert.That(result.Trace.Bus.Direction, Is.EqualTo(BusDirection.Read));
        }

        [Test]
        public void RegisterAlu_TraceHasFlagAndRegisterChanges()
        {
            // ADD r0, r0 (opcode 0x50 = ADD base, src=0, dst=0)
            // With r0 = 0x80, result = 0x80 + 0x80 = 0x100 → wraps to 0x00, carry=true
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.ADD],
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0x80);

            // First tick: FetchOpcode
            cpu.Tick();
            // Second tick: AluOp
            var result = cpu.Tick();

            Assert.That(result.Trace, Is.Not.Null);
            Assert.That(result.Trace!.Phase, Is.EqualTo(MicroPhase.AluOp));
            Assert.That(result.Trace.Type, Is.EqualTo(TickType.Internal));

            // Register r0 changed from 0x80 to 0x00
            Assert.That(result.Trace.RegisterChanges, Has.Length.EqualTo(1));
            Assert.That(result.Trace.RegisterChanges[0].Index, Is.EqualTo(0));
            Assert.That(result.Trace.RegisterChanges[0].OldValue, Is.EqualTo(0x80));
            Assert.That(result.Trace.RegisterChanges[0].NewValue, Is.EqualTo(0x00));

            // Carry flag: false → true (overflow)
            Assert.That(result.Trace.CarryFlagBefore, Is.False);
            Assert.That(result.Trace.CarryFlagAfter, Is.True);
        }

        [Test]
        public void MemoryWrite_TraceHasBusWrite()
        {
            // STA r0, [0x10] — store r0 value to address 0x10
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.STA, .. OpcodeTestHelpers.GetAddress(0x10)],
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0x42);

            // Tick through the instruction collecting traces
            var traces = new List<TickTrace>();
            MicrocodeTickResult result;
            do
            {
                result = cpu.Tick();
                if (result.Trace != null) traces.Add(result.Trace);
            } while (!result.IsInstructionComplete);

            // Find the MemoryWrite trace
            var writeTrace = traces.Find(t => t.Phase == MicroPhase.MemoryWrite);
            Assert.That(writeTrace, Is.Not.Null);
            Assert.That(writeTrace!.Type, Is.EqualTo(TickType.Bus));
            Assert.That(writeTrace.Bus, Is.Not.Null);
            Assert.That(writeTrace.Bus!.Address, Is.EqualTo(0x10));
            Assert.That(writeTrace.Bus.Data, Is.EqualTo(0x42));
            Assert.That(writeTrace.Bus.Direction, Is.EqualTo(BusDirection.Write));
        }

        [Test]
        public void Step_CollectsAllTraces()
        {
            // LDA r0, [0x10] — multi-tick instruction
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.LDA, .. OpcodeTestHelpers.GetAddress(0x10)],
                out _,
                out _,
                out _);

            cpu.Step();
            var inspector = cpu.GetInspector();

#if x16
            // FetchOpcode + FetchOperand16Low + FetchOperand16High + MemoryRead = 4 traces
            Assert.That(inspector.Traces, Has.Length.EqualTo(4));
#else
            // FetchOpcode + FetchOperand + MemoryRead = 3 traces
            Assert.That(inspector.Traces, Has.Length.EqualTo(3));
#endif
            Assert.That(inspector.Traces[0].Phase, Is.EqualTo(MicroPhase.FetchOpcode));
        }

        private static MicroPhase[] TickSequence(CPU cpu)
        {
            var phases = new List<MicroPhase>();
            MicrocodeTickResult result;
            do
            {
                result = cpu.Tick();
                phases.Add(result.NextPhase);
            } while (!result.IsInstructionComplete);
            return [.. phases];
        }

    }
}
