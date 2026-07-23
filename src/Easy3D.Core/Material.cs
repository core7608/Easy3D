using System;
using System.Collections.Generic;

namespace Easy3D.Core
{
    /// <summary>
    /// نمط الإضاءة (Shading Model)
    /// </summary>
    public enum ShadingModel
    {
        Unlit,          // بدون إضاءة
        Lambert,        // انتشاري فقط
        Phong,          // فونغ
        BlinnPhong,     // بلين-فونغ
        PBR,            // فيزيائي (PBR)
        Toon,           // كرتوني
        Flat            // مسطح
    }

    /// <summary>
    /// مادة PBR متكاملة
    /// </summary>
    public class Material : IDisposable
    {
        // المعلومات الأساسية
        public string Name { get; set; }
        public ShadingModel ShadingModel { get; set; } = ShadingModel.PBR;
        public bool DoubleSided { get; set; } = false;
        public bool Transparent { get; set; } = false;
        public int RenderQueue { get; set; } = 2000;

        // الألوان
        public Color AlbedoColor { get; set; } = Color.DefaultMesh;
        public Color EmissiveColor { get; set; } = Color.Black;
        public Color SpecularColor { get; set; } = Color.White;
        public Color AmbientColor { get; set; } = new(30, 30, 30);
        public ColorTransparentColor { get; set; } = Color.White;

        // خصائص PBR
        public float Metalness { get; set; } = 0f;
        public float Roughness { get; set; } = 0.5f;
        public float Specular { get; set; } = 0.5f;
        public float Shininess { get; set; } = 32f;
        public float Opacity { get; set; } = 1f;
        public float EmissiveIntensity { get; set; } = 0f;
        public float NormalStrength { get; set; } = 1f;
        public float OcclusionStrength { get; set; } = 1f;
        public float AlphaCutoff { get; set; } = 0.5f;
        public float IOR { get; set; } = 1.5f;        // معامل الانكسار
        public float ClearCoat { get; set; } = 0f;
        public float ClearCoatRoughness { get; set; } = 0f;

        // UV Transform
        public Vector2 UVOffset { get; set; } = Vector2.Zero;
        public Vector2 UVScale { get; set; } = Vector2.One;
        public float UVRotate { get; set; } = 0f;

        // التكستشرات
        public Dictionary<TextureType, Texture> Textures { get; } = new();

        // الخصائص المساعدة
        public bool IsEmissive => EmissiveIntensity > 0;
        public bool IsTransparent => Opacity < 1f || Transparent;
        public bool HasTexture => Textures.Count > 0;

        public Material(string name = "Material")
        {
            Name = name;
        }

        public Material(string name, Color albedo) : this(name)
        {
            AlbedoColor = albedo;
        }

        // ==================== إدارة التكستشرات ====================

        public void SetTexture(TextureType type, Texture texture) => Textures[type] = texture;
        public Texture? GetTexture(TextureType type) => Textures.TryGetValue(type, out var tex) ? tex : null;
        public bool HasTextureType(TextureType type) => Textures.ContainsKey(type);
        public void RemoveTexture(TextureType type) => Textures.Remove(type);
        public void ClearTextures() => Textures.Clear();

        // ==================== حساب الإضاءة ====================

        /// <summary>
        /// حساب لون الإضاءة (Blinn-Phong)
        /// </summary>
        public Color CalculateLighting(Color baseColor, Vector3 normal, Vector3 lightDir, Vector3 viewDir, 
                                       Color lightColor, float lightIntensity)
        {
            switch (ShadingModel)
            {
                case ShadingModel.Unlit:
                    return baseColor;
                    
                case ShadingModel.Flat:
                    return baseColor.Multiply(lightIntensity);
                    
                case ShadingModel.Toon:
                    return CalculateToon(baseColor, normal, lightDir, lightColor, lightIntensity);
                    
                case ShadingModel.Lambert:
                    return CalculateLambert(baseColor, normal, lightDir, lightColor, lightIntensity);
                    
                case ShadingModel.Phong:
                    return CalculatePhong(baseColor, normal, lightDir, viewDir, lightColor, lightIntensity);
                    
                case ShadingModel.PBR:
                    return CalculatePBR(baseColor, normal, lightDir, viewDir, lightColor, lightIntensity);
                    
                default: // BlinnPhong
                    return CalculateBlinnPhong(baseColor, normal, lightDir, viewDir, lightColor, lightIntensity);
            }
        }

