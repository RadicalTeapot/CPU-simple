using Controller.Configuration;
using Controller.Exceptions;
using CPU.components;
using System.Runtime.CompilerServices;
using static Controller.Exceptions.ControllerException;

namespace Controller.Storage
{
    [InlineArray(ButtonsState.MaxButtons)]
    internal struct ButtonBuffer { private bool _element; }

    internal struct CurrentState
    {
        public bool Up;
        public bool Down;
        public bool Left;
        public bool Right;
        public ButtonBuffer Buttons;
    }

    public class ButtonsState
    {
        public const int MaxButtons = 4; // TODO Allow more buttons by using additional registers for 16bit builds

        public void SetLeft() { lock (_lock) { _directions[Button.Left] = true; } }
        public void SetRight() { lock (_lock) { _directions[Button.Right] = true; } }
        public void SetUp() { lock (_lock) { _directions[Button.Up] = true; } }
        public void SetDown() { lock (_lock) { _directions[Button.Down] = true; } }

        public int ButtonCount => _configuration.ButtonCount;

        public ButtonsState(ControllerConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (configuration.ButtonCount < 0)
                throw new NegativeButtonCountException("Button count cannot be negative.");

            if (configuration.ButtonCount > MaxButtons)
                throw new TooManyButtonsException($"Controller supports a maximum of {MaxButtons} buttons, but {configuration.ButtonCount} were configured.");


            _configuration = configuration;
            _buttons = new bool[_configuration.ButtonCount];
        }

        public void SetButton(int index)
        {
            if (index < 0 || index >= _buttons.Length)
                throw new ArgumentOutOfRangeException(nameof(index), $"Button index must be between 0 and {_buttons.Length - 1}.");

            lock (_lock)
            {
                _buttons[index] = true;
            }
        }

        internal CurrentState ReadAndReset()
        {
            CurrentState state = new();
            lock (_lock)
            {
                state.Up = _directions[Button.Up];
                _directions[Button.Up] = false;
                state.Down = _directions[Button.Down];
                _directions[Button.Down] = false;
                state.Left = _directions[Button.Left];
                _directions[Button.Left] = false;
                state.Right = _directions[Button.Right];
                _directions[Button.Right] = false;

                for (int i = 0; i < _buttons.Length; i++)
                {
                    state.Buttons[i] = _buttons[i];
                    _buttons[i] = false;
                }
            }

            return state;
        }

        private enum Button
        {
            Up = 0,
            Down,
            Left,
            Right,
        }

        private readonly Lock _lock = new();
        private readonly ControllerConfiguration _configuration;
        private readonly Dictionary<Button, bool> _directions = new() {
            { Button.Up, false },
            { Button.Down, false },
            { Button.Left, false },
            { Button.Right, false },
        };
        private readonly bool[] _buttons;
    }
}
