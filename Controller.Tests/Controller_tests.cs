using Controller.Configuration;

namespace Controller.Tests
{
    [TestFixture]
    public class Controller_tests
    {
        [Test]
        public void Registers_IsNotNull()
        {
            var controller = new Controller(new ControllerConfiguration(0));
            Assert.That(controller.Registers, Is.Not.Null);
        }

        [Test]
        public void ButtonState_IsNotNull()
        {
            var controller = new Controller(new ControllerConfiguration(0));
            Assert.That(controller.ButtonState, Is.Not.Null);
        }

        [Test]
        public void ReadRegister_AfterSettingDirection_ReturnsBitSet()
        {
            var controller = new Controller(new ControllerConfiguration(0));
            controller.ButtonState.SetUp();
            Assert.That(controller.Registers.ReadRegister(0) & 0x01, Is.EqualTo(0x01));
        }

        [Test]
        public void ReadRegister_Twice_SecondReadIsZero()
        {
            var controller = new Controller(new ControllerConfiguration(0));
            controller.ButtonState.SetLeft();
            controller.Registers.ReadRegister(0);
            Assert.That(controller.Registers.ReadRegister(0), Is.EqualTo(0));
        }
    }
}
