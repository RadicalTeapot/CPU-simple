using Controller.Configuration;
using Controller.Exceptions;
using Controller.Storage;

namespace Controller.Tests
{
    [TestFixture]
    public class ButtonsState_tests
    {
        // Constructor guards

        [Test]
        public void Constructor_NullConfiguration_ThrowsArgumentNullException()
        {
            Assert.That(() => new ButtonsState(null!), Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void Constructor_NegativeButtonCount_ThrowsNegativeButtonCountException()
        {
            Assert.That(() => new ButtonsState(new ControllerConfiguration(-1)),
                Throws.InstanceOf<ControllerException.NegativeButtonCountException>());
        }

        [Test]
        public void Constructor_TooManyButtonCount_ThrowsTooManyButtonsException()
        {
            Assert.That(() => new ButtonsState(new ControllerConfiguration(5)),
                Throws.InstanceOf<ControllerException.TooManyButtonsException>());
        }

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
            Assert.That(() => state.SetButton(2), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void SetButton_NegativeIndex_Throws()
        {
            var state = new ButtonsState(new ControllerConfiguration(2));
            Assert.That(() => state.SetButton(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void ReadAndReset_ReturnsCorrectButtonValue()
        {
            var state = new ButtonsState(new ControllerConfiguration(2));
            state.SetButton(0);
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
            state.SetButton(0);
            state.ReadAndReset();
            var snapshot = state.ReadAndReset();
            Assert.That(snapshot.Buttons[0], Is.False);
        }

        [Test]
        public void ReadAndReset_ReturnsDirectionState()
        {
            var state = new ButtonsState(new ControllerConfiguration(0));
            state.SetUp();
            var snapshot = state.ReadAndReset();
            Assert.That(snapshot.Up, Is.True);
        }

        [Test]
        public void ReadAndReset_ReturnsDownState()
        {
            var state = new ButtonsState(new ControllerConfiguration(0));
            state.SetDown();
            var snapshot = state.ReadAndReset();
            Assert.That(snapshot.Down, Is.True);
        }

        [Test]
        public void ReadAndReset_ReturnsLeftState()
        {
            var state = new ButtonsState(new ControllerConfiguration(0));
            state.SetLeft();
            var snapshot = state.ReadAndReset();
            Assert.That(snapshot.Left, Is.True);
        }

        [Test]
        public void ReadAndReset_ReturnsRightState()
        {
            var state = new ButtonsState(new ControllerConfiguration(0));
            state.SetRight();
            var snapshot = state.ReadAndReset();
            Assert.That(snapshot.Right, Is.True);
        }

        [Test]
        public void ReadAndReset_UnpressedDirection_ReturnsFalse()
        {
            var state = new ButtonsState(new ControllerConfiguration(0));
            var snapshot = state.ReadAndReset();
            Assert.That(snapshot.Up, Is.False);
        }

        [Test]
        public void ReadAndReset_ResetsAllDirectionsAfterRead()
        {
            var state = new ButtonsState(new ControllerConfiguration(0));
            state.SetUp();
            state.SetDown();
            state.SetLeft();
            state.SetRight();
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
