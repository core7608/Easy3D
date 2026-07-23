using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Easy3D.Core
{
    public class Scene
    {
        public List<Mesh3D> Objects { get; set; } = new();
        public List<Light> Lights { get; set; } = new();
        public Camera Camera { get; set; } = new();
        public Color BackgroundColor { get; set; } = new(30, 30, 46);
        public Color AmbientLight { get; set; } = new(40, 40, 50);
        public bool ShowGrid { get; set; } = true;
        public bool ShowAxes { get; set; } = true;
        public bool ShowBoundingBoxes { get; set; } = false;
        public float GridSize { get; set; } = 10f;
        public int GridDivisions { get; set; } = 20;
        public float AxisLength { get; set; } = 2f;
        public Mesh3D? SelectedObject { get; set; }
        public int SelectedIndex { get; set; } = -1;
        public string? FilePath { get; set; }
        public bool IsDirty { get; set; }

        // History
        private readonly List<string> _history = new();
        private int _historyIndex = -1;

        public Scene() { AddDefaultLights(); }

        private void AddDefaultLights()
        {
            Lights.Add(Light.CreateDirectional("Sun", new(-1, -1, -1).Normalized(), Color.White, 1f));
            Lights.Add(Light.CreateHemisphere("Ambient", new(100, 100, 120), new(40, 40, 50), 0.3f));
        }

        // Object management
        public void AddObject(Mesh3D obj) { Objects.Add(obj); SelectObject(Objects.Count - 1); IsDirty = true; }
        public void RemoveObject(Mesh3D obj) { int i = Objects.IndexOf(obj); Objects.Remove(obj); if (SelectedObject == obj) { if (Objects.Count > 0) SelectObject(Math.Min(i, Objects.Count - 1)); else DeselectAll(); } IsDirty = true; }
        public void RemoveSelected() { if (SelectedObject != null) RemoveObject(SelectedObject); }
        public void SelectObject(int index) { DeselectAll(); if (index >= 0 && index < Objects.Count) { SelectedObject = Objects[index]; SelectedObject.Selected = true; SelectedIndex = index; } }
        public void SelectObject(Mesh3D obj) { DeselectAll(); obj.Selected = true; SelectedObject = obj; SelectedIndex = Objects.IndexOf(obj); }
        public void DeselectAll() { foreach (var o in Objects) o.Selected = false; SelectedObject = null; SelectedIndex = -1; }
        public Mesh3D? DuplicateSelected() { if (SelectedObject == null) return null; var c = SelectedObject.Clone(); c.Position += new Vector3(1, 0, 0); AddObject(c); return c; }
        public Mesh3D? FindObject(string name) => Objects.FirstOrDefault(o => o.Name == name);

        // Light management
        public void AddLight(Light light) { Lights.Add(light); IsDirty = true; }
        public void RemoveLight(Light light) { Lights.Remove(light); IsDirty = true; }

        // Transforms
        public void MoveSelected(Vector3 delta) { if (SelectedObject != null) { SelectedObject.Position += delta; IsDirty = true; } }
        public void RotateSelected(Vector3 delta) { if (SelectedObject != null) { SelectedObject.Rotation += delta; IsDirty = true; } }
        public void ScaleSelected(Vector3 delta) { if (SelectedObject != null) { SelectedObject.Scale *= delta; IsDirty = true; } }
        public void ScaleSelectedUniform(float f) { if (SelectedObject != null) { SelectedObject.Scale *= f; IsDirty = true; } }

        // History
        public void SaveState() { _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1); _history.Add(Serialize()); _historyIndex = _history.Count - 1; }
        public void Undo() { if (_historyIndex > 0) { _historyIndex--; Deserialize(_history[_historyIndex]); } }
        public void Redo() { if (_historyIndex < _history.Count - 1) { _historyIndex++; Deserialize(_history[_historyIndex]); } }

        // Serialization
        public string Serialize()
        {
            var data = new
            {
                version = "2.0",
                objects = Objects.Select(o => new
                {
                    name = o.Name,
                    vertices = o.Vertices.Select(v => new { x = v.Position.X, y = v.Position.Y, z = v.Position.Z, nx = v.Normal.X, ny = v.Normal.Y, nz = v.Normal.Z, u = v.UV.X, v = v.UV.Y }).ToArray(),
                    faces = o.Faces.Select(f => f.Indices.ToArray()).ToArray(),
                    position = new { x = o.Position.X, y = o.Position.Y, z = o.Position.Z },
                    rotation = new { x = o.Rotation.X, y = o.Rotation.Y, z = o.Rotation.Z },
                    scale = new { x = o.Scale.X, y = o.Scale.Y, z = o.Scale.Z },
                    materials = o.Materials.Select(m => new
                    {
                        name = m.Name,
                        shadingModel = m.ShadingModel.ToString(),
                        albedo = m.AlbedoColor.ToHex(),
                        emissive = m.EmissiveColor.ToHex(),
                        metalness = m.Metalness,
                        roughness = m.Roughness,
                        specular = m.Specular,
                        shininess = m.Shininess,
                        opacity = m.Opacity,
                        emissiveIntensity = m.EmissiveIntensity,
                        doubleSided = m.DoubleSided,
                        uvOffset = new { x = m.UVOffset.X, y = m.UVOffset.Y },
                        uvScale = new { x = m.UVScale.X, y = m.UVScale.Y },
                        uvRotate = m.UVRotate,
                        textures = m.Textures.Select(t => new { type = t.Key.ToString(), path = t.Value.FilePath }).ToArray()
                    }).ToArray()
                }).ToArray(),
                lights = Lights.Select(l => new
                {
                    name = l.Name, type = l.Type.ToString(),
                    color = l.Color.ToHex(), intensity = l.Intensity,
                    position = new { x = l.Position.X, y = l.Position.Y, z = l.Position.Z },
                    direction = new { x = l.Direction.X, y = l.Direction.Y, z = l.Direction.Z },
                    range = l.Range, spotAngle = l.SpotAngle, castShadows = l.CastShadows
                }).ToArray(),
                camera = new { pitch = Camera.Pitch, yaw = Camera.Yaw, distance = Camera.Distance, fov = Camera.FieldOfView, target = new { x = Camera.Target.X, y = Camera.Target.Y, z = Camera.Target.Z } },
                settings = new { bgColor = BackgroundColor.ToHex(), ambient = AmbientLight.ToHex(), showGrid = ShowGrid, showAxes = ShowAxes, gridSize = GridSize, gridDivisions = GridDivisions }
            };
            return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        }

        public void Deserialize(string json)
        {
            Objects.Clear(); DeselectAll();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("objects", out var objs))
                foreach (var o in objs.EnumerateArray())
                {
                    var name = o.GetProperty("name").GetString() ?? "Mesh";
                    var verts = new List<Geometry.Vertex>();
                    foreach (var v in o.GetProperty("vertices").EnumerateArray())
                    {
                        var vert = new Geometry.Vertex(new Vector3((float)v.GetProperty("x").GetDouble(), (float)v.GetProperty("y").GetDouble(), (float)v.GetProperty("z").GetDouble()));
                        if (v.TryGetProperty("nx", out var nx)) vert.Normal = new((float)nx.GetDouble(), (float)v.GetProperty("ny").GetDouble(), (float)v.GetProperty("nz").GetDouble());
                        if (v.TryGetProperty("u", out var u)) vert.UV = new((float)u.GetDouble(), (float)v.GetProperty("v").GetDouble());
                        verts.Add(vert);
                    }
                    var faces = new List<Geometry.Face>();
                    foreach (var f in o.GetProperty("faces").EnumerateArray())
                    {
                        var indices = new List<int>();
                        foreach (var i in f.EnumerateArray()) indices.Add(i.GetInt32());
                        faces.Add(new Geometry.Face(indices));
                    }
                    var mesh = new Mesh3D(name, verts, faces);
                    var pos = o.GetProperty("position");
                    mesh.Position = new((float)pos.GetProperty("x").GetDouble(), (float)pos.GetProperty("y").GetDouble(), (float)pos.GetProperty("z").GetDouble());
                    var rot = o.GetProperty("rotation");
                    mesh.Rotation = new((float)rot.GetProperty("x").GetDouble(), (float)rot.GetProperty("y").GetDouble(), (float)rot.GetProperty("z").GetDouble());
                    var scl = o.GetProperty("scale");
                    mesh.Scale = new((float)scl.GetProperty("x").GetDouble(), (float)scl.GetProperty("y").GetDouble(), (float)scl.GetProperty("z").GetDouble());
                    if (o.TryGetProperty("materials", out var mats) && mats.GetArrayLength() > 0)
                    {
                        var m = mats[0];
                        mesh.Materials[0].AlbedoColor = Color.FromHex(m.GetProperty("albedo").GetString() ?? "#4a90d9");
                        mesh.Materials[0].Metalness = (float)m.GetProperty("metalness").GetDouble();
                        mesh.Materials[0].Roughness = (float)m.GetProperty("roughness").GetDouble();
                        mesh.Materials[0].Opacity = (float)m.GetProperty("opacity").GetDouble();
                    }
                    Objects.Add(mesh);
                }

            if (root.TryGetProperty("lights", out var lights))
            {
                Lights.Clear();
                foreach (var l in lights.EnumerateArray())
                {
                    var light = new Light(l.GetProperty("name").GetString() ?? "Light");
                    light.Type = Enum.Parse<LightType>(l.GetProperty("type").GetString() ?? "Directional");
                    light.Color = Color.FromHex(l.GetProperty("color").GetString() ?? "#ffffff");
                    light.Intensity = (float)l.GetProperty("intensity").GetDouble();
                    var lp = l.GetProperty("position");
                    light.Position = new((float)lp.GetProperty("x").GetDouble(), (float)lp.GetProperty("y").GetDouble(), (float)lp.GetProperty("z").GetDouble());
                    var ld = l.GetProperty("direction");
                    light.Direction = new((float)ld.GetProperty("x").GetDouble(), (float)ld.GetProperty("y").GetDouble(), (float)ld.GetProperty("z").GetDouble());
                    light.Range = (float)l.GetProperty("range").GetDouble();
                    Lights.Add(light);
                }
            }

            if (root.TryGetProperty("camera", out var cam))
            {
                Camera.Pitch = (float)cam.GetProperty("pitch").GetDouble();
                Camera.Yaw = (float)cam.GetProperty("yaw").GetDouble();
                Camera.Distance = (float)cam.GetProperty("distance").GetDouble();
                if (cam.TryGetProperty("fov", out var fov)) Camera.FieldOfView = (float)fov.GetDouble();
                var t = cam.GetProperty("target");
                Camera.Target = new((float)t.GetProperty("x").GetDouble(), (float)t.GetProperty("y").GetDouble(), (float)t.GetProperty("z").GetDouble());
                Camera.UpdateViewMatrix();
            }

            if (root.TryGetProperty("settings", out var s))
            {
                if (s.TryGetProperty("bgColor", out var bg)) BackgroundColor = Color.FromHex(bg.GetString() ?? "#1e1e2e");
                if (s.TryGetProperty("showGrid", out var sg)) ShowGrid = sg.GetBoolean();
                if (s.TryGetProperty("showAxes", out var sa)) ShowAxes = sa.GetBoolean();
            }
        }
    }
}
