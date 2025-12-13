using CPU.opcodes;
using NUnit.Framework;

namespace CPU.Tests
{
    [TestFixture]
    public class MOV_tests
    {
        [Test]
        public void MOV_R0_R1_CopiesValueFromR1ToR0()
        {
            // Arrange
            var cpu = new CPU();
            cpu.State.SetRegister(1, 42); // Set R1 to a known value
            byte[] program = [(byte)Opcode.MOV | 0b0000 | 0b0001]; // MOV R0, R1
            cpu.LoadProgram(program);

            // Act
            cpu.Step(traceEnabled: false);
            // Assert
            Assert.That(cpu.State.GetRegister(0), Is.EqualTo(42), "R0 should contain the value copied from R1.");
        }
    }

    [TestFixture]
    public class LDI_tests
    {
        [Test]
        public void LDI_R0_100_LoadsImmediateValueIntoR0()
        {
            // Arrange
            var cpu = new CPU();
            byte[] program = [(byte)Opcode.LDI | 0b0000, 0x01]; // LDI R0, 100
            cpu.LoadProgram(program);
            // Act
            cpu.Step(traceEnabled: false);
            // Assert
            Assert.That(cpu.State.GetRegister(0), Is.EqualTo(0x01), "R0 should contain the immediate value 1.");
        }
    }
    
    [TestFixture]
    public class HLT_tests
    {
        [Test]
        public void HLT_ThrowsHaltException()
        {
            // Arrange
            var cpu = new CPU();
            byte[] program = [(byte)Opcode.HLT];
            cpu.LoadProgram(program);
            // Act & Assert
            Assert.Throws<OpcodeException.HaltException>(() => cpu.Step(traceEnabled: false));
        }
    }
}
