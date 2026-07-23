using System;

namespace Easy3D.Core
{
    public enum LightType { Directional, Point, Spot, Area, Hemisphere }

    public class Light
    {
        public string Name { get; set; }
        public LightType Type { get; set; }
        public Color Color { get; set; }
        public float Intensity { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Direction { get; set; }
        public float Range { get; set; }
        public float SpotAngle { get; set; }
        public float SpotOuterAngle { get; set; }
        public float AttenuationConstant { get; set; } = 1f;
        public float AttenuationLinear { get; set; } = 0.09f;
        public float AttenuationQuadratic { get; set; } = 0.032f;
        public bool CastShadows { get; set; }
        public float ShadowStrength { get; set; }
        public int ShadowResolution { get; set; } = 1024;
        public float ShadowBias { get; set; } = 0.005f;
        public Color GroundColor { get; set; } = new(80, 80, 80);

        public Light(string name = "Light")
        {
            Name = name;
            Type = LightType.Directional;
            Color = Color.White;
            Intensity = 1f;
            Direction = new Vector3(-1, -1, -1).Normalized();
            Range = 10f;
            SpotAngle = 30f;
            SpotOuterAngle = 45f;
            CastShadows = true;
            ShadowStrength = 0.5f;
        }

        public float CalculateAttenuation(Vector3 point)
        {
            if (Type == LightType.Directional) return 1f;
            float distance = (point - Position).Length;
            if (distance > Range) return 0f;
            float att = 1f / (AttenuationConstant + AttenuationLinear * distance + AttenuationQuadratic * distance * distance);
            if (Type == LightType.Spot)
            {
                Vector3 toPoint = (point - Position).Normalized();
                float cos = toPoint.Dot(Direction);
                float cosSpot = MathF.Cos(SpotAngle * MathF.PI / 360);
                float cosOuter = MathF.Cos(SpotOuterAngle * MathF.PI / 360);
                if (cos < cosOuter) return 0f;
                att *= Math.Clamp((cos - cosOuter) / (cosSpot - cosOuter), 0, 1);
            }
            return att;
        }

        public Vector3 GetDirectionToLight(Vector3 point) =>
            Type == LightType.Directional ? -Direction : (Position - point).Normalized();

        public static Light CreateDirectional(string name = "Sun", Vector3? dir = null, Color? color = null, float intensity = 1f) =>
            new(name) { Type = LightType.Directional, Direction = dir ?? new(-1, -1, -1).Normalized(), Color = color ?? Color.White, Intensity = intensity };

        public static Light CreatePoint(string name = "Point Light", Vector3? pos = null, Color? color = null, float intensity = 1f, float range = 10f) =>
            new(name) { Type = LightType.Point, Position = pos ?? new(0, 3, 0), Color = color ?? Color.White, Intensity = intensity, Range = range };

        public static Light CreateSpot(string name = "Spot Light", Vector3? pos = null, Vector3? dir = null, Color? color = null, float intensity = 1f, float angle = 30f) =>
            new(name) { Type = LightType.Spot, Position = pos ?? new(0, 3, 0), Direction = dir ?? new(0, -1, 0), Color = color ?? Color.White, Intensity = intensity, SpotAngle = angle, SpotOuterAngle = angle * 1.2f };

        public static Light CreateHemisphere(string name = "Hemisphere", Color? sky = null, Color? ground = null, float intensity = 0.5f) =>
            new(name) { Type = LightType.Hemisphere, Color = sky ?? new(135, 206, 235), GroundColor = ground ?? new(80, 80, 80), Intensity = intensity };

        public Light Clone() => new(Name)
        {
            Type = Type, Color = Color, Intensity = Intensity, Position = Position, Direction = Direction,
            Range = Range, SpotAngle = SpotAngle, SpotOuterAngle = SpotOuterAngle,
            AttenuationConstant = AttenuationConstant, AttenuationLinear = AttenuationLinear,
            AttenuationQuadratic = AttenuationQuadratic, CastShadows = CastShadows,
            ShadowStrength = ShadowStrength, ShadowResolution = ShadowResolution, ShadowBias = ShadowBias,
            GroundColor = GroundColor
        };
    }
}
