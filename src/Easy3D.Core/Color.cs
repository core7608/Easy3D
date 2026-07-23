using System;
using System.Runtime.InteropServices;

namespace Easy3D.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Color : IEquatable<Color>
    {
        public byte R, G, B, A;

        public Color(byte r, byte g, byte b, byte a = 255) { R = r; G = g; B = b; A = a; }
        public Color(float r, float g, float b, float a = 1f)
        {
            R = (byte)(Math.Clamp(r, 0, 1) * 255);
            G = (byte)(Math.Clamp(g, 0, 1) * 255);
            B = (byte)(Math.Clamp(b, 0, 1) * 255);
            A = (byte)(Math.Clamp(a, 0, 1) * 255);
        }
        public Color(int r, int g, int b, int a = 255)
        {
            R = (byte)Math.Clamp(r, 0, 255);
            G = (byte)Math.Clamp(g, 0, 255);
            B = (byte)Math.Clamp(b, 0, 255);
            A = (byte)Math.Clamp(a, 0, 255);
        }

        // Predefined
        public static readonly Color White = new(255, 255, 255);
        public static readonly Color Black = new(0, 0, 0);
        public static readonly Color Red = new(255, 0, 0);
        public static readonly Color Green = new(0, 255, 0);
        public static readonly Color Blue = new(0, 0, 255);
        public static readonly Color Yellow = new(255, 255, 0);
        public static readonly Color Cyan = new(0, 255, 255);
        public static readonly Color Magenta = new(255, 0, 255);
        public static readonly Color Orange = new(255, 165, 0);
        public static readonly Color Purple = new(128, 0, 128);
        public static readonly Color Gray = new(128, 128, 128);
        public static readonly Color DarkGray = new(64, 64, 64);
        public static readonly Color LightGray = new(192, 192, 192);
        public static readonly Color Transparent = new(0, 0, 0, 0);

        // Theme colors
        public static readonly Color DefaultMesh = new(74, 144, 217);
        public static readonly Color Selected = new(255, 200, 50);
        public static readonly Color Hover = new(100, 180, 255);
        public static readonly Color Grid = new(50, 50, 65);
        public static readonly Color GridMajor = new(70, 70, 90);
        public static readonly Color Background = new(30, 30, 46);
        public static readonly Color AxisX = new(200, 60, 60);
        public static readonly Color AxisY = new(60, 200, 60);
        public static readonly Color AxisZ = new(60, 60, 200);

        public readonly float Rf => R / 255f;
        public readonly float Gf => G / 255f;
        public readonly float Bf => B / 255f;
        public readonly float Af => A / 255f;

        public readonly Color WithAlpha(byte a) => new(R, G, B, a);
        public readonly Color WithAlpha(float a) => new(R, G, B, (byte)(a * 255));

        public readonly Color Lerp(Color target, float t)
        {
            t = Math.Clamp(t, 0, 1);
            return new(
                (byte)(R + (target.R - R) * t),
                (byte)(G + (target.G - G) * t),
                (byte)(B + (target.B - B) * t),
                (byte)(A + (target.A - A) * t)
            );
        }

        public readonly Color Multiply(float f) => new(
            (byte)Math.Clamp(R * f, 0, 255),
            (byte)Math.Clamp(G * f, 0, 255),
            (byte)Math.Clamp(B * f, 0, 255),
            A
        );

        public readonly Color Add(Color o) => new(
            (byte)Math.Clamp(R + o.R, 0, 255),
            (byte)Math.Clamp(G + o.G, 0, 255),
            (byte)Math.Clamp(B + o.B, 0, 255),
            A
        );

        public static Color FromHex(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length >= 6)
            {
                int r = Convert.ToInt32(hex[..2], 16);
                int g = Convert.ToInt32(hex[2..4], 16);
                int b = Convert.ToInt32(hex[4..6], 16);
                int a = hex.Length >= 8 ? Convert.ToInt32(hex[6..8], 16) : 255;
                return new Color(r, g, b, a);
            }
            return White;
        }

        public readonly string ToHex() => $"#{R:X2}{G:X2}{B:X2}";
        public readonly string ToHexAlpha() => $"#{R:X2}{G:X2}{B:X2}{A:X2}";

        public readonly (float H, float S, float L) ToHSL()
        {
            float r = Rf, g = Gf, b = Bf;
            float max = MathF.Max(r, MathF.Max(g, b));
            float min = MathF.Min(r, MathF.Min(g, b));
            float l = (max + min) / 2;
            if (max == min) return (0, 0, l);
            float d = max - min;
            float s = l > 0.5f ? d / (2 - max - min) : d / (max + min);
            float h = max == r ? ((g - b) / d + (g < b ? 6 : 0)) / 6 :
                      max == g ? ((b - r) / d + 2) / 6 :
                                 ((r - g) / d + 4) / 6;
            return (h * 360, s, l);
        }

        public static Color FromHSL(float h, float s, float l, float a = 1f)
        {
            h = ((h % 360) + 360) % 360;
            s = Math.Clamp(s, 0, 1);
            l = Math.Clamp(l, 0, 1);
            float c = (1 - MathF.Abs(2 * l - 1)) * s;
            float x = c * (1 - MathF.Abs((h / 60) % 2 - 1));
            float m = l - c / 2;
            float r, g, b;
            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }
            return new Color(r + m, g + m, b + m, a);
        }

        public static Color FromHSV(float h, float s, float v, float a = 1f)
        {
            h = ((h % 360) + 360) % 360;
            s = Math.Clamp(s, 0, 1);
            v = Math.Clamp(v, 0, 1);
            float c = v * s;
            float x = c * (1 - MathF.Abs((h / 60) % 2 - 1));
            float m = v - c;
            float r, g, b;
            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }
            return new Color(r + m, g + m, b + m, a);
        }

        public static Color Random(Random? rng = null)
        {
            rng ??= Random.Shared;
            return new Color(rng.Next(256), rng.Next(256), rng.Next(256));
        }

        public readonly bool Equals(Color other) => R == other.R && G == other.G && B == other.B && A == other.A;
        public readonly override bool Equals(object? obj) => obj is Color c && Equals(c);
        public readonly override int GetHashCode() => HashCode.Combine(R, G, B, A);
        public readonly override string ToString() => $"Color({R}, {G}, {B}, {A})";

        public static bool operator ==(Color a, Color b) => a.Equals(b);
        public static bool operator !=(Color a, Color b) => !a.Equals(b);
    }
}
