using NUnit.Framework;
using CPU.components;
using CPU.opcodes;

namespace CPU.Tests
{
    internal static class OpcodeTestHelpers
    {
#if x16
        public static byte AddressSize = 2;
        public static byte[] GetAddress(ushort address) => [(byte)(address & 0xFF), (byte)((address >> 8) & 0xFF)];
#else
        public static byte AddressSize = 1;
        public static byte[] GetAddress(ushort address) => [(byte)(address & 0xFF)];
#endif
        public static CPU CreateCPUWithProgram(byte[] program, out State state, out components.Stack stack, out Memory memory)
        {
            state = new State(4);
            stack = new components.Stack(16);
            memory = new Memory(256);
            var cpu = new CPU(state, stack, memory);
            cpu.LoadProgram(program);
            return cpu;
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
            Assert.That(state.GetPC(), Is.EqualTo(1), "PC should increment by 1 after NOP.");
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
            Assert.That(state.GetPC(), Is.EqualTo(0), "PC should halt before PC increment.");
        }
    }

    [TestFixture]
    public class JMP_tests
    {
        [Test]
        public void JMP_ToAddress_SetsPC()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.JMP, 0x05], // JMP to address 5
                out var state,
                out _,
                out _);

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.GetPC(), Is.EqualTo(5), "PC should be set to the target address after JMP.");
        }
    }

    [TestFixture]
    public class CAL_tests
    {
        [Test]
        public void CAL_ToAddress_CallsSubroutineAtAddress1()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.CAL, .. OpcodeTestHelpers.GetAddress(5)], // CAL R0, ADDR: 5
                out var state,
                out var stack,
                out _);

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.GetPC(), Is.EqualTo(5), "PC should be set to the target address after CAL.");
            Assert.That(stack.SP, Is.EqualTo(stack.Size - 1 - OpcodeTestHelpers.AddressSize), "Stack pointer should have been decremented.");
            var expectedReturnAddress = 1 + OpcodeTestHelpers.AddressSize; // instruction + address size
            Assert.That(stack.PeekAddress(), Is.EqualTo(expectedReturnAddress), "Return address should point to next instruction");
        }
    }

    [TestFixture]
    public class RET_tests
    {
        [Test]
        public void RET_ToAddress_SetsPC()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.RET], // RET
                out var state,
                out var stack,
                out _);
            stack.PushAddress(5);

            // Act
            cpu.Step(traceEnabled: false);

            // Arrange
            Assert.That(state.GetPC(), Is.EqualTo(5), "PC should be set to the address popped from the stack after RET.");
            Assert.That(stack.SP, Is.EqualTo(stack.Size - 1), "Stack pointer should have been incremented.");
        }
    }

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
            Assert.That(state.GetPC(), Is.EqualTo(1), "PC should increment by 1 after MOV instruction.");
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
            Assert.That(state.GetPC(), Is.EqualTo(2), "PC should increment by 2 after LDI instruction.");
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
                program: [(byte)OpcodeBaseCode.LDR | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // LDR R0, ADDR: 16
                out var state,
                out _,
                out var memory);
            memory.WriteByte(0x10, 1); // Set memory at address 16 to 1

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(1), "R0 should contain the value loaded from memory.");
            var expectedPc = 1 + OpcodeTestHelpers.AddressSize; // instruction + address size
            Assert.That(state.GetPC(), Is.EqualTo(expectedPc), $"PC should increment by {OpcodeTestHelpers.AddressSize} after LDR instruction.");
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
            var expectedPc = 1 + OpcodeTestHelpers.AddressSize; // instruction + address size
            Assert.That(state.GetPC(), Is.EqualTo(expectedPc), $"PC should increment by {expectedPc} after STR instruction.");
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
            Assert.That(state.GetRegister(0), Is.EqualTo(2), "Result should be R0 + 1 + carry = 1 + 1 + 0 = 2");
            Assert.That(state.C, Is.False, "Carry flag should not be set.");
            Assert.That(state.Z, Is.False, "Zero flag should not be set.");
            Assert.That(state.GetPC(), Is.EqualTo(2), "PC should increment by 2 after ADI instruction.");
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
            Assert.That(state.GetRegister(0), Is.EqualTo(0), "Result should be R0 + 255 + carry = 1 + 255 + 0 = 0 (overflow)");
            Assert.That(state.C, Is.True, "Carry flag should be set.");
        }

        [Test]
        public void ADI_R0_0_SetsZeroFlag()
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
            Assert.That(state.GetRegister(0), Is.EqualTo(0), "Result should be R0 + 0 + carry = 0 + 0 + 0 = 0");
            Assert.That(state.Z, Is.True, "Zero flag should be set.");
        }

        [Test]
        public void ADI_R0_WithCarry_UsesCarry()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.ADI | 0b0000, 0], // ADI R0, 0
                out var state,
                out _,
                out _);
            state.SetCarryFlag(true);

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(1), "Result should be R0 + 0 + carry = 0 + 0 + 1 = 1");
        }
    }

    [TestFixture]
    public class SBI_tests
    {
        [Test]
        public void SBI_R0_1_Subtract1FromR0()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.SBI | 0b0000, 0x01], // SBI R0, 1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 2); // Set R0 to 2
            state.SetCarryFlag(true); // No borrow carry

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(1), "Result shoud be R0 - 1 - (1 - carry) = 2 - 1 - 0 = 1");
            Assert.That(state.C, Is.True, "Carry flag should be set (uses a no borrow carry).");
            Assert.That(state.Z, Is.False, "Zero flag should not be set.");
            Assert.That(state.GetPC(), Is.EqualTo(2), "PC should increment by 2 after SBI instruction.");
        }

        [Test]
        public void SBI_1_SetsZeroFlag()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.SBI | 0b0000, 0x01], // SBI R0, 1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 1); // Set R0 to 1
            state.SetCarryFlag(true); // No borrow carry

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0), "Result should be R0 - 1 - (1 - carry) = 1 - 1 - 0 = 0");
            Assert.That(state.Z, Is.True, "Zero flag should be set.");
        }

        [Test]
        public void SBI_NoCarry_UsesBorrow()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.SBI | 0b0000, 0x00], // SBI R0, 0
                out var state,
                out _,
                out _);
            state.SetRegister(0, 1); // Set R0 to 1
            state.SetCarryFlag(false); // No borrow carry

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0), "Result should be R0 - 0 - (1 - carry) = 1 - 0 - 1 = 0");
        }

        [Test]
        public void SBI_1_DoNotSetCarryFlagIfBorrow()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.SBI | 0b0000, 0x01], // SBI R0, 1
                out var state,
                out _,
                out _);
            state.SetCarryFlag(true); // No borrow carry

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(255), "Result should be R0 - 1 - (1 - carry) = 0 - 1 - 0 = 255 (underflow)");
            Assert.That(state.C, Is.False, "Carry flag should not be set (uses a no borrow carry).");
        }
    }

    [TestFixture]
    public class CLC_tests
    {
        [Test]
        public void CLC_ClearsCarryFlag()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.CLC], // CLC
                out var state,
                out _,
                out _);
            state.SetCarryFlag(true);

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.C, Is.False, "Carry flag should have been cleared");
            Assert.That(state.GetPC(), Is.EqualTo(1), "PC should increment by 1 after CLC instruction");
        }
    }

    [TestFixture]
    public class CMP_tests
    {
        [Test]
        public void CMP_R0_R1_SetsZeroFlagWhenEqual()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.CMP | 0b0000 | 0b0001], // CMP R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 1);
            state.SetRegister(1, 1);
            state.SetZeroFlag(true);

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.Z, Is.True, "Zero flag should be set when R0 equals R1.");
            Assert.That(state.GetPC(), Is.EqualTo(1), "PC should increment by 1 after CMP instruction.");
        }

        [Test]
        public void CMP_RO_R1_ClearsZeroFlagWhenEqual()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.CMP | 0b0000 | 0b0001], // CMP R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 1);
            state.SetRegister(1, 2);
            state.SetZeroFlag(true);

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.Z, Is.False, "Zero flag should be cleared when R0 equals R1.");
        }

        [Test]
        public void CMP_R0_R1_SetsCarryFlagWhenDestinationGreaterThanSource()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.CMP | 0b0000 | 0b0001], // CMP R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 1); // source
            state.SetRegister(1, 2); // destination
            state.SetCarryFlag(false);

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.C, Is.True, "Carry flag should be set when destination > source.");
        }

        [Test]
        public void CMP_R0_R1_SetsCarryFlagWhenDestinationEqualsSource()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.CMP | 0b0000 | 0b0001], // CMP R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 5); // source
            state.SetRegister(1, 5); // destination
            state.SetCarryFlag(false);

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.C, Is.True, "Carry flag should be set when destination == source.");
        }

        [Test]
        public void CMP_R0_R1_ClearsCarryFlagWhenDestinationLessThanSource()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.CMP | 0b0000 | 0b0001], // CMP R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 2); // source
            state.SetRegister(1, 1); // destination
            state.SetCarryFlag(true);

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.C, Is.False, "Carry flag should be cleared when destination < source.");
        }
    }


    [TestFixture]
    public class JCC_tests
    {
        [Test]
        public void JCC_JumpsToAddress_WhenCarryFlagIsFalse()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.JCC, .. OpcodeTestHelpers.GetAddress(0x10)], // JCC to address 16
                out var state,
                out _,
                out _);
            state.SetCarryFlag(false);

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.GetPC(), Is.EqualTo(0x10), "PC should be set to the target address when carry flag is false.");
        }

        [Test]
        public void JCC_SkipsJump_WhenCarryFlagIsTrue()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.JCC, .. OpcodeTestHelpers.GetAddress(0x10)], // JCC to address 16
                out var state,
                out _,
                out _);
            state.SetCarryFlag(true);

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            var expectedPc = 1 + OpcodeTestHelpers.AddressSize; // instruction + address size
            Assert.That(state.GetPC(), Is.EqualTo(expectedPc), "PC should skip the jump address when carry flag is true.");
        }
    }

    [TestFixture]
    public class JCS_tests
    {
        [Test]
        public void JCS_JumpsToAddress_WhenCarryFlagIsTrue()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.JCS, .. OpcodeTestHelpers.GetAddress(0x10)], // JCS to address 16
                out var state,
                out _,
                out _);
            state.SetCarryFlag(true);

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.GetPC(), Is.EqualTo(0x10), "PC should be set to the target address when carry flag is true.");
        }

        [Test]
        public void JCS_SkipsJump_WhenCarryFlagIsFalse()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.JCS, .. OpcodeTestHelpers.GetAddress(0x10)], // JCS to address 16
                out var state,
                out _,
                out _);
            state.SetCarryFlag(false);

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            var expectedPc = 1 + OpcodeTestHelpers.AddressSize; // instruction + address size
            Assert.That(state.GetPC(), Is.EqualTo(expectedPc), "PC should skip the jump address when carry flag is false.");
        }
    }

    [TestFixture]
    public class JZC_tests
    {
        [Test]
        public void JZC_JumpsToAddress_WhenZeroFlagIsFalse()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.JZC, .. OpcodeTestHelpers.GetAddress(0x10)], // JZC to address 16
                out var state,
                out _,
                out _);
            state.SetZeroFlag(false);

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.GetPC(), Is.EqualTo(0x10), "PC should be set to the target address when zero flag is false.");
        }

        [Test]
        public void JZC_SkipsJump_WhenZeroFlagIsTrue()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.JZC, .. OpcodeTestHelpers.GetAddress(0x10)], // JZC to address 16
                out var state,
                out _,
                out _);
            state.SetZeroFlag(true);

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            var expectedPc = 1 + OpcodeTestHelpers.AddressSize; // instruction + address size
            Assert.That(state.GetPC(), Is.EqualTo(expectedPc), "PC should skip the jump address when zero flag is true.");
        }
    }

    [TestFixture]
    public class JZS_tests
    {
        [Test]
        public void JZS_JumpsToAddress_WhenZeroFlagIsTrue()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.JZS, .. OpcodeTestHelpers.GetAddress(0x10)], // JZS to address 16
                out var state,
                out _,
                out _);
            state.SetZeroFlag(true);

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            Assert.That(state.GetPC(), Is.EqualTo(0x10), "PC should be set to the target address when zero flag is true.");
        }

        [Test]
        public void JZS_SkipsJump_WhenZeroFlagIsFalse()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.JZS, .. OpcodeTestHelpers.GetAddress(0x10)], // JZS to address 16
                out var state,
                out _,
                out _);
            state.SetZeroFlag(false);

            // Act
            cpu.Step(traceEnabled: false);

            // Assert
            var expectedPc = 1 + OpcodeTestHelpers.AddressSize; // instruction + address size
            Assert.That(state.GetPC(), Is.EqualTo(expectedPc), "PC should skip the jump address when zero flag is false.");
        }
    }
}
