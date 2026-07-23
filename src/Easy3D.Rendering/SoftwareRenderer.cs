using Easy3D.Core;
using Easy3D.Geometry;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Easy3D.Rendering
{
    /// <summary>
    /// محرك رسم برمجي متقدم (Software Renderer)
    /// يعمل بدون GPU باستخدام SkiaSharp
    /// </summary>
    public class SoftwareRenderer : IDisposable
    {
        private SKBitmap _frameBuffer;
        private float[] _depthBuffer;
        private int _width, _height;
        private SKCanvas _canvas;

        // Stats
        public int TrianglesRendered { get; private set; }
        public int VerticesProcessed { get; private set; }
        public float FPS { get; private set; }
        private int _frameCount;
        private DateTime _lastFpsTime = DateTime.Now;

        // Settings
        public RenderMode RenderMode { get; set; } = RenderMode.Solid;
        public bool BackfaceCulling { get; set; } = true;
        public bool ShowGrid { get; set; } = true;
        public bool ShowAxes { get; set; } = true;
        public bool ShowBoundingBoxes { get; set; } = false;
        public bool ShowWireframe { get; set; } = false;
        public bool ShowNormals { get; set; } = false;
        public bool ShowUVs { get; set; } = false;
        public bool AntiAliasing { get; set; } = true;
        public bool Shadows { get; set; } = true;
        public bool AmbientOcclusion { get; set; } = false;
        public float GridOpacity { get; set; } = 0.5f;

        public SoftwareRenderer(int width, int height)
        {
            _width = width;
            _height = height;
            _frameBuffer = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            _depthBuffer = new float[width * height];
            _canvas = new SKCanvas(_frameBuffer);
        }

        public void Resize(int width, int height)
        {
            if (width <= 0 || height <= 0 || (width == _width && height == _height)) return;
            _width = width; _height = height;
            _canvas.Dispose(); _frameBuffer.Dispose();
            _frameBuffer = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            _depthBuffer = new float[width * height];
            _canvas = new SKCanvas(_frameBuffer);
        }

        public SKBitmap Render(Scene scene)
        {
            _canvas.Clear(ToSKColor(scene.BackgroundColor));
            Array.Fill(_depthBuffer, float.MaxValue);
            TrianglesRendered = 0; VerticesProcessed = 0;

            float aspect = (float)_width / _height;
            scene.Camera.UpdateProjectionMatrix(aspect);

            if (ShowGrid) DrawGrid(scene);
            if (ShowAxes) DrawAxes(scene);

            foreach (var obj in scene.Objects)
            {
                if (obj.Visible)
                {
                    DrawMesh(obj, scene);
                    if (ShowBoundingBoxes) DrawBoundingBox(obj, scene);
                }
            }

            UpdateFPS();
            return _frameBuffer;
        }

        private void DrawGrid(Scene scene)
        {
            float size = scene.GridSize;
            int divs = scene.GridDivisions;
            float step = size / divs;
            float half = size / 2;

            var gridPaint = new SKPaint { StrokeWidth = 1, IsAntialias = AntiAliasing };
            var majorPaint = new SKPaint { StrokeWidth = 1.5f, IsAntialias = AntiAliasing };

            for (int i = 0; i <= divs; i++)
            {
                float pos = -half + i * step;
                bool major = i % 5 == 0;
                var paint = major ? majorPaint : gridPaint;
                paint.Color = ToSKColor(major ? Color.GridMajor : Color.Grid).WithAlpha((byte)(GridOpacity * 255));

                var p1 = scene.Camera.WorldToScreen(new(pos, 0, -half), _width, _height);
                var p2 = scene.Camera.WorldToScreen(new(pos, 0, half), _width, _height);
                if (p1.HasValue && p2.HasValue)
                    _canvas.DrawLine(p1.Value.X, p1.Value.Y, p2.Value.X, p2.Value.Y, paint);

                p1 = scene.Camera.WorldToScreen(new(-half, 0, pos), _width, _height);
                p2 = scene.Camera.WorldToScreen(new(half, 0, pos), _width, _height);
                if (p1.HasValue && p2.HasValue)
                    _canvas.DrawLine(p1.Value.X, p1.Value.Y, p2.Value.X, p2.Value.Y, paint);
            }

            gridPaint.Dispose(); majorPaint.Dispose();
        }

        private void DrawAxes(Scene scene)
        {
            float len = scene.AxisLength;
            var origin = scene.Camera.WorldToScreen(Vector3.Zero, _width, _height);
            if (!origin.HasValue) return;

            var axisPaint = new SKPaint { StrokeWidth = 3, IsAntialias = AntiAliasing, StrokeCap = SKStrokeCap.Round };

            var xEnd = scene.Camera.WorldToScreen(new(len, 0, 0), _width, _height);
            if (xEnd.HasValue) { axisPaint.Color = ToSKColor(Color.AxisX); _canvas.DrawLine(origin.Value.X, origin.Value.Y, xEnd.Value.X, xEnd.Value.Y, axisPaint); }

            var yEnd = scene.Camera.WorldToScreen(new(0, len, 0), _width, _height);
            if (yEnd.HasValue) { axisPaint.Color = ToSKColor(Color.AxisY); _canvas.DrawLine(origin.Value.X, origin.Value.Y, yEnd.Value.X, yEnd.Value.Y, axisPaint); }

            var zEnd = scene.Camera.WorldToScreen(new(0, 0, len), _width, _height);
            if (zEnd.HasValue) { axisPaint.Color = ToSKColor(Color.AxisZ); _canvas.DrawLine(origin.Value.X, origin.Value.Y, zEnd.Value.X, zEnd.Value.Y, axisPaint); }

            // Axis labels
            var textPaint = new SKPaint { TextSize = 14, IsAntialias = true };
            if (xEnd.HasValue) { textPaint.Color = ToSKColor(Color.AxisX); _canvas.DrawText("X", xEnd.Value.X + 5, xEnd.Value.Y - 5, textPaint); }
            if (yEnd.HasValue) { textPaint.Color = ToSKColor(Color.AxisY); _canvas.DrawText("Y", yEnd.Value.X + 5, yEnd.Value.Y - 5, textPaint); }
            if (zEnd.HasValue) { textPaint.Color = ToSKColor(Color.AxisZ); _canvas.DrawText("Z", zEnd.Value.X + 5, zEnd.Value.Y - 5, textPaint); }

            axisPaint.Dispose(); textPaint.Dispose();
        }

        private void DrawMesh(Mesh3D mesh, Scene scene)
        {
            var worldVerts = mesh.GetWorldVertices();
            var rotMatrix = Matrix4x4.Rotation(mesh.Rotation);
            VerticesProcessed += mesh.Vertices.Count;

            var cameraPos = scene.Camera.Position;
            var cameraForward = scene.Camera.GetForward();

            // Prepare faces
            var facesToRender = new List<RenderFace>();
            foreach (var face in mesh.Faces)
            {
                if (face.Count < 3) continue;
                var screenPoints = new List<(float X, float Y, float Z)>();
                float avgDepth = 0;
                bool valid = true;

                foreach (int idx in face.Indices)
                {
                    if (idx >= worldVerts.Count) { valid = false; break; }
                    var sp = scene.Camera.WorldToScreen(worldVerts[idx], _width, _height);
                    if (!sp.HasValue) { valid = false; break; }
                    screenPoints.Add((sp.Value.X, sp.Value.Y, sp.Value.Z));
                    avgDepth += sp.Value.Z;
                }
                if (!valid) continue;
                avgDepth /= face.Count;

                // Normal
                var normal = rotMatrix.TransformDirection(mesh.Vertices[face.Indices[0]].Normal).Normalized();

                // Backface culling
                if (BackfaceCulling)
                {
                    var viewDir = (cameraPos - worldVerts[face.Indices[0]]).Normalized();
                    if (normal.Dot(viewDir) < -0.1f) continue;
                }

                // Material
                var mat = mesh.Materials.Count > face.MaterialIndex ? mesh.Materials[face.MaterialIndex] : mesh.Materials[0];
                var baseColor = mesh.Selected ? Color.Selected : mat.AlbedoColor;

                // Lighting
                Color finalColor = baseColor;
                foreach (var light in scene.Lights)
                {
                    float att = light.CalculateAttenuation(worldVerts[face.Indices[0]]);
                    if (att <= 0) continue;
                    var lightDir = light.GetDirectionToLight(worldVerts[face.Indices[0]]);
                    var viewDir = (cameraPos - worldVerts[face.Indices[0]]).Normalized();

                    float diff = Math.Max(0, normal.Dot(lightDir));
                    var halfway = (lightDir + viewDir).Normalized();
                    float spec = MathF.Pow(Math.Max(0, normal.Dot(halfway)), mat.Shininess);

                    var lightCol = light.Color.Multiply(light.Intensity * att);
                    finalColor = finalColor.Lerp(lightCol, diff * 0.7f * (1 - mat.Metalness));
                    finalColor = finalColor.Add(lightCol.Multiply(spec * mat.Specular * (1 - mat.Roughness)));
                }
                finalColor = finalColor.Add(scene.AmbientLight.Multiply(0.1f));
                if (mat.IsEmissive) finalColor = finalColor.Add(mat.EmissiveColor.Multiply(mat.EmissiveIntensity));
                finalColor.A = (byte)(mat.Opacity * 255);

                // Texture sampling
                if (mat.HasTextureType(TextureType.Diffuse))
                {
                    var tex = mat.GetTexture(TextureType.Diffuse);
                    if (tex != null && tex.IsLoaded)
                    {
                        // Sample at face center UV
                        var avgUV = Vector2.Zero;
                        foreach (int idx in face.Indices) avgUV += mesh.Vertices[idx].UV;
                        avgUV = avgUV / face.Count;
                        avgUV = mat.TransformUV(avgUV);
                        var texColor = tex.Sample(avgUV.X, avgUV.Y);
                        finalColor = new Color(
                            (byte)(finalColor.R * texColor.R / 255),
                            (byte)(finalColor.G * texColor.G / 255),
                            (byte)(finalColor.B * texColor.B / 255),
                            finalColor.A
                        );
                    }
                }

                facesToRender.Add(new RenderFace
                {
                    ScreenPoints = screenPoints,
                    Depth = avgDepth,
                    Color = finalColor,
                    Normal = normal,
                    Material = mat,
                    Selected = mesh.Selected
                });
            }

            // Sort back to front
            facesToRender.Sort((a, b) => b.Depth.CompareTo(a.Depth));

            // Draw
            foreach (var rf in facesToRender)
            {
                if (RenderMode == RenderMode.Solid || RenderMode == RenderMode.Textured)
                    DrawFilledPolygon(rf);
                if (ShowWireframe || RenderMode == RenderMode.Wireframe)
                    DrawWireframePolygon(rf);
                TrianglesRendered += rf.ScreenPoints.Count - 2;
            }

            // Selection indicator
            if (mesh.Selected)
            {
                var center = scene.Camera.WorldToScreen(mesh.Position, _width, _height);
                if (center.HasValue)
                {
                    var selPaint = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 2, Color = SKColors.Yellow, IsAntialias = true };
                    _canvas.DrawCircle(center.Value.X, center.Value.Y, 15, selPaint);
                    selPaint.Dispose();
                }

                // Transform gizmo
                if (mesh == scene.SelectedObject)
                    DrawTransformGizmo(mesh, scene);
            }

            // Normals
            if (ShowNormals)
                DrawNormals(mesh, worldVerts, scene);
        }

        private void DrawFilledPolygon(RenderFace rf)
        {
            if (rf.ScreenPoints.Count < 3) return;
            using var path = new SKPath();
            path.MoveTo(rf.ScreenPoints[0].X, rf.ScreenPoints[0].Y);
            for (int i = 1; i < rf.ScreenPoints.Count; i++)
                path.LineTo(rf.ScreenPoints[i].X, rf.ScreenPoints[i].Y);
            path.Close();

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = ToSKColor(rf.Color),
                IsAntialias = AntiAliasing
            };
            _canvas.DrawPath(path, paint);

            // Subtle outline
            if (rf.Selected)
            {
                using var outlinePaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 2,
                    Color = SKColors.Yellow,
                    IsAntialias = true
                };
                _canvas.DrawPath(path, outlinePaint);
            }
        }

        private void DrawWireframePolygon(RenderFace rf)
        {
            if (rf.ScreenPoints.Count < 2) return;
            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                Color = rf.Selected ? new SKColor(255, 255, 100) : new SKColor(200, 200, 200, 100),
                IsAntialias = AntiAliasing
            };
            for (int i = 0; i < rf.ScreenPoints.Count; i++)
            {
                int next = (i + 1) % rf.ScreenPoints.Count;
                _canvas.DrawLine(rf.ScreenPoints[i].X, rf.ScreenPoints[i].Y,
                               rf.ScreenPoints[next].X, rf.ScreenPoints[next].Y, paint);
            }
        }

        private void DrawBoundingBox(Mesh3D mesh, Scene scene)
        {
            var min = mesh.Bounds.Min;
            var max = mesh.Bounds.Max;
            var transform = mesh.GetTransformMatrix();

            var corners = new Vector3[]
            {
                new(min.X, min.Y, min.Z), new(max.X, min.Y, min.Z),
                new(max.X, max.Y, min.Z), new(min.X, max.Y, min.Z),
                new(min.X, min.Y, max.Z), new(max.X, min.Y, max.Z),
                new(max.X, max.Y, max.Z), new(min.X, max.Y, max.Z),
            };

            var screen = corners.Select(c => scene.Camera.WorldToScreen(transform.TransformPoint(c), _width, _height)).ToArray();

            using var paint = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 1, Color = new SKColor(255, 255, 0, 100), PathEffect = SKPathEffect.Dash(new float[] { 5, 5 }, 0) };
            int[,] edges = { { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 }, { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 }, { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } };
            for (int i = 0; i < 12; i++)
            {
                var p1 = screen[edges[i, 0]];
                var p2 = screen[edges[i, 1]];
                if (p1.HasValue && p2.HasValue)
                    _canvas.DrawLine(p1.Value.X, p1.Value.Y, p2.Value.X, p2.Value.Y, paint);
            }
        }

        private void DrawTransformGizmo(Mesh3D mesh, Scene scene)
        {
            float gizmoLen = 1f;
            var origin = scene.Camera.WorldToScreen(mesh.Position, _width, _height);
            if (!origin.HasValue) return;

            var paint = new SKPaint { StrokeWidth = 3, IsAntialias = true, StrokeCap = SKStrokeCap.Round };

            var xEnd = scene.Camera.WorldToScreen(mesh.Position + new Vector3(gizmoLen, 0, 0), _width, _height);
            if (xEnd.HasValue) { paint.Color = new SKColor(255, 80, 80); _canvas.DrawLine(origin.Value.X, origin.Value.Y, xEnd.Value.X, xEnd.Value.Y, paint); }

            var yEnd = scene.Camera.WorldToScreen(mesh.Position + new Vector3(0, gizmoLen, 0), _width, _height);
            if (yEnd.HasValue) { paint.Color = new SKColor(80, 255, 80); _canvas.DrawLine(origin.Value.X, origin.Value.Y, yEnd.Value.X, yEnd.Value.Y, paint); }

            var zEnd = scene.Camera.WorldToScreen(mesh.Position + new Vector3(0, 0, gizmoLen), _width, _height);
            if (zEnd.HasValue) { paint.Color = new SKColor(80, 80, 255); _canvas.DrawLine(origin.Value.X, origin.Value.Y, zEnd.Value.X, zEnd.Value.Y, paint); }

            paint.Dispose();
        }

        private void DrawNormals(Mesh3D mesh, List<Vector3> worldVerts, Scene scene)
        {
            var paint = new SKPaint { StrokeWidth = 1, Color = new SKColor(0, 255, 255, 100), IsAntialias = true };
            float normalLen = 0.1f;
            var rotMatrix = Matrix4x4.Rotation(mesh.Rotation);

            for (int i = 0; i < Math.Min(mesh.Vertices.Count, worldVerts.Count); i += 3)
            {
                var normal = rotMatrix.TransformDirection(mesh.Vertices[i].Normal).Normalized();
                var start = scene.Camera.WorldToScreen(worldVerts[i], _width, _height);
                var end = scene.Camera.WorldToScreen(worldVerts[i] + normal * normalLen, _width, _height);
                if (start.HasValue && end.HasValue)
                    _canvas.DrawLine(start.Value.X, start.Value.Y, end.Value.X, end.Value.Y, paint);
            }
            paint.Dispose();
        }

        private void UpdateFPS()
        {
            _frameCount++;
            var elapsed = (DateTime.Now - _lastFpsTime).TotalSeconds;
            if (elapsed >= 0.5)
            {
                FPS = (float)(_frameCount / elapsed);
                _frameCount = 0;
                _lastFpsTime = DateTime.Now;
            }
        }

        private static SKColor ToSKColor(Color c) => new(c.R, c.G, c.B, c.A);

        public void Dispose()
        {
            _canvas?.Dispose();
            _frameBuffer?.Dispose();
        }

        private struct RenderFace
        {
            public List<(float X, float Y, float Z)> ScreenPoints;
            public float Depth;
            public Color Color;
            public Vector3 Normal;
            public Material Material;
            public bool Selected;
        }
    }

    public enum RenderMode
    {
        Solid,
        Wireframe,
        Textured,
        Material,
        Lighting,
        Normal,
        UV,
        Depth
    }
}
