using NUnit.Framework;
using CPU.components;

namespace CPU.Tests
{
    [TestFixture]
    internal class Stack_tests
    {
#if x16
        [Test]
        public void PushAddress_Works()
        {
            // Arrange
            var memory = new Memory(256);
            var stack = new Stack(memory, 0xFF);
            var executionContext = new ExecutionContext();

            // Act
            stack.PushAddress(0x1234, executionContext);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(stack.SP, Is.EqualTo(0xFD), "SP should be decremented by 2");
                Assert.That(memory.ReadByte(0xFF), Is.EqualTo(0x12), "High byte should be at top of stack");
                Assert.That(memory.ReadByte(0xFE), Is.EqualTo(0x34), "Low byte should be next on stack");
                Assert.That(executionContext.StackChanges.Count, Is.EqualTo(2), "Two stack changes should be recorded");
                Assert.That(executionContext.StackChanges[0].Key, Is.EqualTo(0xFF), "First stack change address should be correct");
                Assert.That(executionContext.StackChanges[0].Value, Is.EqualTo(0x12), "First stack change value should be correct");
                Assert.That(executionContext.StackChanges[1].Key, Is.EqualTo(0xFE), "Second stack change address should be correct");
                Assert.That(executionContext.StackChanges[1].Value, Is.EqualTo(0x34), "Second stack change value should be correct");
            });
        }

        [Test]
        public void PopAddress_Works() {
            // Arrange
            var memory = new Memory(256);
            var stack = new Stack(memory, 0xFF);
            ushort testAddress = 0x1234;
            stack.PushAddress(testAddress, new ExecutionContext()); // Push without recording to avoid interference

            // Act
            var poppedAddress = stack.PopAddress();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(poppedAddress, Is.EqualTo(testAddress), "Popped address should match pushed address");
                Assert.That(stack.SP, Is.EqualTo(0xFF), "SP should be back to initial value");
            });
        }

        [Test]
        public void PeekAddress_Works()
        {
            // Arrange
            var memory = new Memory(256);
            var stack = new Stack(memory, 0xFF);
            ushort testAddress = 0x1234;
            stack.PushAddress(testAddress, new ExecutionContext());

            // Act
            var peekedAddress = stack.PeekAddress();

            // Assert
            Assert.That(peekedAddress, Is.EqualTo(testAddress), "Peeked address should match pushed address");
            Assert.That(stack.SP, Is.EqualTo(0xFD), "SP should be not be modified");
        }
#else
        [Test]
        public void PushAddress_Works()
        {
            // Arrange
            var memory = new Memory(256);
            var stack = new Stack(memory, 0xFF);
            var executionContext = new ExecutionContext();

            // Act
            stack.PushAddress(0x12, executionContext);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(stack.SP, Is.EqualTo(0xFE), "SP should be decremented by 1");
                Assert.That(memory.ReadByte(0xFF), Is.EqualTo(0x12), "Pushed byte should be at top of stack");
                Assert.That(executionContext.StackChanges.Count, Is.EqualTo(1), "One stack change should be recorded");
                Assert.That(executionContext.StackChanges[0].Key, Is.EqualTo(0xFF), "Stack change address should be correct");
                Assert.That(executionContext.StackChanges[0].Value, Is.EqualTo(0x12), "Stack change value should be correct");
            });
        }

        [Test]
        public void PopAddress_Works()
        {
            // Arrange
            var memory = new Memory(256);
            var stack = new Stack(memory, 0xFF);
            byte testAddress = 0x12;
            stack.PushAddress(testAddress, new ExecutionContext()); // Push without recording to avoid interference

            // Act
            var poppedAddress = stack.PopAddress();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(poppedAddress, Is.EqualTo(testAddress), "Popped address should match pushed address");
                Assert.That(stack.SP, Is.EqualTo(0xFF), "SP should be back to initial value");
            });
        }

        [Test]
        public void PeekAddress_Works()
        {
            // Arrange
            var memory = new Memory(256);
            var stack = new Stack(memory, 0xFF);
            byte testAddress = 0x12;
            stack.PushAddress(testAddress, new ExecutionContext());

            // Act
            var peekedAddress = stack.PeekAddress();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(peekedAddress, Is.EqualTo(testAddress), "Peeked address should match pushed address");
                Assert.That(stack.SP, Is.EqualTo(0xFE), "SP should be not be modified");
            });
        }
#endif

        [Test]
        public void PushByte_Works()
        {
            // Arrange
            var memory = new Memory(256);
            var stack = new Stack(memory, 0xFF);
            var executionContext = new ExecutionContext();

            // Act
            stack.PushByte(0x12, executionContext);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(stack.SP, Is.EqualTo(0xFE), "SP should be decremented by 1");
                Assert.That(memory.ReadByte(0xFF), Is.EqualTo(0x12), "Pushed byte should be at top of stack");
                Assert.That(executionContext.StackChanges.Count, Is.EqualTo(1), "One stack change should be recorded");
                Assert.That(executionContext.StackChanges[0].Key, Is.EqualTo(0xFF), "Stack change address should be correct");
                Assert.That(executionContext.StackChanges[0].Value, Is.EqualTo(0x12), "Stack change value should be correct");
            });
        }

        [Test]
        public void PopByte_Works()
        {
            // Arrange
            var memory = new Memory(256);
            var stack = new Stack(memory, 0xFF);
            byte testAddress = 0x12;
            stack.PushByte(testAddress, new ExecutionContext());

            // Act
            var poppedAddress = stack.PopByte();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(poppedAddress, Is.EqualTo(testAddress), "Popped address should match pushed address");
                Assert.That(stack.SP, Is.EqualTo(0xFF), "SP should be back to initial value");
            });
        }

        [Test]
        public void PeekByte_Works()
        {
            // Arrange
            var memory = new Memory(256);
            var stack = new Stack(memory, 0xFF);
            byte testAddress = 0x12;
            stack.PushByte(testAddress, new ExecutionContext());

            // Act
            var peekedAddress = stack.PeekByte();

            // Assert
            Assert.That(peekedAddress, Is.EqualTo(testAddress), "Peeked address should match pushed address");
            Assert.That(stack.SP, Is.EqualTo(0xFE), "SP should be not be modified");
        }
    }
}
