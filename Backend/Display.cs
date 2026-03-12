using Raylib_cs;

namespace Backend
{
    internal class Display : IDisposable
    {
        public static bool ShouldClose => Raylib.WindowShouldClose();

        public Display(int screenWidth, int screenHeight, int scale)
        {
            _scale = scale;
            _colors = new Color[screenWidth * screenHeight];

            Raylib.InitWindow(screenWidth * scale, screenHeight * scale, "cpu-simple");
            var image = Raylib.GenImageColor(screenWidth, screenHeight, Color.Black);
            _texture = Raylib.LoadTextureFromImage(image);
            Raylib.UnloadImage(image);
            Raylib.SetTargetFPS(60);
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
            Raylib.CloseWindow();
        }

        private readonly int _scale;
        private readonly Color[] _colors;
        private readonly Texture2D _texture;
    }
}
