using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Easy3D.Core
{
    /// <summary>
    /// أنواع التكستشر
    /// </summary>
    public enum TextureType
    {
        Diffuse,        // اللون الأساسي
        Normal,         // الخريطة العادية
        Specular,       // اللمعان
        Emissive,       // الانبعاث
        Roughness,      // الخشونة
        Metalness,      // المعدنية
        AmbientOcclusion, // الظلال المحيطية
        Height,         // الارتفاع
        Opacity         // الشفافية
    }

    /// <summary>
    /// أنماط اللف (Wrapping)
    /// </summary>
    public enum TextureWrap
    {
        Repeat,
        ClampToEdge,
        MirroredRepeat
    }

    /// <summary>
    /// أنماط الترشيح (Filtering)
    /// </summary>
    public enum TextureFilter
    {
        Nearest,
        Linear,
        Bilinear,
        Trilinear
    }

    /// <summary>
    /// تكستشر (صورة) للاستخدام في المواد
    /// </summary>
    public class Texture : IDisposable
    {
        public string Name { get; set; }
        public string? FilePath { get; set; }
        public TextureType Type { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Channels { get; private set; }
        public byte[] Data { get; private set; }
        public TextureWrap WrapMode { get; set; } = TextureWrap.Repeat;
        public TextureFilter FilterMode { get; set; } = TextureFilter.Bilinear;
        public bool IsLoaded { get; private set; }
        public bool HasMipmaps { get; private set; }

        private byte[][]? _mipmaps;

        public Texture(string name, int width, int height, int channels = 4)
        {
            Name = name;
            Width = width;
            Height = height;
            Channels = channels;
            Data = new byte[width * height * channels];
            IsLoaded = true;
        }

        public Texture(string name, string filePath, TextureType type = TextureType.Diffuse)
        {
            Name = name;
            FilePath = filePath;
            Type = type;
            Data = Array.Empty<byte>();
        }

        /// <summary>
        /// تحميل تكستشر من ملف
        /// </summary>
        public static Texture? LoadFromFile(string filePath, TextureType type = TextureType.Diffuse)
        {
            if (!File.Exists(filePath))
                return null;

            try
            {
                // محاولة تحميل باستخدام System.Drawing
                using var stream = File.OpenRead(filePath);
                using var bitmap = new System.Drawing.Bitmap(stream);
                
                int w = bitmap.Width;
                int h = bitmap.Height;
                var data = new byte[w * h * 4];

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        var pixel = bitmap.GetPixel(x, y);
                        int idx = (y * w + x) * 4;
                        data[idx] = pixel.R;
                        data[idx + 1] = pixel.G;
                        data[idx + 2] = pixel.B;
                        data[idx + 3] = pixel.A;
                    }
                }

                var texture = new Texture(Path.GetFileNameWithoutExtension(filePath), w, h, 4)
                {
                    FilePath = filePath,
                    Type = type,
                    Data = data
                };

                texture.GenerateMipmaps();
                return texture;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// تحميل تكستشر من بايتس
        /// </summary>
        public static Texture FromBytes(string name, byte[] imageData, TextureType type = TextureType.Diffuse)
        {
            using var stream = new MemoryStream(imageData);
            using var bitmap = new System.Drawing.Bitmap(stream);
            
            int w = bitmap.Width;
            int h = bitmap.Height;
            var data = new byte[w * h * 4];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    int idx = (y * w + x) * 4;
                    data[idx] = pixel.R;
                    data[idx + 1] = pixel.G;
                    data[idx + 2] = pixel.B;
                    data[idx + 3] = pixel.A;
                }
            }

            return new Texture(name, w, h, 4) { Type = type, Data = data };
        }

        /// <summary>
        /// إنشاء تكستشر لون خالص
        /// </summary>
        public static Texture CreateSolid(string name, Color color, int size = 4)
        {
            var tex = new Texture(name, size, size, 4);
            for (int i = 0; i < size * size; i++)
            {
                tex.Data[i * 4] = color.R;
                tex.Data[i * 4 + 1] = color.G;
                tex.Data[i * 4 + 2] = color.B;
                tex.Data[i * 4 + 3] = color.A;
            }
            tex.IsLoaded = true;
            return tex;
        }

        /// <summary>
        /// إنشاء تكستشر شطرنج
        /// </summary>
        public static Texture CreateCheckerboard(string name, int size = 256, Color? color1 = null, Color? color2 = null)
        {
            var c1 = color1 ?? Color.White;
            var c2 = color2 ?? Color.LightGray;
            var tex = new Texture(name, size, size, 4);

            int cellSize = size / 8;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isWhite = ((x / cellSize) + (y / cellSize)) % 2 == 0;
                    var c = isWhite ? c1 : c2;
                    int idx = (y * size + x) * 4;
                    tex.Data[idx] = c.R;
                    tex.Data[idx + 1] = c.G;
                    tex.Data[idx + 2] = c.B;
                    tex.Data[idx + 3] = c.A;
                }
            }

            tex.IsLoaded = true;
            return tex;
        }

        /// <summary>
        /// إنشاء تكستشر تدرج
        /// </summary>
        public static Texture CreateGradient(string name, Color start, Color end, int size = 256, bool horizontal = true)
        {
            var tex = new Texture(name, size, size, 4);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float t = horizontal ? (float)x / size : (float)y / size;
                    var c = start.Lerp(end, t);
                    int idx = (y * size + x) * 4;
                    tex.Data[idx] = c.R;
                    tex.Data[idx + 1] = c.G;
                    tex.Data[idx + 2] = c.B;
                    tex.Data[idx + 3] = c.A;
                }
            }

            tex.IsLoaded = true;
            return tex;
        }

        /// <summary>
        /// إنشاء تكستشر ضوضاء
        /// </summary>
        public static Texture CreateNoise(string name, int size = 256, float scale = 50f, int seed = 0)
        {
            var tex = new Texture(name, size, size, 4);
            var rng = new Random(seed);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    byte v = (byte)rng.Next(256);
                    int idx = (y * size + x) * 4;
                    tex.Data[idx] = v;
                    tex.Data[idx + 1] = v;
                    tex.Data[idx + 2] = v;
                    tex.Data[idx + 3] = 255;
                }
            }

            tex.IsLoaded = true;
            return tex;
        }

        /// <summary>
        /// إنشاء Normal Map من Height Map
        /// </summary>
        public Texture CreateNormalMap(float strength = 1f)
        {
            if (!IsLoaded || Channels < 1) return this;

            var normal = new Texture(Name + "_normal", Width, Height, 4);

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float left = GetHeight(x - 1, y);
                    float right = GetHeight(x + 1, y);
                    float up = GetHeight(x, y - 1);
                    float down = GetHeight(x, y + 1);

                    float dx = (left - right) * strength;
                    float dy = (up - down) * strength;

                    Vector3 n = new Vector3(dx, dy, 1f).Normalized();

                    int idx = (y * Width + x) * 4;
                    normal.Data[idx] = (byte)((n.X * 0.5f + 0.5f) * 255);
                    normal.Data[idx + 1] = (byte)((n.Y * 0.5f + 0.5f) * 255);
                    normal.Data[idx + 2] = (byte)((n.Z * 0.5f + 0.5f) * 255);
                    normal.Data[idx + 3] = 255;
                }
            }

            normal.IsLoaded = true;
            return normal;
        }

        private float GetHeight(int x, int y)
        {
            x = Math.Clamp(x, 0, Width - 1);
            y = Math.Clamp(y, 0, Height - 1);
            int idx = (y * Width + x) * Channels;
            return Data[idx] / 255f;
        }

        /// <summary>
        /// توليد Mipmaps
        /// </summary>
        public void GenerateMipmaps()
        {
            if (!IsLoaded) return;

            int levels = (int)MathF.Floor(MathF.Log2(Math.Max(Width, Height))) + 1;
            _mipmaps = new byte[levels][];
            _mipmaps[0] = Data;

            int w = Width, h = Height;
            for (int i = 1; i < levels; i++)
            {
                w = Math.Max(1, w / 2);
                h = Math.Max(1, h / 2);
                _mipmaps[i] = new byte[w * h * Channels];

                // Simple box filter downsample
                var prev = _mipmaps[i - 1];
                int pw = Width >> (i - 1);
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int srcX = x * 2, srcY = y * 2;
                        for (int c = 0; c < Channels; c++)
                        {
                            int s00 = ((srcY * pw + srcX) * Channels + c);
                            int s10 = s00 + Channels;
                            int s01 = s00 + pw * Channels;
                            int s11 = s01 + Channels;

                            int v = (prev[s00] + prev[s10] + prev[s01] + prev[s11]) / 4;
                            _mipmaps[i][(y * w + x) * Channels + c] = (byte)v;
                        }
                    }
                }
            }

            HasMipmaps = true;
        }

        /// <summary>
        /// أخذ عينة من التكستشر (Sampling)
        /// </summary>
        public Color Sample(float u, float v)
        {
            if (!IsLoaded || Data.Length == 0)
                return Color.Magenta;

            // Apply wrap mode
            u = ApplyWrap(u, WrapMode);
            v = ApplyWrap(v, WrapMode);

            // To pixel coordinates
            float px = u * (Width - 1);
            float py = v * (Height - 1);

            if (FilterMode == TextureFilter.Nearest)
            {
                int x = (int)MathF.Round(px);
                int y = (int)MathF.Round(py);
                return GetPixel(x, y);
            }
            else // Bilinear
            {
                int x0 = (int)MathF.Floor(px);
                int y0 = (int)MathF.Floor(py);
                int x1 = Math.Min(x0 + 1, Width - 1);
                int y1 = Math.Min(y0 + 1, Height - 1);

                float fx = px - x0;
                float fy = py - y0;

                Color c00 = GetPixel(x0, y0);
                Color c10 = GetPixel(x1, y0);
                Color c01 = GetPixel(x0, y1);
                Color c11 = GetPixel(x1, y1);

                return LerpColors(LerpColors(c00, c10, fx), LerpColors(c01, c11, fx), fy);
            }
        }

        private Color GetPixel(int x, int y)
        {
            x = Math.Clamp(x, 0, Width - 1);
            y = Math.Clamp(y, 0, Height - 1);
            int idx = (y * Width + x) * Channels;
            return Channels switch
            {
                1 => new Color(Data[idx], Data[idx], Data[idx]),
                3 => new Color(Data[idx], Data[idx + 1], Data[idx + 2]),
                4 => new Color(Data[idx], Data[idx + 1], Data[idx + 2], Data[idx + 3]),
                _ => Color.Magenta
            };
        }

        public void SetPixel(int x, int y, Color color)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            int idx = (y * Width + x) * Channels;
            Data[idx] = color.R;
            if (Channels >= 3)
            {
                Data[idx + 1] = color.G;
                Data[idx + 2] = color.B;
            }
            if (Channels >= 4)
                Data[idx + 3] = color.A;
        }

        private static float ApplyWrap(float v, TextureWrap wrap) => wrap switch
        {
            TextureWrap.Repeat => v - MathF.Floor(v),
            TextureWrap.ClampToEdge => Math.Clamp(v, 0, 1),
            TextureWrap.MirroredRepeat => (int)MathF.Floor(v) % 2 == 0 ? v - MathF.Floor(v) : 1 - (v - MathF.Floor(v)),
            _ => v
        };

        private static Color LerpColors(Color a, Color b, float t) => a.Lerp(b, t);

        /// <summary>
        /// حفظ التكستشر كصورة
        /// </summary>
        public void SaveToFile(string filePath)
        {
            if (!IsLoaded) return;

            using var bitmap = new System.Drawing.Bitmap(Width, Height);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var c = GetPixel(x, y);
                    bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B));
                }
            }

            string ext = Path.GetExtension(filePath).ToLower();
            var format = ext switch
            {
                ".jpg" or ".jpeg" => System.Drawing.Imaging.ImageFormat.Jpeg,
                ".bmp" => System.Drawing.Imaging.ImageFormat.Bmp,
                ".gif" => System.Drawing.Imaging.ImageFormat.Gif,
                ".tiff" or ".tif" => System.Drawing.Imaging.ImageFormat.Tiff,
                _ => System.Drawing.Imaging.ImageFormat.Png
            };

            bitmap.Save(filePath, format);
        }

        /// <summary>
        /// نسخ التكستشر
        /// </summary>
        public Texture Clone()
        {
            var clone = new Texture(Name, Width, Height, Channels)
            {
                FilePath = FilePath,
                Type = Type,
                WrapMode = WrapMode,
                FilterMode = FilterMode
            };
            Array.Copy(Data, clone.Data, Data.Length);
            if (_mipmaps != null)
            {
                clone._mipmaps = new byte[_mipmaps.Length][];
                for (int i = 0; i < _mipmaps.Length; i++)
                {
                    clone._mipmaps[i] = new byte[_mipmaps[i].Length];
                    Array.Copy(_mipmaps[i], clone._mipmaps[i], _mipmaps[i].Length);
                }
                clone.HasMipmaps = true;
            }
            return clone;
        }

        /// <summary>
        /// تطبيق Blur
        /// </summary>
        public void ApplyBlur(int radius = 1)
        {
            if (!IsLoaded) return;
            var result = new byte[Width * Height * Channels];
            Array.Copy(Data, result, Data.Length);

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int r = 0, g = 0, b = 0, a = 0, count = 0;
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            var c = GetPixel(x + dx, y + dy);
                            r += c.R; g += c.G; b += c.B; a += c.A;
                            count++;
                        }
                    }
                    int idx = (y * Width + x) * Channels;
                    result[idx] = (byte)(r / count);
                    if (Channels >= 3) { result[idx + 1] = (byte)(g / count); result[idx + 2] = (byte)(b / count); }
                    if (Channels >= 4) result[idx + 3] = (byte)(a / count);
                }
            }
            Data = result;
        }

        /// <summary>
        /// تطبيق Sharpen
        /// </summary>
        public void ApplySharpen(float amount = 1f)
        {
            if (!IsLoaded) return;
            var result = new byte[Width * Height * Channels];

            float[,] kernel = {
                { 0, -amount, 0 },
                { -amount, 1 + 4 * amount, -amount },
                { 0, -amount, 0 }
            };

            ApplyKernel(kernel, result);
            Data = result;
        }

        /// <summary>
        /// تطبيق Emboss
        /// </summary>
        public void ApplyEmboss()
        {
            if (!IsLoaded) return;
            var result = new byte[Width * Height * Channels];

            float[,] kernel = {
                { -2, -1, 0 },
                { -1, 1, 1 },
                { 0, 1, 2 }
            };

            ApplyKernel(kernel, result);
            Data = result;
        }

        /// <summary>
        /// تطبيق Edge Detection
        /// </summary>
        public void ApplyEdgeDetect()
        {
            if (!IsLoaded) return;
            var result = new byte[Width * Height * Channels];

            float[,] kernel = {
                { -1, -1, -1 },
                { -1, 8, -1 },
                { -1, -1, -1 }
            };

            ApplyKernel(kernel, result);
            Data = result;
        }

        /// <summary>
        /// قلب الألوان (Invert)
        /// </summary>
        public void ApplyInvert()
        {
            for (int i = 0; i < Data.Length; i += Channels)
            {
                Data[i] = (byte)(255 - Data[i]);
                if (Channels >= 3) { Data[i + 1] = (byte)(255 - Data[i + 1]); Data[i + 2] = (byte)(255 - Data[i + 2]); }
            }
        }

        /// <summary>
        /// تحويل لرمادي
        /// </summary>
        public void ApplyGrayscale()
        {
            for (int i = 0; i < Data.Length; i += Channels)
            {
                byte gray = (byte)(Data[i] * 0.299f + Data[i + 1] * 0.587f + Data[i + 2] * 0.114f);
                Data[i] = Data[i + 1] = Data[i + 2] = gray;
            }
        }

        /// <summary>
        /// تعديل السطوع والتباين
        /// </summary>
        public void AdjustBrightnessContrast(float brightness = 0, float contrast = 0)
        {
            float factor = (259 * (contrast + 255)) / (255 * (259 - contrast));
            for (int i = 0; i < Data.Length; i += Channels)
            {
                for (int c = 0; c < Math.Min(3, Channels); c++)
                {
                    float v = Data[i + c] + brightness;
                    v = factor * (v - 128) + 128;
                    Data[i + c] = (byte)Math.Clamp(v, 0, 255);
                }
            }
        }

        private void ApplyKernel(float[,] kernel, byte[] result)
        {
            int kSize = kernel.GetLength(0);
            int kHalf = kSize / 2;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float r = 0, g = 0, b = 0;
                    for (int ky = 0; ky < kSize; ky++)
                    {
                        for (int kx = 0; kx < kSize; kx++)
                        {
                            var c = GetPixel(x + kx - kHalf, y + ky - kHalf);
                            r += c.R * kernel[ky, kx];
                            g += c.G * kernel[ky, kx];
                            b += c.B * kernel[ky, kx];
                        }
                    }
                    int idx = (y * Width + x) * Channels;
                    result[idx] = (byte)Math.Clamp(r, 0, 255);
                    if (Channels >= 3) { result[idx + 1] = (byte)Math.Clamp(g, 0, 255); result[idx + 2] = (byte)Math.Clamp(b, 0, 255); }
                    if (Channels >= 4) result[idx + 3] = Data[idx + 3];
                }
            }
        }

        public void Dispose()
        {
            Data = Array.Empty<byte>();
            _mipmaps = null;
            IsLoaded = false;
        }
    }
}
