using Controller.Configuration;

namespace Controller.Storage
{
    internal struct CurrentState
    {
        public bool Up;
        public bool Down;
        public bool Left;
        public bool Right;
        public bool[] Buttons;
    }

    public class ButtonsState(ControllerConfiguration configuration)
    {
        public bool Left {
            set { lock (_lock) _directions[Button.Left] = value; }
        }
        public bool Right {
            set { lock (_lock) _directions[Button.Right] = value; }
        }
        public bool Up {
            set { lock (_lock) _directions[Button.Up] = value; }
        }
        public bool Down { 
            set { lock (_lock) _directions[Button.Down] = value; }
        }

        public int ButtonCount => configuration.ButtonCount;

        public void SetButton(int index, bool pressed)
        {
            if (index < 0 || index >= _buttons.Length)
                throw new ArgumentOutOfRangeException(nameof(index), $"Button index must be between 0 and {_buttons.Length - 1}.");

            lock (_lock)
            {
                _buttons[index] = pressed;
            }
        }

        internal CurrentState ReadAndReset()
        {
            CurrentState state;
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

                state.Buttons = new bool[_buttons.Length];
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
        private readonly Dictionary<Button, bool> _directions = new() {
            { Button.Up, false },
            { Button.Down, false },
            { Button.Left, false },
            { Button.Right, false },
        };
        private readonly bool[] _buttons = new bool[configuration.ButtonCount];
    }
}
