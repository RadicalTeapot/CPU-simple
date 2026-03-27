namespace AudioChip.Processors
{
    internal class Attenuator
    {
        public float Apply(float input, float level)
        {
            return input * level;
        }
    }
}
