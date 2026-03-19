using Controller.Configuration;
using Controller.Storage;

namespace Controller.Tests
{
    [TestFixture]
    public class ButtonsState_tests
    {
        [Test]
        public void ButtonCount_ReturnsConfiguredCount()
        {
            var state = new ButtonsState(new ControllerConfiguration(3));
            Assert.That(state.ButtonCount, Is.EqualTo(3));
        }

        [Test]
        public void SetButton_OutOfRangeIndex_Throws()
        {
            var state = new ButtonsState(new ControllerConfiguration(2));
            Assert.That(() => state.SetButton(2, true), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void SetButton_NegativeIndex_Throws()
        {
            var state = new ButtonsState(new ControllerConfiguration(2));
            Assert.That(() => state.SetButton(-1, true), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void ReadAndReset_ReturnsCorrectButtonValue()
        {
            var state = new ButtonsState(new ControllerConfiguration(2));
            state.SetButton(0, true);
            var snapshot = state.ReadAndReset();
            Assert.That(snapshot.Buttons[0], Is.True);
        }

        [Test]
        public void ReadAndReset_UnpressedButton_ReturnsFalse()
        {
            var state = new ButtonsState(new ControllerConfiguration(2));
            var snapshot = state.ReadAndReset();
            Assert.That(snapshot.Buttons[1], Is.False);
        }

        [Test]
        public void ReadAndReset_ResetsButtonsAfterRead()
        {
            var state = new ButtonsState(new ControllerConfiguration(1));
            state.SetButton(0, true);
            state.ReadAndReset();
            var snapshot = state.ReadAndReset();
            Assert.That(snapshot.Buttons[0], Is.False);
        }

        [TestCase(true, true)]
        [TestCase(false, false)]
        public void ReadAndReset_ReturnsDirectionState(bool pressed, bool expected)
        {
            var state = new ButtonsState(new ControllerConfiguration(0));
            state.Up = pressed;
            var snapshot = state.ReadAndReset();
            Assert.That(snapshot.Up, Is.EqualTo(expected));
        }

        [Test]
        public void ReadAndReset_ResetsAllDirectionsAfterRead()
        {
            var state = new ButtonsState(new ControllerConfiguration(0));
            state.Up = true;
            state.Down = true;
            state.Left = true;
            state.Right = true;
            state.ReadAndReset();
            var snapshot = state.ReadAndReset();
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Up, Is.False);
                Assert.That(snapshot.Down, Is.False);
                Assert.That(snapshot.Left, Is.False);
                Assert.That(snapshot.Right, Is.False);
            });
        }
    }
}
