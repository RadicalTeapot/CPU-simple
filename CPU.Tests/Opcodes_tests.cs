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
            cpu.Step();

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
            Assert.Throws<OpcodeException.HaltException>(() => cpu.Step());
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
            cpu.Step();

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
            var initialSP = stack.SP;

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetPC(), Is.EqualTo(5), "PC should be set to the target address after CAL.");
            Assert.That(stack.SP, Is.EqualTo(initialSP - OpcodeTestHelpers.AddressSize), "Stack pointer should have been decremented.");
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
            var initialSP = stack.SP;

            // Act
            cpu.Step();

            // Arrange
            Assert.That(state.GetPC(), Is.EqualTo(5), "PC should be set to the address popped from the stack after RET.");
            Assert.That(stack.SP, Is.EqualTo(initialSP + OpcodeTestHelpers.AddressSize), "Stack pointer should have been incremented.");
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
            cpu.Step();

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
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0x01), "R0 should contain the immediate value 1.");
            Assert.That(state.GetPC(), Is.EqualTo(2), "PC should increment by 2 after LDI instruction.");
        }
    }

    [TestFixture]
    public class LDA_tests
    {
        [Test]
        public void LDA_R0_2_LoadsValueFromMemoryIntoR0()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.LDA | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // LDR R0, ADDR: 16
                out var state,
                out _,
                out var memory);
            memory.WriteByte(0x10, 1); // Set memory at address 16 to 1

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(1), "R0 should contain the value loaded from memory.");
            var expectedPc = 1 + OpcodeTestHelpers.AddressSize; // instruction + address size
            Assert.That(state.GetPC(), Is.EqualTo(expectedPc), $"PC should increment by {OpcodeTestHelpers.AddressSize} after LDR instruction.");
        }
    }

    [TestFixture]
    public class STA_tests
    {
        [Test]
        public void STA_R0_0_StoresValueFromR0IntoMemory()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.STA | 0b0000, 0x00], // STR R0, ADDR: 0
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 1); // Set R0 to 1

            // Act
            cpu.Step();

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
            cpu.Step();

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
            cpu.Step();

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
            cpu.Step();

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
            cpu.Step();

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
            cpu.Step();

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
            cpu.Step();

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
            cpu.Step();

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
            cpu.Step();

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
            cpu.Step();

            // Assert
            Assert.That(state.C, Is.False, "Carry flag should have been cleared");
            Assert.That(state.GetPC(), Is.EqualTo(1), "PC should increment by 1 after CLC instruction");
        }
    }

    [TestFixture]
    public class SEC_tests
    {
        [Test]
        public void SEC_SetsCarryFlag()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.SEC], // SEC
                out var state,
                out _,
                out _);
            state.SetCarryFlag(false);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.C, Is.True, "Carry flag should have been set");
            Assert.That(state.GetPC(), Is.EqualTo(1), "PC should increment by 1 after SEC instruction");
        }
    }

    [TestFixture]
    public class CLZ_tests
    {
        [Test]
        public void CLZ_ClearsZeroFlag()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.CLZ], // CLZ
                out var state,
                out _,
                out _);
            state.SetZeroFlag(true);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.Z, Is.False, "Zero flag should have been cleared");
            Assert.That(state.GetPC(), Is.EqualTo(1), "PC should increment by 1 after CLZ instruction");
        }
    }

    [TestFixture]
    public class SEZ_tests
    {
        [Test]
        public void SEZ_SetsZeroFlag()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.SEZ], // SEZ
                out var state,
                out _,
                out _);
            state.SetZeroFlag(false);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.Z, Is.True, "Zero flag should have been set");
            Assert.That(state.GetPC(), Is.EqualTo(1), "PC should increment by 1 after SEZ instruction");
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
                program: [(byte)OpcodeBaseCode.CMP | 0b0001], // CMP R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 1);
            state.SetRegister(1, 1);
            state.SetZeroFlag(true);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.Z, Is.True, "Zero flag should be set when R0 equals R1.");
            Assert.That(state.GetPC(), Is.EqualTo(1), "PC should increment by 1 after CMP instruction.");
        }

        [Test]
        public void CMP_RO_R1_ClearsZeroFlagWhenEqual()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.CMP | 0b0001], // CMP R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 1);
            state.SetRegister(1, 2);
            state.SetZeroFlag(true);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.Z, Is.False, "Zero flag should be cleared when R0 equals R1.");
        }

        [Test]
        public void CMP_R0_R1_SetsCarryFlagWhenDestinationGreaterThanSource()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.CMP | 0b0001], // CMP R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 1); // source
            state.SetRegister(1, 2); // destination
            state.SetCarryFlag(false);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.C, Is.True, "Carry flag should be set when destination > source.");
        }

        [Test]
        public void CMP_R0_R1_SetsCarryFlagWhenDestinationEqualsSource()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.CMP | 0b0001], // CMP R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 5); // source
            state.SetRegister(1, 5); // destination
            state.SetCarryFlag(false);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.C, Is.True, "Carry flag should be set when destination == source.");
        }

        [Test]
        public void CMP_R0_R1_ClearsCarryFlagWhenDestinationLessThanSource()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.CMP | 0b0001], // CMP R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 2); // source
            state.SetRegister(1, 1); // destination
            state.SetCarryFlag(true);

            // Act
            cpu.Step();

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
            cpu.Step();

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
            cpu.Step();

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
            cpu.Step();

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
            cpu.Step();

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
            cpu.Step();

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
            cpu.Step();

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
            cpu.Step();

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
            cpu.Step();

            // Assert
            var expectedPc = 1 + OpcodeTestHelpers.AddressSize; // instruction + address size
            Assert.That(state.GetPC(), Is.EqualTo(expectedPc), "PC should skip the jump address when zero flag is false.");
        }
    }

    [TestFixture]
    public class PSH_tests
    {
        [Test]
        public void PSH_R0_PushesValueOntoStack()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.PSH | 0b0000], // PSH R0
                out var state,
                out var stack,
                out _);
            state.SetRegister(0, 1);
            var initialSP = stack.SP;

            // Act
            cpu.Step();

            // Assert
            Assert.That(stack.PeekByte(), Is.EqualTo(1), "Stack should contain the value from R0.");
            Assert.That(stack.SP, Is.EqualTo(initialSP - 1), "Stack pointer should have been decremented.");
            Assert.That(state.GetPC(), Is.EqualTo(1), "PC should increment by 1 after PSH instruction.");
        }

        [Test]
        public void PSH_R0_DoesNotModifyRegister()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.PSH | 0b0000], // PSH R0
                out var state,
                out _,
                out _);
            state.SetRegister(0, 1);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(1), "R0 should remain unchanged after PSH.");
        }
    }

    [TestFixture]
    public class PEK_tests
    {
        [Test]
        public void PEK_R0_PeeksValueFromStack()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.PEK | 0b0000], // PEK R0
                out var state,
                out var stack,
                out _);
            stack.PushByte(1);
            var initialSP = stack.SP;

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(1), "R0 should contain the value peeked from stack.");
            Assert.That(stack.SP, Is.EqualTo(initialSP), "Stack pointer should remain unchanged after PEK.");
            Assert.That(state.GetPC(), Is.EqualTo(1), "PC should increment by 1 after PEK instruction.");
        }

        [Test]
        public void PEK_R0_DoesNotRemoveValueFromStack()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.PEK | 0b0000], // PEK R0
                out _,
                out var stack,
                out _);
            stack.PushByte(1);

            // Act
            cpu.Step();

            // Assert
            Assert.That(stack.PeekByte(), Is.EqualTo(1), "Stack value should still be present after PEK.");
        }
    }

    [TestFixture]
    public class POP_tests
    {
        [Test]
        public void POP_R0_PopsValueFromStack()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.POP | 0b0000], // POP R0
                out var state,
                out var stack,
                out _);
            stack.PushByte(1);
            var initialSP = stack.SP;

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(1), "R0 should contain the value popped from stack.");
            Assert.That(stack.SP, Is.EqualTo(initialSP + 1), "Stack pointer should have been incremented after POP.");
            Assert.That(state.GetPC(), Is.EqualTo(1), "PC should increment by 1 after POP instruction.");
        }

        [Test]
        public void POP_R0_RemovesValueFromStack()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.POP | 0b0000], // POP R0
                out var state,
                out var stack,
                out _);
            stack.PushByte(1);
            stack.PushByte(2);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(2), "R0 should contain the top value.");
            Assert.That(stack.PeekByte(), Is.EqualTo(1), "Stack should now have previous value at top.");
        }
    }

    [TestFixture]
    public class ADD_tests
    {
        [Test]
        public void ADD_R0_R1_AddsRegisters()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)((byte)OpcodeBaseCode.ADD | 0b0001)], // ADD R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 5);
            state.SetRegister(1, 3);
            state.SetCarryFlag(false);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(1), Is.EqualTo(8), "Result should be R0 + R1 + carry = 5 + 3 + 0 = 8");
            Assert.That(state.C, Is.False, "Carry flag should not be set.");
            Assert.That(state.Z, Is.False, "Zero flag should not be set.");
            Assert.That(state.GetPC(), Is.EqualTo(1), "PC should increment by 1 after ADD instruction.");
        }

        [Test]
        public void ADD_R0_R1_SetsCarryFlagOnOverflow()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)((byte)OpcodeBaseCode.ADD | 0b0001)], // ADD R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 2);
            state.SetRegister(1, 255);
            state.SetCarryFlag(false);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(1), Is.EqualTo(1), "Result should be (2 + 255) mod 256 = 1");
            Assert.That(state.C, Is.True, "Carry flag should be set on overflow.");
        }

        [Test]
        public void ADD_R0_R1_SetsZeroFlag()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)((byte)OpcodeBaseCode.ADD | 0b0001)], // ADD R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0);
            state.SetRegister(1, 0);
            state.SetCarryFlag(false);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0), "Result should be 0");
            Assert.That(state.Z, Is.True, "Zero flag should be set.");
        }

        [Test]
        public void ADD_R0_R1_WithCarry_UsesCarry()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)((byte)OpcodeBaseCode.ADD | 0b0001)], // ADD R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 5);
            state.SetRegister(1, 3);
            state.SetCarryFlag(true);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(1), Is.EqualTo(9), "Result should be R0 + R1 + carry = 5 + 3 + 1 = 9");
        }
    }

    [TestFixture]
    public class SUB_tests
    {
        [Test]
        public void SUB_R0_R1_SubtractsRegisters()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)((byte)OpcodeBaseCode.SUB | 0b0001)], // SUB R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 3);
            state.SetRegister(1, 10);
            state.SetCarryFlag(true); // No borrow

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(1), Is.EqualTo(7), "Result should be R1 - R0 - (1 - carry) = 10 - 3 - 0 = 7");
            Assert.That(state.C, Is.True, "Carry flag should be set (no borrow).");
            Assert.That(state.Z, Is.False, "Zero flag should not be set.");
            Assert.That(state.GetPC(), Is.EqualTo(1), "PC should increment by 1 after SUB instruction.");
        }

        [Test]
        public void SUB_R0_R1_ClearsCarryFlagOnBorrow()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)((byte)OpcodeBaseCode.SUB | 0b0001)], // SUB R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 2);
            state.SetRegister(1, 1);
            state.SetCarryFlag(true); // No borrow

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(1), Is.EqualTo(255), "Result should be (1 - 2) mod 256 = 255");
            Assert.That(state.C, Is.False, "Carry flag should be cleared (borrow occurred).");
        }

        [Test]
        public void SUB_R0_R1_SetsZeroFlag()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)((byte)OpcodeBaseCode.SUB | 0b0001)], // SUB R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 5);
            state.SetRegister(1, 5);
            state.SetCarryFlag(true); // No borrow

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(1), Is.EqualTo(0), "Result should be 0");
            Assert.That(state.Z, Is.True, "Zero flag should be set.");
        }

        [Test]
        public void SUB_R0_R1_WithBorrow_UsesBorrow()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)((byte)OpcodeBaseCode.SUB | 0b0001)], // SUB R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 3);
            state.SetRegister(1, 10);
            state.SetCarryFlag(false); // Borrow

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(1), Is.EqualTo(6), "Result should be R0 - R1 - (1 - carry) = 10 - 3 - 1 = 6");
        }
    }

    [TestFixture]
    public class ADA_tests
    {
        [Test]
        public void ADA_R0_AddsMemoryValueToRegister()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.ADA | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // ADA R0, ADDR: 16
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 5);
            memory.WriteByte(0x10, 3);
            state.SetCarryFlag(false);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(8), "Result should be R0 + mem[16] + carry = 5 + 3 + 0 = 8");
            Assert.That(state.C, Is.False, "Carry flag should not be set.");
            Assert.That(state.Z, Is.False, "Zero flag should not be set.");
            var expectedPc = 1 + OpcodeTestHelpers.AddressSize;
            Assert.That(state.GetPC(), Is.EqualTo(expectedPc), $"PC should increment by {expectedPc} after ADA instruction.");
        }

        [Test]
        public void ADA_R0_SetsCarryFlagOnOverflow()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.ADA | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // ADA R0, ADDR: 16
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 2);
            memory.WriteByte(0x10, 255);
            state.SetCarryFlag(false);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(1), "Result should be (2 + 255) mod 256 = 1");
            Assert.That(state.C, Is.True, "Carry flag should be set on overflow.");
        }

        [Test]
        public void ADA_R0_SetsZeroFlag()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.ADA | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // ADA R0, ADDR: 16
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 0);
            memory.WriteByte(0x10, 0);
            state.SetCarryFlag(false);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0), "Result should be 0");
            Assert.That(state.Z, Is.True, "Zero flag should be set.");
        }

        [Test]
        public void ADA_R0_WithCarry_UsesCarry()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.ADA | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // ADA R0, ADDR: 16
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 5);
            memory.WriteByte(0x10, 3);
            state.SetCarryFlag(true);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(9), "Result should be R0 + mem[16] + carry = 5 + 3 + 1 = 9");
        }
    }

    [TestFixture]
    public class SBA_tests
    {
        [Test]
        public void SBA_R0_SubtractsMemoryValueFromRegister()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.SBA | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // SBA R0, ADDR: 16
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 10);
            memory.WriteByte(0x10, 3);
            state.SetCarryFlag(true); // No borrow

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(7), "Result should be R0 - mem[16] - (1 - carry) = 10 - 3 - 0 = 7");
            Assert.That(state.C, Is.True, "Carry flag should be set (no borrow).");
            Assert.That(state.Z, Is.False, "Zero flag should not be set.");
            var expectedPc = 1 + OpcodeTestHelpers.AddressSize;
            Assert.That(state.GetPC(), Is.EqualTo(expectedPc), $"PC should increment by {expectedPc} after SBA instruction.");
        }

        [Test]
        public void SBA_R0_ClearsCarryFlagOnBorrow()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.SBA | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // SBA R0, ADDR: 16
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 1);
            memory.WriteByte(0x10, 2);
            state.SetCarryFlag(true); // No borrow

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(255), "Result should be (1 - 2) mod 256 = 255");
            Assert.That(state.C, Is.False, "Carry flag should be cleared (borrow occurred).");
        }

        [Test]
        public void SBA_R0_SetsZeroFlag()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.SBA | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // SBA R0, ADDR: 16
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 5);
            memory.WriteByte(0x10, 5);
            state.SetCarryFlag(true); // No borrow

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0), "Result should be 0");
            Assert.That(state.Z, Is.True, "Zero flag should be set.");
        }

        [Test]
        public void SBA_R0_WithBorrow_UsesBorrow()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.SBA | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // SBA R0, ADDR: 16
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 10);
            memory.WriteByte(0x10, 3);
            state.SetCarryFlag(false); // Borrow

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(6), "Result should be R0 - mem[16] - (1 - carry) = 10 - 3 - 1 = 6");
        }
    }

    [TestFixture]
    public class LSH_tests
    {
        [Test]
        public void LSH_R0_ShiftsLeft()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.LSH | 0b0000], // LSH R0
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b0010_1010); // 42

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0b0101_0100), "Result should be 42 << 1 = 84");
            Assert.That(state.C, Is.False, "Carry flag should not be set (bit 7 was 0).");
            Assert.That(state.GetPC(), Is.EqualTo(1), "PC should increment by 1 after LSH instruction.");
        }

        [Test]
        public void LSH_R0_SetsCarryFlagWhenBit7IsSet()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.LSH | 0b0000], // LSH R0
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b1000_0001); // 129

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0b0000_0010), "Result should be 129 << 1 = 2");
            Assert.That(state.C, Is.True, "Carry flag should be set (bit 7 was 1).");
        }

        [Test]
        public void LSH_R0_ClearsCarryFlagWhenBit7IsClear()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.LSH | 0b0000], // LSH R0
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b0111_1111); // 127
            state.SetCarryFlag(true);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0b1111_1110), "Result should be 127 << 1 = 254");
            Assert.That(state.C, Is.False, "Carry flag should be cleared (bit 7 was 0).");
        }
    }

    [TestFixture]
    public class RSH_tests
    {
        [Test]
        public void RSH_R0_ShiftsRight()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.RSH | 0b0000], // RSH R0
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b0010_1010); // 42

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0b0001_0101), "Result should be 42 >> 1 = 21");
            Assert.That(state.C, Is.False, "Carry flag should not be set (bit 0 was 0).");
            Assert.That(state.GetPC(), Is.EqualTo(1), "PC should increment by 1 after RSH instruction.");
        }

        [Test]
        public void RSH_R0_SetsCarryFlagWhenBit0IsSet()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.RSH | 0b0000], // RSH R0
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b0000_0011); // 3

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0b0000_0001), "Result should be 3 >> 1 = 1");
            Assert.That(state.C, Is.True, "Carry flag should be set (bit 0 was 1).");
        }

        [Test]
        public void RSH_R0_ClearsCarryFlagWhenBit0IsClear()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.RSH | 0b0000], // RSH R0
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b1111_1110); // 254
            state.SetCarryFlag(true);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0b0111_1111), "Result should be 254 >> 1 = 127");
            Assert.That(state.C, Is.False, "Carry flag should be cleared (bit 0 was 0).");
        }
    }

    [TestFixture]
    public class LRT_tests
    {
        [Test]
        public void LRT_R0_RotatesLeft()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.LRT | 0b0000], // LRT R0
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b0010_1010); // 42

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0b0101_0100), "Result should be 42 rotated left = 84");
            Assert.That(state.GetPC(), Is.EqualTo(1), "PC should increment by 1 after LRT instruction.");
        }

        [Test]
        public void LRT_R0_RotatesBit7ToBit0()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.LRT | 0b0000], // LRT R0
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b1000_0000); // 128

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0b0000_0001), "Result should be 128 rotated left = 1");
        }

        [Test]
        public void LRT_R0_CarryFlagUnaffected()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.LRT | 0b0000], // LRT R0
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b1000_0000); // 128
            state.SetCarryFlag(false);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.C, Is.False, "Carry flag should remain unaffected.");
        }
    }

    [TestFixture]
    public class RRT_tests
    {
        [Test]
        public void RRT_R0_RotatesRight()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.RRT | 0b0000], // RRT R0
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b0010_1010); // 42

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0b0001_0101), "Result should be 42 rotated right = 21");
            Assert.That(state.GetPC(), Is.EqualTo(1), "PC should increment by 1 after RRT instruction.");
        }

        [Test]
        public void RRT_R0_RotatesBit0ToBit7()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.RRT | 0b0000], // RRT R0
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b0000_0001); // 1

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0b1000_0000), "Result should be 1 rotated right = 128");
        }

        [Test]
        public void RRT_R0_CarryFlagUnaffected()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.RRT | 0b0000], // RRT R0
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b0000_0001); // 1
            state.SetCarryFlag(false);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.C, Is.False, "Carry flag should remain unaffected.");
        }
    }

    [TestFixture]
    public class CPI_tests
    {
        [Test]
        public void CPI_R0_SetsZeroFlagWhenEqual()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.CPI | 0b0000, 0x01], // CPI R0, 0x42
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0x01);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.Z, Is.True, "Zero flag should be set when values are equal.");
            Assert.That(state.C, Is.True, "Carry flag should be set when R0 >= immediate.");
        }

        [Test]
        public void CPI_R0_SetsCarryFlagWhenGreater()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.CPI | 0b0000, 0x01], // CPI R0, 0x01
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0x02);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.Z, Is.False, "Zero flag should be clear when values are not equal.");
            Assert.That(state.C, Is.True, "Carry flag should be set when R0 >= immediate.");
        }

        [Test]
        public void CPI_R0_ClearsCarryFlagWhenLess()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.CPI | 0b0000, 0x02], // CPI R0, 0x02
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0x01);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.Z, Is.False, "Zero flag should be clear when values are not equal.");
            Assert.That(state.C, Is.False, "Carry flag should be clear when R0 < immediate.");
        }
    }

    [TestFixture]
    public class CPA_tests
    {
        [Test]
        public void CPA_R0_SetsZeroFlagWhenEqual()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.CPA | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // CPA R0, [0x10]
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 0x01);
            memory.WriteByte(0x10, 0x01);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.Z, Is.True, "Zero flag should be set when values are equal.");
            Assert.That(state.C, Is.True, "Carry flag should be set when R0 >= memory value.");
        }

        [Test]
        public void CPA_R0_SetsCarryFlagWhenGreater()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.CPA | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // CPA R0, [0x10]
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 0x02);
            memory.WriteByte(0x10, 0x01);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.Z, Is.False, "Zero flag should be clear when values are not equal.");
            Assert.That(state.C, Is.True, "Carry flag should be set when R0 >= memory value.");
        }

        [Test]
        public void CPA_R0_ClearsCarryFlagWhenLess()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.CPA | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // CPA R0, [0x10]
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 0x01);
            memory.WriteByte(0x10, 0x02);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.Z, Is.False, "Zero flag should be clear when values are not equal.");
            Assert.That(state.C, Is.False, "Carry flag should be clear when R0 < memory value.");
        }
    }

    [TestFixture]
    public class INC_tests
    {
        [Test]
        public void INC_R0_IncrementsRegister()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.INC | 0b0000], // INC R0
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0x00);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0x01), "R0 should be incremented by 1.");
            Assert.That(state.Z, Is.False, "Zero flag should be clear when result is not zero.");
        }

        [Test]
        public void INC_R0_SetsZeroFlagOnOverflow()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.INC | 0b0000], // INC R0
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0xFF);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0x00), "R0 should wrap to 0 on overflow.");
            Assert.That(state.Z, Is.True, "Zero flag should be set when result is zero.");
        }
    }

    [TestFixture]
    public class DEC_tests
    {
        [Test]
        public void DEC_R0_DecrementsRegister()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.DEC | 0b0000], // DEC R0
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0x02);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0x01), "R0 should be decremented by 1.");
            Assert.That(state.Z, Is.False, "Zero flag should be clear when result is not zero.");
        }

        [Test]
        public void DEC_R0_SetsZeroFlagWhenResultIsZero()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.DEC | 0b0000], // DEC R0
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0x01);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0x00), "R0 should be 0.");
            Assert.That(state.Z, Is.True, "Zero flag should be set when result is zero.");
        }

        [Test]
        public void DEC_R0_WrapsOnUnderflow()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.DEC | 0b0000], // DEC R0
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0x00);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0xFF), "R0 should wrap to 0xFF on underflow.");
            Assert.That(state.Z, Is.False, "Zero flag should be clear when result is not zero.");
        }
    }

    [TestFixture]
    public class AND_tests
    {
        [Test]
        public void AND_R0_R1_PerformsBitwiseAnd()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.AND | 0b0001], // AND R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b1111_0000);
            state.SetRegister(1, 0b1010_1010);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(1), Is.EqualTo(0b1010_0000), "R1 should contain R0 AND R1.");
            Assert.That(state.Z, Is.False, "Zero flag should be clear when result is not zero.");
        }

        [Test]
        public void AND_R0_R1_SetsZeroFlagWhenResultIsZero()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.AND | 0b0001], // AND R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b1111_0000);
            state.SetRegister(1, 0b0000_1111);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(1), Is.EqualTo(0x00), "R1 should be 0.");
            Assert.That(state.Z, Is.True, "Zero flag should be set when result is zero.");
        }
    }

    [TestFixture]
    public class ANI_tests
    {
        [Test]
        public void ANI_R0_PerformsBitwiseAndWithImmediate()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.ANI | 0b0000, 0b1010_1010], // ANI R0, 0xAA
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b1111_0000);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0b1010_0000), "R0 should contain R0 AND immediate.");
            Assert.That(state.Z, Is.False, "Zero flag should be clear when result is not zero.");
        }

        [Test]
        public void ANI_R0_SetsZeroFlagWhenResultIsZero()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.ANI | 0b0000, 0b0000_1111], // ANI R0, 0x0F
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b1111_0000);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0x00), "R0 should be 0.");
            Assert.That(state.Z, Is.True, "Zero flag should be set when result is zero.");
        }
    }

    [TestFixture]
    public class ANA_tests
    {
        [Test]
        public void ANA_R0_PerformsBitwiseAndWithMemory()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.ANA | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // ANA R0, [0x10]
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 0b1111_0000);
            memory.WriteByte(0x10, 0b1010_1010);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0b1010_0000), "R0 should contain R0 AND memory value.");
            Assert.That(state.Z, Is.False, "Zero flag should be clear when result is not zero.");
        }

        [Test]
        public void ANA_R0_SetsZeroFlagWhenResultIsZero()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.ANA | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // ANA R0, [0x10]
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 0b1111_0000);
            memory.WriteByte(0x10, 0b0000_1111);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0x00), "R0 should be 0.");
            Assert.That(state.Z, Is.True, "Zero flag should be set when result is zero.");
        }
    }

    [TestFixture]
    public class OR_tests
    {
        [Test]
        public void OR_R0_R1_PerformsBitwiseOr()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.OR | 0b0001], // OR R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b1111_0000);
            state.SetRegister(1, 0b0000_1111);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(1), Is.EqualTo(0b1111_1111), "R1 should contain R0 OR R1.");
            Assert.That(state.Z, Is.False, "Zero flag should be clear when result is not zero.");
        }

        [Test]
        public void OR_R0_R1_SetsZeroFlagWhenResultIsZero()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.OR | 0b0001], // OR R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0x00);
            state.SetRegister(1, 0x00);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(1), Is.EqualTo(0x00), "R1 should be 0.");
            Assert.That(state.Z, Is.True, "Zero flag should be set when result is zero.");
        }
    }

    [TestFixture]
    public class ORI_tests
    {
        [Test]
        public void ORI_R0_PerformsBitwiseOrWithImmediate()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.ORI | 0b0000, 0b0000_1111], // ORI R0, 0x0F
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b1111_0000);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0b1111_1111), "R0 should contain R0 OR immediate.");
            Assert.That(state.Z, Is.False, "Zero flag should be clear when result is not zero.");
        }

        [Test]
        public void ORI_R0_SetsZeroFlagWhenResultIsZero()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.ORI | 0b0000, 0x00], // ORI R0, 0x00
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0x00);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0x00), "R0 should be 0.");
            Assert.That(state.Z, Is.True, "Zero flag should be set when result is zero.");
        }
    }

    [TestFixture]
    public class ORA_tests
    {
        [Test]
        public void ORA_R0_PerformsBitwiseOrWithMemory()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.ORA | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // ORA R0, [0x10]
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 0b1111_0000);
            memory.WriteByte(0x10, 0b0000_1111);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0b1111_1111), "R0 should contain R0 OR memory value.");
            Assert.That(state.Z, Is.False, "Zero flag should be clear when result is not zero.");
        }

        [Test]
        public void ORA_R0_SetsZeroFlagWhenResultIsZero()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.ORA | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // ORA R0, [0x10]
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 0x00);
            memory.WriteByte(0x10, 0x00);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0x00), "R0 should be 0.");
            Assert.That(state.Z, Is.True, "Zero flag should be set when result is zero.");
        }
    }

    [TestFixture]
    public class XOR_tests
    {
        [Test]
        public void XOR_R0_R1_PerformsBitwiseXor()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.XOR | 0b0001], // XOR R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b1111_0000);
            state.SetRegister(1, 0b1010_1010);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(1), Is.EqualTo(0b0101_1010), "R1 should contain R0 XOR R1.");
            Assert.That(state.Z, Is.False, "Zero flag should be clear when result is not zero.");
        }

        [Test]
        public void XOR_R0_R1_SetsZeroFlagWhenResultIsZero()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.XOR | 0b0001], // XOR R0, R1
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b1010_1010);
            state.SetRegister(1, 0b1010_1010);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(1), Is.EqualTo(0x00), "R1 should be 0 when XORing same values.");
            Assert.That(state.Z, Is.True, "Zero flag should be set when result is zero.");
        }
    }

    [TestFixture]
    public class XRI_tests
    {
        [Test]
        public void XRI_R0_PerformsBitwiseXorWithImmediate()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.XRI | 0b0000, 0b1010_1010], // XRI R0, 0xAA
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b1111_0000);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0b0101_1010), "R0 should contain R0 XOR immediate.");
            Assert.That(state.Z, Is.False, "Zero flag should be clear when result is not zero.");
        }

        [Test]
        public void XRI_R0_SetsZeroFlagWhenResultIsZero()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.XRI | 0b0000, 0b1010_1010], // XRI R0, 0xAA
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b1010_1010);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0x00), "R0 should be 0.");
            Assert.That(state.Z, Is.True, "Zero flag should be set when result is zero.");
        }
    }

    [TestFixture]
    public class XRA_tests
    {
        [Test]
        public void XRA_R0_PerformsBitwiseXorWithMemory()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.XRA | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // XRA R0, [0x10]
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 0b1111_0000);
            memory.WriteByte(0x10, 0b1010_1010);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0b0101_1010), "R0 should contain R0 XOR memory value.");
            Assert.That(state.Z, Is.False, "Zero flag should be clear when result is not zero.");
        }

        [Test]
        public void XRA_R0_SetsZeroFlagWhenResultIsZero()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.XRA | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // XRA R0, [0x10]
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 0b1010_1010);
            memory.WriteByte(0x10, 0b1010_1010);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0x00), "R0 should be 0.");
            Assert.That(state.Z, Is.True, "Zero flag should be set when result is zero.");
        }
    }

    [TestFixture]
    public class BTI_tests
    {
        [Test]
        public void BTI_R0_SetsZeroFlagWhenBitsMatch()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.BTI | 0b0000, 0b0000_1000], // BTI R0, 0x08
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b1111_1111);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.Z, Is.True, "Zero flag should be set when bits match (result is not zero).");
        }

        [Test]
        public void BTI_R0_ClearsZeroFlagWhenBitsDoNotMatch()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.BTI | 0b0000, 0b0000_1000], // BTI R0, 0x08
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b1111_0000);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.Z, Is.False, "Zero flag should be clear when bits do not match (result is zero).");
        }

        [Test]
        public void BTI_R0_ClearsZeroFlagWhenBothZero()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.BTI | 0b0000, 0b0000_0000], // BTI R0, 0x00
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b0000_0000);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.Z, Is.False, "Zero flag should be clear when bits match and are zero.");
        }

        [Test]
        public void BTI_R0_DoesNotModifyRegister()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.BTI | 0b0000, 0b1010_1010], // BTI R0, 0xAA
                out var state,
                out _,
                out _);
            state.SetRegister(0, 0b1111_0000);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0b1111_0000), "R0 should not be modified by BTI.");
        }
    }

    [TestFixture]
    public class BTA_tests
    {
        [Test]
        public void BTA_R0_SetsZeroFlagWhenBitsMatch()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.BTA | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // BTA R0, [0x10]
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 0b1111_1111);
            memory.WriteByte(0x10, 0b0000_1000);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.Z, Is.True, "Zero flag should be set when bits match (result is not zero).");
        }

        [Test]
        public void BTA_R0_ClearsZeroFlagWhenBitsDoNotMatch()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.BTA | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // BTA R0, [0x10]
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 0b1111_0000);
            memory.WriteByte(0x10, 0b0000_1111);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.Z, Is.False, "Zero flag should be clear when bits do not match (result is zero).");
        }

        [Test]
        public void BTA_R0_ClearsZeroFlagWhenBothZero()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.BTA | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // BTA R0, [0x10]
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 0b0000_0000);
            memory.WriteByte(0x10, 0b0000_0000);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.Z, Is.False, "Zero flag should be clear when bits match and are zero.");
        }

        [Test]
        public void BTA_R0_DoesNotModifyRegister()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.BTA | 0b0000, .. OpcodeTestHelpers.GetAddress(0x10)], // BTA R0, [0x10]
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 0b1111_0000);
            memory.WriteByte(0x10, 0b1010_1010);

            // Act
            cpu.Step();

            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0b1111_0000), "R0 should not be modified by BTA.");
        }
    }

    [TestFixture]
    public class LDX_tests
    {
        [Test]
        public void LDX_R0_R1WithoutOffset_LoadsValueFromMemory()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.LDX | 0b0000, 0b0000_0001], // LDX R0, [R1]
                out var state,
                out _,
                out var memory);
            state.SetRegister(1, 0x10); // R1 points to address 0x10
            memory.WriteByte(0x10, 0x42);
            // Act
            cpu.Step();
            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0x42), "R0 should load value 0x42 from memory address 0x10.");
        }

        [Test]
        public void LDX_R0_R1WithOffset_LoadsValueFromMemory()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.LDX | 0b0000, 0b0000_0101], // LDX R0, [R1+1]
                out var state,
                out _,
                out var memory);
            state.SetRegister(1, 0x10); // R1 points to address 0x10 + 0x01 offset
            memory.WriteByte(0x11, 0x42);
            // Act
            cpu.Step();
            // Assert
            Assert.That(state.GetRegister(0), Is.EqualTo(0x42), "R0 should load value 0x42 from memory address 0x11.");
        }
    }

    [TestFixture]
    public class STX_tests
    {
        [Test]
        public void STX_R0_R1WithoutOffset_StoresValueToMemory()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.STX | 0b0000, 0b0000_0001], // STX R0, [R1]
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 0x42); // R0 contains value 0x42
            state.SetRegister(1, 0x10); // R1 points to address 0x10
            // Act
            cpu.Step();
            // Assert
            Assert.That(memory.ReadByte(0x10), Is.EqualTo(0x42), "Memory address 0x10 should contain value 0x42 after STX.");
        }

        [Test]
        public void STX_R0_R1WithOffset_StoresValueToMemory()
        {
            // Arrange
            var cpu = OpcodeTestHelpers.CreateCPUWithProgram(
                program: [(byte)OpcodeBaseCode.STX | 0b0000, 0b0000_0101], // STX R0, [R1+1]
                out var state,
                out _,
                out var memory);
            state.SetRegister(0, 0x42); // R0 contains value 0x42
            state.SetRegister(1, 0x10); // R1 points to address 0x10 + 0x01 offset
            // Act
            cpu.Step();
            // Assert
            Assert.That(memory.ReadByte(0x11), Is.EqualTo(0x42), "Memory address 0x11 should contain value 0x42 after STX.");
        }
    }
}
