using Easy3D.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Easy3D.Geometry
{
    /// <summary>
    /// نقطة (Vertex)
    /// </summary>
    public class Vertex
    {
        public Vector3 Position { get; set; }
        public Vector3 Normal { get; set; }
        public Color Color { get; set; }
        public Vector2 UV { get; set; }
        public Vector3 Tangent { get; set; }
        public Vector3 Bitangent { get; set; }
        public Vector4 BoneWeights { get; set; }
        public Vector4 BoneIndices { get; set; }

        public Vertex(Vector3 pos) { Position = pos; Color = Core.Color.White; }
        public Vertex(Vector3 pos, Vector3 normal) : this(pos) { Normal = normal; }
        public Vertex(Vector3 pos, Vector3 normal, Color color) : this(pos, normal) { Color = color; }
        public Vertex(Vector3 pos, Vector3 normal, Color color, Vector2 uv) : this(pos, normal, color) { UV = uv; }

        public Vertex Clone() => new(Position, Normal, Color, UV) { Tangent = Tangent, Bitangent = Bitangent };
    }

    /// <summary>
    /// وجه (Face)
    /// </summary>
    public class Face
    {
        public List<int> Indices { get; set; }
        public Vector3 Normal { get; set; }
        public int MaterialIndex { get; set; }

        public Face(params int[] indices) { Indices = new List<int>(indices); }
        public Face(List<int> indices) { Indices = new List<int>(indices); }

        public int Count => Indices.Count;
        public bool IsTriangle => Count == 3;
        public bool IsQuad => Count == 4;

        public void CalculateNormal(List<Vertex> vertices)
        {
            if (Count < 3) { Normal = Vector3.Zero; return; }
            var v0 = vertices[Indices[0]].Position;
            var v1 = vertices[Indices[1]].Position;
            var v2 = vertices[Indices[2]].Position;
            Normal = (v1 - v0).Cross(v2 - v0).Normalized();
        }

        public Vector3 GetCenter(List<Vertex> vertices)
        {
            var center = Vector3.Zero;
            foreach (int i in Indices)
                if (i < vertices.Count) center += vertices[i].Position;
            return center / Count;
        }

        public List<int[]> Triangulate()
        {
            var result = new List<int[]>();
            if (Count < 3) return result;
            if (Count == 3) { result.Add([Indices[0], Indices[1], Indices[2]]); return result; }
            for (int i = 1; i < Count - 1; i++)
                result.Add([Indices[0], Indices[i], Indices[i + 1]]);
            return result;
        }

        public Face Clone() => new(new List<int>(Indices)) { Normal = Normal, MaterialIndex = MaterialIndex };
    }

    /// <summary>
    /// Bounding Box
    /// </summary>
    public struct BoundingBox
    {
        public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }
        public Vector3 Center => (Min + Max) / 2;
        public Vector3 Size => Max - Min;
        public Vector3 Extents => Size / 2;

        public BoundingBox(Vector3 min, Vector3 max) { Min = min; Max = max; }

        public static BoundingBox FromVertices(List<Vertex> vertices)
        {
            if (vertices.Count == 0) return new(Vector3.Zero, Vector3.Zero);
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);
            foreach (var v in vertices) { min = min.Min(v.Position); max = max.Max(v.Position); }
            return new(min, max);
        }

        public bool Contains(Vector3 point) => point.X >= Min.X && point.X <= Max.X && point.Y >= Min.Y && point.Y <= Max.Y && point.Z >= Min.Z && point.Z <= Max.Z;

        public bool IntersectsRay(Vector3 origin, Vector3 direction, out float distance)
        {
            distance = 0;
            float tmin = float.MinValue, tmax = float.MaxValue;
            if (MathF.Abs(direction.X) < 1e-6f) { if (origin.X < Min.X || origin.X > Max.X) return false; }
            else { float t1 = (Min.X - origin.X) / direction.X; float t2 = (Max.X - origin.X) / direction.X; tmin = Math.Max(tmin, Math.Min(t1, t2)); tmax = Math.Min(tmax, Math.Max(t1, t2)); }
            if (MathF.Abs(direction.Y) < 1e-6f) { if (origin.Y < Min.Y || origin.Y > Max.Y) return false; }
            else { float t1 = (Min.Y - origin.Y) / direction.Y; float t2 = (Max.Y - origin.Y) / direction.Y; tmin = Math.Max(tmin, Math.Min(t1, t2)); tmax = Math.Min(tmax, Math.Max(t1, t2)); }
            if (MathF.Abs(direction.Z) < 1e-6f) { if (origin.Z < Min.Z || origin.Z > Max.Z) return false; }
            else { float t1 = (Min.Z - origin.Z) / direction.Z; float t2 = (Max.Z - origin.Z) / direction.Z; tmin = Math.Max(tmin, Math.Min(t1, t2)); tmax = Math.Min(tmax, Math.Max(t1, t2)); }
            if (tmax < 0 || tmin > tmax) return false;
            distance = tmin >= 0 ? tmin : tmax;
            return true;
        }
    }

    /// <summary>
    /// شبكة ثلاثية الأبعاد
    /// </summary>
    public class Mesh : IDisposable
    {
        public string Name { get; set; }
        public List<Vertex> Vertices { get; set; }
        public List<Face> Faces { get; set; }
        public List<Material> Materials { get; set; }
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Vector3 Rotation { get; set; } = Vector3.Zero;
        public Vector3 Scale { get; set; } = Vector3.One;
        public bool Visible { get; set; } = true;
        public bool Selected { get; set; } = false;
        public bool CastShadows { get; set; } = true;
        public bool ReceiveShadows { get; set; } = true;
        public BoundingBox Bounds { get; private set; }
        public string? ParentName { get; set; }
        public List<string> Children { get; set; } = new();
        public Dictionary<string, object> CustomData { get; set; } = new();

        public Mesh(string name = "Mesh") { Name = name; Vertices = new(); Faces = new(); Materials = new() { Material.Default }; UpdateBounds(); }
        public Mesh(string name, List<Vertex> vertices, List<Face> faces) : this(name) { Vertices = vertices; Faces = faces; UpdateBounds(); CalculateNormals(); }

        public void CalculateNormals()
        {
            foreach (var v in Vertices) v.Normal = Vector3.Zero;
            foreach (var face in Faces)
            {
                face.CalculateNormal(Vertices);
                foreach (int i in face.Indices)
                    if (i < Vertices.Count) Vertices[i].Normal += face.Normal;
            }
            foreach (var v in Vertices)
                if (v.Normal.Length > 0) v.Normal = v.Normal.Normalized();
        }

        public void CalculateTangents()
        {
            if (Vertices.Count == 0 || Faces.Count == 0) return;
            var tangents = new Vector3[Vertices.Count];
            var bitangents = new Vector3[Vertices.Count];

            foreach (var face in Faces)
            {
                if (face.Count < 3) continue;
                var triangles = face.Triangulate();
                foreach (var tri in triangles)
                {
                    var v0 = Vertices[tri[0]]; var v1 = Vertices[tri[1]]; var v2 = Vertices[tri[2]];
                    var e1 = v1.Position - v0.Position; var e2 = v2.Position - v0.Position;
                    var duv1 = v1.UV - v0.UV; var duv2 = v2.UV - v0.UV;
                    float f = duv1.X * duv2.Y - duv2.X * duv1.Y;
                    if (MathF.Abs(f) < 1e-6f) continue;
                    float inv = 1f / f;
                    var t = (e1 * duv2.Y - e2 * duv1.Y) * inv;
                    var b = (e2 * duv1.X - e1 * duv2.X) * inv;
                    foreach (int i in tri) { tangents[i] += t; bitangents[i] += b; }
                }
            }

            for (int i = 0; i < Vertices.Count; i++)
            {
                var n = Vertices[i].Normal;
                var t = tangents[i];
                if (t.Length < 1e-6f) t = Vector3.UnitX;
                Vertices[i].Tangent = (t - n * n.Dot(t)).Normalized();
                Vertices[i].Bitangent = n.Cross(Vertices[i].Tangent).Normalized();
            }
        }

        public void UpdateBounds() => Bounds = BoundingBox.FromVertices(Vertices);

        public Matrix4x4 GetTransformMatrix() => Matrix4x4.Translation(Position) * Matrix4x4.Rotation(Rotation) * Matrix4x4.Scale(Scale);

        public List<Vector3> GetWorldVertices()
        {
            var m = GetTransformMatrix();
            return Vertices.Select(v => m.TransformPoint(v.Position)).ToList();
        }

        public void ApplyTransform()
        {
            var m = GetTransformMatrix();
            var rm = Matrix4x4.Rotation(Rotation);
            foreach (var v in Vertices) { v.Position = m.TransformPoint(v.Position); v.Normal = rm.TransformDirection(v.Normal).Normalized(); }
            Position = Vector3.Zero; Rotation = Vector3.Zero; Scale = Vector3.One;
            UpdateBounds();
        }

        public void MergeVertices(float threshold = 0.001f)
        {
            var map = new Dictionary<int, int>();
            var newVerts = new List<Vertex>();
            for (int i = 0; i < Vertices.Count; i++)
            {
                int merged = -1;
                for (int j = 0; j < newVerts.Count; j++)
                    if (Vertices[i].Position.DistanceTo(newVerts[j].Position) < threshold) { merged = j; break; }
                map[i] = merged >= 0 ? merged : newVerts.Count;
                if (merged < 0) newVerts.Add(Vertices[i].Clone());
            }
            foreach (var f in Faces) f.Indices = f.Indices.Select(i => map[i]).ToList();
            Vertices = newVerts; UpdateBounds(); CalculateNormals();
        }

        public void FlipNormals()
        {
            foreach (var f in Faces) { f.Indices.Reverse(); f.Normal = -f.Normal; }
            foreach (var v in Vertices) v.Normal = -v.Normal;
        }

        public void Subdivide()
        {
            var newFaces = new List<Face>();
            var cache = new Dictionary<(int, int), int>();
            int Mid(int a, int b) { var k = (Math.Min(a, b), Math.Max(a, b)); if (cache.TryGetValue(k, out int c)) return c; int ni = Vertices.Count; Vertices.Add(new Vertex((Vertices[a].Position + Vertices[b].Position) / 2)); cache[k] = ni; return ni; }
            foreach (var face in Faces)
            {
                if (face.IsTriangle) { int a = face.Indices[0], b = face.Indices[1], c = face.Indices[2]; int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a); newFaces.Add(new(a, ab, ca)); newFaces.Add(new(b, bc, ab)); newFaces.Add(new(c, ca, bc)); newFaces.Add(new(ab, bc, ca)); }
                else if (face.IsQuad) { int a = face.Indices[0], b = face.Indices[1], c = face.Indices[2], d = face.Indices[3]; int ab = Mid(a, b), bc = Mid(b, c), cd = Mid(c, d), da = Mid(d, a); newFaces.Add(new(a, ab, Mid(a, c), da)); newFaces.Add(new(b, bc, Mid(a, c), ab)); newFaces.Add(new(c, cd, Mid(a, c), bc)); newFaces.Add(new(d, da, Mid(a, c), cd)); }
            }
            Faces = newFaces; UpdateBounds(); CalculateNormals();
        }

        public void Decimate(float ratio)
        {
            int targetCount = Math.Max(4, (int)(Faces.Count * Math.Clamp(ratio, 0.01f, 1f)));
            while (Faces.Count > targetCount && Faces.Count > 4)
            {
                // Simple edge collapse
                float minLen = float.MaxValue; int minFace = 0;
                for (int i = 0; i < Faces.Count; i++)
                {
                    if (Faces[i].Count < 3) continue;
                    var c = Faces[i].GetCenter(Vertices);
                    var v0 = Vertices[Faces[i].Indices[0]].Position;
                    float len = (v0 - c).LengthSquared;
                    if (len < minLen) { minLen = len; minFace = i; }
                }
                if (minFace < Faces.Count) Faces.RemoveAt(minFace);
            }
            UpdateBounds(); CalculateNormals();
        }

        public Mesh Clone()
        {
            var c = new Mesh(Name + " (Copy)")
            {
                Position = Position, Rotation = Rotation, Scale = Scale,
                Visible = Visible, CastShadows = CastShadows, ReceiveShadows = ReceiveShadows
            };
            c.Vertices = Vertices.Select(v => v.Clone()).ToList();
            c.Faces = Faces.Select(f => f.Clone()).ToList();
            c.Materials = Materials.Select(m => m.Clone()).ToList();
            c.UpdateBounds();
            return c;
        }

        public void Dispose()
        {
            foreach (var m in Materials) m.Dispose();
            Vertices.Clear(); Faces.Clear(); Materials.Clear();
        }
    }
}
