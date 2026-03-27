using CPU.components;

namespace AudioChip.Storage
{
    internal class AudioRegisters(Voice voice) : IMmioDevice
    {
        public byte ReadRegister(byte offset) => 0; // Audio has no read registers

        public void WriteRegister(byte offset, byte value)
        {
            switch (offset)
            {
                case NoteOffset:
                    WriteNote(value, voice);
                    break;
                case EnvOffset:
                    WriteEnvelopeAttackRelease(value, voice);
                    break;
                case FilterOffset:
                    WriteFilterParameters(value, voice);
                    break;
                case ControlOffset:
                    WriteEnvelopeSustain(value, voice);
                    WritePulseWidth(value, voice);
                    WriteFilterTypeAndSlope(value, voice);
                    break;
            }
        }

        private static void WriteNote(byte value, Voice voice)
        {
            var (frequency, gate) = Voice.GetNoteAndGate(value);
            voice.Frequency = frequency;
            voice.Gate = gate;
        }

        private static void WriteEnvelopeAttackRelease(byte value, Voice voice)
        {
            var (attack, release) = Envelope.GetAttackRelease(value);
            voice.Envelope.Attack = attack;
            voice.Envelope.Release = release;
        }

        private static void WriteEnvelopeSustain(byte value, Voice voice)
        {
            byte sustain = Envelope.GetSustain(value);
            voice.Envelope.Sustain = sustain;
        }

        private static void WriteFilterParameters(byte value, Voice voice)
        {
            var (cutoff, useEnv) = Filter.GetFilterParameters(value);
            voice.Filter.Cutoff = cutoff;
        }

        private static void WriteFilterTypeAndSlope(byte value, Voice voice)
        {
            var (type, slope) = Filter.GetFilterTypeAndSlope(value);
            voice.Filter.Type = type;
            voice.Filter.Slope = slope;
        }

        private static void WritePulseWidth(byte value, Voice voice)
        {
            voice.PulseWidth = Voice.GetPulseWidth(value);
        }

        private const byte NoteOffset = 0;
        private const byte EnvOffset = 1;
        private const byte FilterOffset = 2;
        private const byte ControlOffset = 3;
    }
}
