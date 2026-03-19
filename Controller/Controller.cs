using Controller.Storage;
using Controller.Configuration;
using CPU.components;

namespace Controller
{
    public class Controller
    {
        public IMmioDevice Registers => _registers;
        public ButtonsState ButtonState { get; }

        public Controller(ControllerConfiguration configuration)
        {
            ButtonState = new ButtonsState(configuration);
            _registers = new ControllerRegisters(ButtonState);
        }

        private readonly ControllerRegisters _registers;
    }
}
