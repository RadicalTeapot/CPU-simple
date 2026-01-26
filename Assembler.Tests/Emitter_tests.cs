using Assembler.Analysis.EmitNode;
using Assembler.AST;

namespace Assembler.Tests
{
    [TestFixture]
    internal class Emitter_tests
    {
        [Test]
        public void Emitter_EmptyProgram_EmptyOutput()
        {
            var result = new Emitter().Emit(new List<IEmitNode>());
#if x16
            Assert.That(result.Length, Is.EqualTo(0));
#else
            Assert.That(result.Length, Is.EqualTo(0));
#endif
        }

        [Test]
        public void Emitter_SingleByteNode_WritesByteToMemory()
        {
            var nodes = new List<IEmitNode>
            {
                new TestEmitNode([0xAB])
            };
            var result = new Emitter().Emit(nodes);
            Assert.Multiple(() =>
            {
                Assert.That(result[0], Is.EqualTo(0xAB));
                Assert.That(result.Skip(1), Is.All.EqualTo(0x00));
            });
        }

        [Test]
        public void Emitter_MultipleNodes_WritesBytesInOrder()
        {
            var nodes = new List<IEmitNode>
            {
                new TestEmitNode([0x01, 0x02]),
                new TestEmitNode([0x03, 0x04, 0x05])
            };
            var result = new Emitter().Emit(nodes);
            Assert.Multiple(() =>
            {
                Assert.That(result[0], Is.EqualTo(0x01));
                Assert.That(result[1], Is.EqualTo(0x02));
                Assert.That(result[2], Is.EqualTo(0x03));
                Assert.That(result[3], Is.EqualTo(0x04));
                Assert.That(result[4], Is.EqualTo(0x05));
                Assert.That(result.Skip(5), Is.All.EqualTo(0x00));
            });
        }

        [Test]
        public void Emitter_WriteBeyondMaxAddress_ThrowsEmitterException()
        {
            var nodes = new List<IEmitNode>
            {
                new TestFillEmitNode(11, 0xFF)
            };
            var emitter = new Emitter(10);
            Assert.Throws<EmitterException>(() => emitter.Emit(nodes));
        }

        [Test]
        public void Emitter_WriteBeyondMemorySize_ThrowsEmitterException()
        {
#if x16
            var maxSize = 65536;
#else
            var maxSize = 256;
#endif
            var nodes = new List<IEmitNode>
            {
                new TestFillEmitNode(maxSize, 0xFF)
            };
            var emitter = new Emitter();
            Assert.Throws<EmitterException>(() => emitter.Emit(nodes));
        }
    }

    internal class TestEmitNode(byte[] bytes) : IEmitNode
    {
        public byte[] Emit()
        {
            return bytes;
        }
        public int Count => bytes.Length;
        public NodeSpan Span => new();
    }

    internal class TestFillEmitNode(int count, byte value) : IEmitNode
    {
        public byte[] Emit()
        {
            return [.. Enumerable.Repeat(value, count)];
        }
        public int Count => count;
        public NodeSpan Span => new();
    }
}
