using Raylib_cs;
using System.Runtime.CompilerServices;
using static Raylib_cs.Raylib;

namespace AudioChip.Tests
{
    public class POC_tests
    {
        [SetUp]
        public void Setup()
        {
            InitWindow(640, 360, "Test raylib audio");
            InitAudioDevice();
            SetTargetFPS(FrameRate);
            SetAudioStreamBufferSizeDefault(BufferSize); // Set default buffer size to 1024 samples (must be power of 2 and >= SampleRate / FrameRate)
            _audioStream = LoadAudioStream(SampleRate, 32, 1); // 44100 Hz, float32, mono
            PlayAudioStream(_audioStream);
        }

        [TearDown]
        public void TearDown()
        {
            StopAudioStream(_audioStream);
            UnloadAudioStream(_audioStream);
            CloseAudioDevice();
            CloseWindow();
        }

        [Test]
        public unsafe void Raylib_AudioStream_Test()
        {
            const int MidiNote = 60;        // C4 ≈ 261 Hz
            const float DutyCycle = 0.25f;     // 25 %
            var noteFreq = 440f * MathF.Pow(2f, (MidiNote - 69) / 12f);
            var phase = 0f;

            var audioBuffer = new float[BufferSize];
            var bufferIndex = 0;
            while (!WindowShouldClose())
            {
                if (IsAudioStreamProcessed(_audioStream))
                {
                    for (int i = 0; i < BufferSize; i++)
                    {
                        float osc = phase < DutyCycle ? 1f : -1f;
                        phase += noteFreq / SampleRate;
                        if (phase >= 1f) phase -= 1f;
                        audioBuffer[bufferIndex++] = Math.Clamp(osc, -1f, 1f) * 0.2f; // Reduce volume to avoid clipping
                        if (bufferIndex >= BufferSize) bufferIndex = 0;
                    }

                    fixed (float* bufferPtr = audioBuffer)
                        UpdateAudioStream(_audioStream, bufferPtr, BufferSize);
                }

                BeginDrawing();
                ClearBackground(Color.Black);
                DrawText("Playing square wave - press ESC to exit", 20, 20, 20, Color.RayWhite);
                EndDrawing();
            }
        }

        [Test]
        public unsafe void Raylib_AudioStream_RingBuffer_Test()
        {
            const int MidiNote = 60;        // C4 ≈ 261 Hz
            const float DutyCycle = 0.25f;     // 25 %
            var noteFreq = 440f * MathF.Pow(2f, (MidiNote - 69) / 12f);
            var phase = 0f;

            // 4× BufferSize gives enough slack so the write pointer never catches up to the
            // read pointer even if a frame runs long or IsAudioStreamProcessed fires late.
            var ringBuffer = new float[BufferSize*4];
            // Linearised staging area: ring buffer data is copied here (handling wrap-around)
            // before being handed to Raylib, which requires a contiguous pointer.
            var submitBuffer = new float[BufferSize];
            // Carries the sub-sample remainder across frames so that, over time, exactly
            // SampleRate samples are generated per second despite integer truncation each frame.
            var fractionalIndex = 0.0;
            // Ideal (fractional) number of samples to generate per frame: SampleRate / FrameRate.
            var samplesPerFrame = (double)SampleRate / FrameRate;
            // Write head: next slot in ringBuffer where the oscillator writes a new sample.
            var writeIndex = 0;
            // Read head: next slot in ringBuffer that will be submitted to the audio device.
            var readIndex = 0;

            // Pre-fill exactly one device buffer worth of samples before the loop.
            // Without this, IsAudioStreamProcessed fires on the very first frame (the stream
            // starts empty) before we have written BufferSize samples, causing the guard below
            // to skip the submission and leave the device silent for one frame — an audible click.
            // After pre-fill, available starts at BufferSize so the first submission can happen
            // immediately, and steady-state available never drops below BufferSize when the
            // device asks for data.
            for (int i = 0; i < BufferSize; i++)
            {
                float osc = phase < DutyCycle ? 1f : -1f;
                phase += noteFreq / SampleRate;
                if (phase >= 1f) phase -= 1f;
                ringBuffer[writeIndex++] = Math.Clamp(osc, -1f, 1f) * 0.2f;
            }

            while (!WindowShouldClose())
            {
                // How many samples to generate this frame, incorporating the carry from last frame.
                // Using fractionalIndex ensures the long-run generation rate stays exactly
                // SampleRate samples/second despite the per-frame integer truncation.
                var intended = (int)(samplesPerFrame + fractionalIndex);
                // Update the carry: record how far the fractional cursor advanced beyond 'intended'
                // so the next frame compensates. This is independent of how many samples were
                // actually written (see 'samplesToWrite' below) to avoid cascading debt.
                fractionalIndex += samplesPerFrame - intended;
                // Number of writable slots before the write head would lap the read head.
                // The -1 keeps one sentinel slot free so that writeIndex == readIndex always
                // means "empty", never "full", making the 'available' formula unambiguous.
                var free = (readIndex - writeIndex - 1 + ringBuffer.Length) % ringBuffer.Length;
                // Cap to free space as a safety net against overruns if a frame runs very long.
                // In practice this never triggers with a 4× ring buffer, but without it an
                // overrun would silently corrupt unread samples and produce glitches.
                var samplesToWrite = Math.Min(intended, free);
                for (int i = 0; i < samplesToWrite; i++)
                {
                    float osc = phase < DutyCycle ? 1f : -1f;
                    phase += noteFreq / SampleRate;
                    if (phase >= 1f) phase -= 1f;
                    ringBuffer[writeIndex] = Math.Clamp(osc, -1f, 1f) * 0.2f; // Reduce volume to avoid clipping
                    writeIndex = (writeIndex + 1) % ringBuffer.Length;
                }

                // How many contiguous (logically) samples are ready to read: distance from
                // read head to write head around the ring. Always in [0, ringBuffer.Length).
                var available = (writeIndex - readIndex + ringBuffer.Length) % ringBuffer.Length;
                // Submit only when the device has consumed its previous buffer AND we have a full
                // BufferSize ready. The second condition prevents underruns: if IsAudioStreamProcessed
                // fires on a frame where available < BufferSize (can happen due to the device's
                // 23 ms period not being an exact multiple of the 16 ms frame period), skipping
                // ensures the device waits one more frame rather than receiving partial silence.
                // The pre-fill above guarantees available >= BufferSize on the very first call,
                // and the steady-state write rate (SampleRate samples/s) equals the consume rate,
                // so available stays well above BufferSize after that.
                if (IsAudioStreamProcessed(_audioStream) && available >= BufferSize)
                {
                    // Copy BufferSize samples from the read head into a contiguous staging buffer,
                    // using modulo to transparently handle wrap-around in the ring.
                    for (int i = 0; i < BufferSize; i++)
                        submitBuffer[i] = ringBuffer[(readIndex + i) % ringBuffer.Length];
                    // Advance the read head, freeing the submitted slots for future writes.
                    readIndex = (readIndex + BufferSize) % ringBuffer.Length;

                    fixed (float* bufferPtr = submitBuffer)
                        UpdateAudioStream(_audioStream, bufferPtr, BufferSize);
                }

                BeginDrawing();
                ClearBackground(Color.Black);
                DrawText("Playing square wave - press ESC to exit", 20, 20, 20, Color.RayWhite);
                EndDrawing();
            }
        }


        private const int SampleRate = 44100;
        private const int FrameRate = 60;
        private const int BufferSize = 1024; // Must be power of 2 and >= SampleRate / FrameRate
        private AudioStream _audioStream;
    }
}
