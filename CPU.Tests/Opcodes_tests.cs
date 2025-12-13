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
                program: [(byte)Opcode.MOV | 0b0000 | 0b0001], // MOV R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(1, 42); // Set R1 to a known value

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
        public void LDI_R0_100_LoadsImmediateValueIntoR0()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)Opcode.LDI | 0b0000, 0x01], // LDI R0, IMM: 1
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
                program: [(byte)Opcode.HLT],
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
                program: [(byte)Opcode.NOP],
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
        public void LDR_R0_0_LoadsValueFromMemoryIntoR0()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)Opcode.LDR | 0b0000, 0x02], // LDR R0, ADDR: 2
                out var state,
                out _,
                out var memory);
            memory.WriteByte(0x02, 1); // Set memory at address 0 to 1

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
                program: [(byte)Opcode.STR | 0b0000, 0x00], // STR R0, ADDR: 0
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
}
