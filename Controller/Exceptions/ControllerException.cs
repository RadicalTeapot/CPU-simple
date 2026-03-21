namespace Controller.Exceptions
{
    public class ControllerException : Exception
    {
        public ControllerException(string message) : base(message) { }
        public ControllerException(string message, Exception innerException) : base(message, innerException) { }

        public class NegativeButtonCountException : ControllerException
        {
            public NegativeButtonCountException(string message) : base(message) { }
            public NegativeButtonCountException(string message, Exception innerException) : base(message, innerException) { }
        }

        public class TooManyButtonsException : ControllerException
        {
            public TooManyButtonsException(string message) : base(message) { }
            public TooManyButtonsException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}