        private Color CalculateLambert(Color baseColor, Vector3 normal, Vector3 lightDir, Color lightColor, float intensity)
        {
            float ambient = 0.15f;
            float diff = Math.Max(0, normal.Dot(lightDir));
            return baseColor.Multiply(ambient + diff * intensity).Add(lightColor.Multiply(diff * 0.1f));
        }

        private Color CalculatePhong(Color baseColor, Vector3 normal, Vector3 lightDir, Vector3 viewDir, Color lightColor, float intensity)
        {
            float ambient = 0.15f;
            float diff = Math.Max(0, normal.Dot(lightDir));
            
            Vector3 reflect = normal * (2 * normal.Dot(lightDir)) - lightDir;
            float spec = MathF.Pow(Math.Max(0, viewDir.Dot(reflect)), Shininess);
            
            Color result = baseColor.Multiply(ambient + diff * (1 - Metalness));
            result = result.Add(SpecularColor.Multiply(spec * Specular));
            result = result.Add(lightColor.Multiply(diff * 0.1f));
            result = result.Multiply(intensity);
            return result;
        }

        private Color CalculateBlinnPhong(Color baseColor, Vector3 normal, Vector3 lightDir, Vector3 viewDir, Color lightColor, float intensity)
        {
            float ambient = 0.15f;
            float diff = Math.Max(0, normal.Dot(lightDir));
            
            Vector3 halfway = (lightDir + viewDir).Normalized();
            float spec = MathF.Pow(Math.Max(0, normal.Dot(halfway)), Shininess);
            
            Color result = baseColor.Multiply(ambient + diff * (1 - Metalness));
            result = result.Add(SpecularColor.Multiply(spec * Specular * (1 - Roughness)));
            
            if (Metalness > 0)
                result = result.Lerp(baseColor.Multiply(diff), Metalness);
            
            result = result.Add(lightColor.Multiply(diff * 0.1f));
            result = result.Multiply(intensity);
            return result;
        }

        private Color CalculatePBR(Color baseColor, Vector3 normal, Vector3 lightDir, Vector3 viewDir, Color lightColor, float intensity)
        {
            // Simplified PBR (Cook-Torrance)
            float NdotL = Math.Max(0, normal.Dot(lightDir));
            float NdotV = Math.Max(0, normal.Dot(viewDir));
            Vector3 halfway = (lightDir + viewDir).Normalized();
            float NdotH = Math.Max(0, normal.Dot(halfway));
            float VdotH = Math.Max(0, viewDir.Dot(halfway));

            // GGX Distribution
            float a = Roughness * Roughness;
            float a2 = a * a;
            float denom = NdotH * NdotH * (a2 - 1) + 1;
            float D = a2 / (MathF.PI * denom * denom);

            // Fresnel (Schlick)
            Color F0 = Color.Lerp(new Color(0.04f, 0.04f, 0.04f), baseColor, Metalness);
            Color F = F0.Lerp(Color.White, MathF.Pow(1 - VdotH, 5));

            // Geometry (Smith GGX)
            float k = (Roughness + 1) * (Roughness + 1) / 8;
            float G1L = NdotL / (NdotL * (1 - k) + k);
            float G1V = NdotV / (NdotV * (1 - k) + k);
            float G = G1L * G1V;

            // Combine
            Color specular = Color.Lerp(F, Color.Black, Roughness).Multiply(D * G / Math.Max(0.001f, 4 * NdotV * NdotL));
            
            Color kD = Color.Lerp(Color.White, Color.Black, Metalness).Multiply(1 - F.Rf);
            Color diffuse = baseColor.Multiply(kD).Multiply(NdotL);

            Color result = diffuse.Add(specular).Multiply(lightColor).Multiply(intensity);
            
            // Ambient
            Color ambient = baseColor.Multiply(0.03f * (1 - Metalness));
            result = result.Add(ambient);

            // Emissive
            if (IsEmissive)
                result = result.Add(EmissiveColor.Multiply(EmissiveIntensity));

            return result;
        }

