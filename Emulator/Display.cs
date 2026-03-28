using AudioChip.Configuration;
using Controller.Storage;
using Raylib_cs;

namespace Emulator
{
    internal class Display : IDisposable
    {
        public static bool ShouldClose => Raylib.WindowShouldClose();

        public Display(int screenWidth, int screenHeight, int scale, AudioConfiguration? audioConfig = null)
        {
            _scale = scale;
            _colors = new Color[screenWidth * screenHeight];

            Raylib.InitWindow(screenWidth * scale, screenHeight * scale, "cpu-simple");
            var image = Raylib.GenImageColor(screenWidth, screenHeight, Color.Black);
            _texture = Raylib.LoadTextureFromImage(image);
            Raylib.UnloadImage(image);
            Raylib.SetTargetFPS(60);

            if (audioConfig != null)
            {
                Raylib.InitAudioDevice();
                Raylib.SetAudioStreamBufferSizeDefault(audioConfig.BufferSize);
                var stream = Raylib.LoadAudioStream((uint)audioConfig.SampleRate, 32, 1);
                Raylib.PlayAudioStream(stream);
                _audioStream = stream;
            }
        }

        public void UpdateFrame(IReadOnlyList<byte> rgbPixels)
        {
            for (var i = 0; i < _colors.Length; i++)
            {
                var offset = i * 3;
                _colors[i] = new Color(rgbPixels[offset], rgbPixels[offset + 1], rgbPixels[offset + 2], (byte)255);
            }
            unsafe
            {
                fixed (Color* ptr = _colors)
                {
                    Raylib.UpdateTexture(_texture, ptr);
                }
            }
        }

        public void PollInput(ButtonsState buttonState)
        {
            if (Raylib.IsKeyDown(KeyboardKey.Up))    buttonState.SetUp();
            if (Raylib.IsKeyDown(KeyboardKey.Down))  buttonState.SetDown();
            if (Raylib.IsKeyDown(KeyboardKey.Left))  buttonState.SetLeft();
            if (Raylib.IsKeyDown(KeyboardKey.Right)) buttonState.SetRight();

            ReadOnlySpan<KeyboardKey> buttonKeys = [KeyboardKey.Z, KeyboardKey.X, KeyboardKey.A, KeyboardKey.S];
            for (int i = 0; i < Math.Min(buttonState.ButtonCount, buttonKeys.Length); i++)
            {
                if (Raylib.IsKeyDown(buttonKeys[i]))
                    buttonState.SetButton(i);
            }
        }

        public bool IsAudioReady() =>
            _audioStream.HasValue && Raylib.IsAudioStreamProcessed(_audioStream.Value);

        public unsafe void SubmitAudio(Span<float> samples)
        {
            if (!_audioStream.HasValue) return;
            fixed (float* ptr = samples)
                Raylib.UpdateAudioStream(_audioStream.Value, ptr, samples.Length);
        }

        public void Render()
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            Raylib.DrawTextureEx(_texture, new System.Numerics.Vector2(0, 0), 0f, _scale, Color.White);
            Raylib.EndDrawing();
        }

        public void Dispose()
        {
            Raylib.UnloadTexture(_texture);
            if (_audioStream.HasValue)
            {
                Raylib.StopAudioStream(_audioStream.Value);
                Raylib.UnloadAudioStream(_audioStream.Value);
                Raylib.CloseAudioDevice();
            }
            Raylib.CloseWindow();
        }

        private readonly int _scale;
        private readonly Color[] _colors;
        private readonly Texture2D _texture;
        private readonly AudioStream? _audioStream;
    }
}
