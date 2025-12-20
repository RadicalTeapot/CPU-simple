namespace CPU.components
{
    internal class Register<T>(T value)
    {
        public T Value { get; set; } = value;
    }
}
