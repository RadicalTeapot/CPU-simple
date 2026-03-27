using AudioChip.Processors;

namespace AudioChip.Tests
{
    [TestFixture]
    internal class Attenuator_tests
    {
        [TestCase(1.0f,  1.0f,  1.0f)]
        [TestCase(1.0f,  0.5f,  0.5f)]
        [TestCase(0.5f,  0.5f,  0.25f)]
        [TestCase(1.0f,  0.0f,  0.0f)]
        [TestCase(-1.0f, 0.5f, -0.5f)]
        public void Apply_ReturnsInputScaledByLevel(float input, float level, float expected)
        {
            var attenuator = new Attenuator();

            Assert.That(attenuator.Apply(input, level), Is.EqualTo(expected).Within(1e-6f));
        }
    }
}
