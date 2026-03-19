namespace Controller.Exceptions
{
    internal class ControllerException : Exception
    {
        public ControllerException(string message) : base(message) { }
        public ControllerException(string message, Exception innerException) : base(message, innerException) { }

        internal class TooManyButtonsException : ControllerException
        {
            public TooManyButtonsException(string message) : base(message) { }
            public TooManyButtonsException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}
