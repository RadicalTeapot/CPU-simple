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
            Assert.That(result.Length, Is.EqualTo(0));
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

        [Test]
        public void Emitter_SpanTracking_ReturnsCorrectAddresses()
        {
            var nodes = new List<IEmitNode>
            {
                new TestEmitNode([0x01, 0x02]),
                new TestEmitNode([0x03])
            };
            var emitter = new Emitter();
            emitter.Emit(nodes);
            var spans = emitter.GetSpanAddresses();
            Assert.Multiple(() =>
            {
                Assert.That(spans, Has.Count.EqualTo(2));
                Assert.That(spans[0].StartAddress, Is.EqualTo(0));
                Assert.That(spans[0].EndAddress, Is.EqualTo(1));
                Assert.That(spans[1].StartAddress, Is.EqualTo(2));
                Assert.That(spans[1].EndAddress, Is.EqualTo(2));
            });
        }

        [Test]
        public void Emitter_EmitCountMismatch_ThrowsEmitterException()
        {
            var nodes = new List<IEmitNode>
            {
                new MismatchedEmitNode()
            };
            var emitter = new Emitter();
            Assert.Throws<EmitterException>(() => emitter.Emit(nodes));
        }

        [Test]
        public void Emitter_ExactMaxAddress_Succeeds()
        {
            var nodes = new List<IEmitNode>
            {
                new TestFillEmitNode(10, 0xAA)
            };
            var emitter = new Emitter(10);
            var result = emitter.Emit(nodes);
            Assert.That(result, Has.Length.EqualTo(10));
            Assert.That(result, Is.All.EqualTo(0xAA));
        }

        [Test]
        public void Emitter_CustomMaxAddress_ClampsCorrectly()
        {
            var nodes = new List<IEmitNode>
            {
                new TestFillEmitNode(6, 0xBB)
            };
            var emitter = new Emitter(5);
            Assert.Throws<EmitterException>(() => emitter.Emit(nodes));
        }

        [Test]
        public void Emitter_IntegrationWithAnalyser_ProducesValidBytes()
        {
            var bytes = AnalyserTestsHelper.AnalyseAndEmit("NOP\nHLT");
            Assert.Multiple(() =>
            {
                Assert.That(bytes[0], Is.EqualTo(0x00)); // NOP
                Assert.That(bytes[1], Is.EqualTo(0x01)); // HLT
            });
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

    internal class MismatchedEmitNode : IEmitNode
    {
        public byte[] Emit() => [0x01, 0x02]; // returns 2 bytes
        public int Count => 1; // but claims count is 1
        public NodeSpan Span => new();
    }
}