        private Color CalculateToon(Color baseColor, Vector3 normal, Vector3 lightDir, Color lightColor, float intensity)
        {
            float diff = normal.Dot(lightDir);
            float steps = 4;
            diff = MathF.Floor(diff * steps) / steps;
            diff = Math.Max(0, diff);
            return baseColor.Multiply(0.2f + diff * 0.8f).Multiply(intensity);
        }

        // ==================== التحويلات UV ====================

        public Vector2 TransformUV(Vector2 uv)
        {
            uv = (uv - new Vector2(0.5f, 0.5f)) * UVScale;
            if (MathF.Abs(UVRotate) > 0.001f)
            {
                float c = MathF.Cos(UVRotate);
                float s = MathF.Sin(UVRotate);
                uv = new Vector2(uv.X * c - uv.Y * s, uv.X * s + uv.Y * c);
            }
            return uv + new Vector2(0.5f, 0.5f) + UVOffset;
        }

        // ==================== المواد الجاهزة ====================

        public static Material Default => new("Default");
        
        public static Material CreateMetal(Color color, float roughness = 0.1f) => new("Metal")
        {
            AlbedoColor = color, Metalness = 0.9f, Roughness = roughness, Shininess = 128f
        };

        public static Material CreatePlastic(Color color, float roughness = 0.4f) => new("Plastic")
        {
            AlbedoColor = color, Metalness = 0f, Roughness = roughness, Shininess = 64f
        };

        public static Material CreateGlass(Color color, float opacity = 0.3f) => new("Glass")
        {
            AlbedoColor = color, Opacity = opacity, Transparent = true,
            Specular = 0.8f, Shininess = 128f, Roughness = 0f, IOR = 1.52f
        };

        public static Material CreateEmissive(Color color, float intensity = 1f) => new("Emissive")
        {
            AlbedoColor = color, EmissiveColor = color, EmissiveIntensity = intensity
        };

        public static Material CreateWood(Color color) => new("Wood")
        {
            AlbedoColor = color, Metalness = 0f, Roughness = 0.8f, Shininess = 8f
        };

        public static Material CreateRubber(Color color) => new("Rubber")
        {
            AlbedoColor = color, Metalness = 0f, Roughness = 1f, Shininess = 2f
        };

        public static Material CreateFabric(Color color) => new("Fabric")
        {
            AlbedoColor = color, Metalness = 0f, Roughness = 0.9f, Shininess = 4f
        };

        public static Material CreateMarble(Color color) => new("Marble")
        {
            AlbedoColor = color, Metalness = 0f, Roughness = 0.2f, Shininess = 96f, Specular = 0.6f
        };

        public static Material CreateChrome(Color color) => new("Chrome")
        {
            AlbedoColor = color, Metalness = 1f, Roughness = 0.05f, Shininess = 256f
        };

        public static Material CreateCarPaint(Color color) => new("CarPaint")
        {
            AlbedoColor = color, Metalness = 0.3f, Roughness = 0.15f, Shininess = 128f,
            ClearCoat = 1f, ClearCoatRoughness = 0.05f
        };

        public Material Clone()
        {
            var clone = new Material(Name)
            {
                ShadingModel = ShadingModel,
                DoubleSided = DoubleSided,
                Transparent = Transparent,
                RenderQueue = RenderQueue,
                AlbedoColor = AlbedoColor,
                EmissiveColor = EmissiveColor,
                SpecularColor = SpecularColor,
                AmbientColor = AmbientColor,
                Metalness = Metalness,
                Roughness = Roughness,
                Specular = Specular,
                Shininess = Shininess,
                Opacity = Opacity,
                EmissiveIntensity = EmissiveIntensity,
                NormalStrength = NormalStrength,
                OcclusionStrength = OcclusionStrength,
                AlphaCutoff = AlphaCutoff,
                IOR = IOR,
                ClearCoat = ClearCoat,
                ClearCoatRoughness = ClearCoatRoughness,
                UVOffset = UVOffset,
                UVScale = UVScale,
                UVRotate = UVRotate
            };
            foreach (var kvp in Textures)
                clone.Textures[kvp.Key] = kvp.Value.Clone();
            return clone;
        }

        public void Dispose()
        {
            foreach (var tex in Textures.Values)
                tex.Dispose();
            Textures.Clear();
        }
    }
}
