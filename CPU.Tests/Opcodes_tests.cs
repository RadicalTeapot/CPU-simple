using CPU.components;
using CPU.opcodes;
using NUnit.Framework;

namespace CPU.Tests
{
    internal static class OpcodeTestHelpers
    {
        public static CPU CreateCPUWithProgram(byte[] program, out State state, out Stack stack, out Memory memory)
        {
            state = new State(4);
            stack = new Stack(16, 0xF);
            memory = new Memory(256);
            var cpu = new CPU(state, stack, memory);
            cpu.LoadProgram(program);
            return cpu;
        }
    }

    [TestFixture]
    public class MOV_tests
    {
        [Test]
        public void MOV_R0_R1_CopiesValueFromR1ToR0()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.MOV | 0b0000 | 0b0001], // MOV R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 42); // Set R0 to a known value

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(42), "R0 should contain the value copied from R1.");
            Assert.That(state.GetRegister(1), Is.EqualTo(42), "R1 should remain unchanged.");
            Assert.That(state.PC, Is.EqualTo(1), "PC should increment by 1 after MOV instruction.");
        }
    }

    [TestFixture]
    public class LDI_tests
    {
        [Test]
        public void LDI_R0_1_LoadsImmediateValueIntoR0()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.LDI | 0b0000, 0x01], // LDI R0, IMM: 1
                out var state,
                out _,
                out _);
            
            // Act
            cpu.Step(traceEnabled: false);
            
            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0x01), "R0 should contain the immediate value 1.");
            Assert.That(state.PC, Is.EqualTo(2), "PC should increment by 2 after LDI instruction.");
        }
    }
    
    [TestFixture]
    public class HLT_tests
    {
        [Test]
        public void HLT_ThrowsHaltException()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.HLT],
                out var state,
                out _,
                out _);

            // Act & Assert
            Assert.Throws<OpcodeException.HaltException>(() => cpu.Step(traceEnabled: false));
            Assert.That(state.PC, Is.EqualTo(0), "PC should halt before PC increment.");
        }
    }

    [TestFixture]
    public class NOP_tests
    {
        [Test]
        public void NOP_DoesNotChangeState()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.NOP],
                out var state,
                out _,
                out _);

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.PC, Is.EqualTo(1), "PC should increment by 1 after NOP.");
        }
    }

    [TestFixture]
    public class LDR_tests
    {
        [Test]
        public void LDR_R0_2_LoadsValueFromMemoryIntoR0()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.LDR | 0b0000, 0x02], // LDR R0, ADDR: 2
                out var state,
                out _,
                out var memory);
            memory.WriteByte(0x02, 1); // Set memory at address 2 to 1

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(1), "R0 should contain the value loaded from memory.");
            Assert.That(state.PC, Is.EqualTo(2), "PC should increment by 2 after LDR instruction.");
        }
    }

    [TestFixture]
    public class STR_tests
    {
        [Test]
        public void STR_R0_0_StoresValueFromR0IntoMemory()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.STR | 0b0000, 0x00], // STR R0, ADDR: 0
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 1); // Set R0 to 1

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(memory.ReadByte(0), Is.EqualTo(1), "Memory at address 0 should contain the value from R0.");
            Assert.That(state.PC, Is.EqualTo(2), "PC should increment by 2 after STR instruction.");
        }
    }

    [TestFixture]
    public class ADI_tests
    {
        [Test]
        public void ADI_R0_1_AddValueToR0()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.ADI | 0b0000, 0x01], // ADI R0, 1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 1); // Set R0 to 1

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(1 + 1));
            Assert.That(state.C, Is.False, "Carry flag should not be set.");
            Assert.That(state.Z, Is.False, "Zero flag should not be set.");
            Assert.That(state.PC, Is.EqualTo(2), "PC should increment by 2 after ADI instruction.");
        }

        [Test]
        public void ADI_R0_255_SetsCarryFlag()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.ADI | 0b0000, 0xFF], // ADI R0, 255
                out var state,
                out _,
                out _);
            state.SetRegister(0, 1); // Set R0 to 1

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0)); // 1 + 255 = 256 -> 0 (byte overflow)
            Assert.That(state.C, Is.True, "Carry flag should be set.");
        }

        [Test]
        public void ADI_R0_255_SetsZeroFlag()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.ADI | 0b0000, 0], // ADI R0, 0
                out var state,
                out _,
                out _);
            
            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0));
            Assert.That(state.Z, Is.True, "Zero flag should be set.");
        }
    }
}
