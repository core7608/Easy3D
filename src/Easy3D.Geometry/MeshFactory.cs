using Easy3D.Core;
using System;
using System.Collections.Generic;

namespace Easy3D.Geometry
{
    /// <summary>
    /// مصنع الأشكال الهندسية
    /// </summary>
    public static class MeshFactory
    {
        public static Mesh CreateCube(float size = 1f, string name = "Cube")
        {
            float s = size / 2;
            var v = new List<Vertex>
            {
                new(new(-s,-s,-s)), new(new(s,-s,-s)), new(new(s,s,-s)), new(new(-s,s,-s)),
                new(new(-s,-s, s)), new(new(s,-s, s)), new(new(s,s, s)), new(new(-s,s, s)),
                new(new(-s, s,-s)), new(new(s, s,-s)), new(new(s, s, s)), new(new(-s, s, s)),
                new(new(-s,-s,-s)), new(new(s,-s,-s)), new(new(s,-s, s)), new(new(-s,-s, s)),
                new(new(s,-s,-s)), new(new(s,-s, s)), new(new(s, s, s)), new(new(s, s,-s)),
                new(new(-s,-s,-s)), new(new(-s,-s, s)), new(new(-s, s, s)), new(new(-s, s,-s)),
            };
            var f = new List<Face> { new(0,1,2,3), new(5,4,7,6), new(8,9,10,11), new(13,12,15,14), new(16,17,18,19), new(21,20,23,22) };
            return new Mesh(name, v, f);
        }

        public static Mesh CreateSphere(float radius = 0.5f, int segments = 16, int rings = 12, string name = "Sphere")
        {
            var v = new List<Vertex>(); var f = new List<Face>();
            for (int ring = 0; ring <= rings; ring++)
            {
                float phi = MathF.PI * ring / rings, cp = MathF.Cos(phi), sp = MathF.Sin(phi);
                for (int seg = 0; seg <= segments; seg++)
                {
                    float theta = 2 * MathF.PI * seg / segments, ct = MathF.Cos(theta), st = MathF.Sin(theta);
                    float x = radius * sp * ct, y = radius * cp, z = radius * sp * st;
                    v.Add(new Vertex(new(x, y, z), new(x, y, z).Normalized()) { UV = new((float)seg / segments, (float)ring / rings) });
                }
            }
            for (int ring = 0; ring < rings; ring++)
                for (int seg = 0; seg < segments; seg++)
                { int a = ring * (segments + 1) + seg, b = a + segments + 1; f.Add(new(a, b, b + 1, a + 1)); }
            return new Mesh(name, v, f);
        }

        public static Mesh CreateCylinder(float radius = 0.5f, float height = 1f, int segments = 16, string name = "Cylinder")
        {
            var v = new List<Vertex>(); var f = new List<Face>();
            float h = height / 2;
            v.Add(new Vertex(new(0, h, 0), Vector3.Up)); v.Add(new Vertex(new(0, -h, 0), Vector3.Down));
            for (int i = 0; i <= segments; i++)
            {
                float a = 2 * MathF.PI * i / segments, x = radius * MathF.Cos(a), z = radius * MathF.Sin(a);
                Vector3 n = new(x, 0, z).Normalized();
                v.Add(new Vertex(new(x, h, z), n)); v.Add(new Vertex(new(x, -h, z), n));
            }
            var top = new List<int>(); var bot = new List<int>();
            for (int i = 0; i <= segments; i++) { top.Add(2 + i * 2); bot.Add(3 + i * 2); }
            f.Add(new Face(top)); f.Add(new Face(bot));
            for (int i = 0; i < segments; i++)
            { int t = 2 + i * 2, b = 3 + i * 2, t2 = 2 + (i + 1) * 2, b2 = 3 + (i + 1) * 2; f.Add(new(t, b, b2, t2)); }
            return new Mesh(name, v, f);
        }

        public static Mesh CreateCone(float radius = 0.5f, float height = 1f, int segments = 16, string name = "Cone")
        {
            var v = new List<Vertex>(); var f = new List<Face>();
            float h = height / 2;
            v.Add(new Vertex(new(0, h, 0), Vector3.Up)); v.Add(new Vertex(new(0, -h, 0), Vector3.Down));
            for (int i = 0; i <= segments; i++)
            {
                float a = 2 * MathF.PI * i / segments, x = radius * MathF.Cos(a), z = radius * MathF.Sin(a);
                v.Add(new Vertex(new(x, -h, z), new(x, 0, z).Normalized()));
            }
            for (int i = 0; i < segments; i++) f.Add(new(0, 2 + i, 3 + i));
            var bot = new List<int>(); for (int i = segments; i >= 0; i--) bot.Add(2 + i); f.Add(new Face(bot));
            return new Mesh(name, v, f);
        }

        public static Mesh CreatePlane(float width = 1f, float depth = 1f, int sub = 1, string name = "Plane")
        {
            var v = new List<Vertex>(); var f = new List<Face>();
            float hw = width / 2, hd = depth / 2; int vc = sub + 1;
            for (int z = 0; z <= sub; z++)
                for (int x = 0; x <= sub; x++)
                    v.Add(new Vertex(new(-hw + width * x / sub, 0, -hd + depth * z / sub), Vector3.Up) { UV = new((float)x / sub, (float)z / sub) });
            for (int z = 0; z < sub; z++)
                for (int x = 0; x < sub; x++)
                { int a = z * vc + x; f.Add(new(a, a + 1, a + vc + 1, a + vc)); }
            return new Mesh(name, v, f);
        }

        public static Mesh CreateTorus(float major = 0.5f, float minor = 0.15f, int majorSeg = 24, int minorSeg = 12, string name = "Torus")
        {
            var v = new List<Vertex>(); var f = new List<Face>();
            for (int i = 0; i <= majorSeg; i++)
            {
                float theta = 2 * MathF.PI * i / majorSeg, ct = MathF.Cos(theta), st = MathF.Sin(theta);
                for (int j = 0; j <= minorSeg; j++)
                {
                    float phi = 2 * MathF.PI * j / minorSeg, cp = MathF.Cos(phi), sp = MathF.Sin(phi);
                    float x = (major + minor * cp) * ct, y = minor * sp, z = (major + minor * cp) * st;
                    Vector3 n = (new Vector3(x, y, z) - new Vector3(major * ct, 0, major * st)).Normalized();
                    v.Add(new Vertex(new(x, y, z), n) { UV = new((float)i / majorSeg, (float)j / minorSeg) });
                }
            }
            for (int i = 0; i < majorSeg; i++)
                for (int j = 0; j < minorSeg; j++)
                { int a = i * (minorSeg + 1) + j, b = a + minorSeg + 1; f.Add(new(a, b, b + 1, a + 1)); }
            return new Mesh(name, v, f);
        }

        public static Mesh CreateTube(float outer = 0.5f, float inner = 0.3f, float height = 1f, int seg = 16, string name = "Tube")
        {
            var v = new List<Vertex>(); var f = new List<Face>();
            float h = height / 2;
            for (int i = 0; i <= seg; i++)
            {
                float a = 2 * MathF.PI * i / seg, c = MathF.Cos(a), s = MathF.Sin(a);
                v.Add(new Vertex(new(outer * c, h, outer * s)));
                v.Add(new Vertex(new(outer * c, -h, outer * s)));
                v.Add(new Vertex(new(inner * c, h, inner * s)));
                v.Add(new Vertex(new(inner * c, -h, inner * s)));
            }
            for (int i = 0; i < seg; i++)
            {
                int c = i * 4, n = (i + 1) * 4;
                f.Add(new(c, c + 1, n + 1, n));
                f.Add(new(c + 2, n + 2, n + 3, c + 3));
                f.Add(new(c, n, n + 2, c + 2));
                f.Add(new(c + 1, c + 3, n + 3, n + 1));
            }
            return new Mesh(name, v, f);
        }

        public static Mesh CreateStar(float outer = 0.5f, float inner = 0.2f, int points = 5, string name = "Star")
        {
            var v = new List<Vertex> { new Vertex(Vector3.Zero, Vector3.Up) };
            var f = new List<Face>();
            for (int i = 0; i < points * 2; i++)
            {
                float a = MathF.PI * i / points - MathF.PI / 2;
                float r = i % 2 == 0 ? outer : inner;
                v.Add(new Vertex(new(r * MathF.Cos(a), 0, r * MathF.Sin(a)), Vector3.Up));
            }
            for (int i = 1; i <= points * 2; i++) f.Add(new(0, i, i + 1 > points * 2 ? 1 : i + 1));
            return new Mesh(name, v, f);
        }

        public static Mesh CreateArrow(float length = 1f, float headSize = 0.1f, string name = "Arrow")
        {
            var v = new List<Vertex> { new(Vector3.Zero), new(new(0, length, 0)), new(new(-headSize, length - headSize * 2, 0)), new(new(headSize, length - headSize * 2, 0)), new(new(0, length - headSize * 2, -headSize)), new(new(0, length - headSize * 2, headSize)), new(new(0, length + headSize, 0)) };
            var f = new List<Face> { new(2, 6, 3), new(4, 6, 5), new(5, 6, 2), new(3, 6, 4) };
            return new Mesh(name, v, f);
        }

        public static Mesh CreateIcosphere(float radius = 0.5f, int subdivisions = 2, string name = "Icosphere")
        {
            float t = (1 + MathF.Sqrt(5)) / 2;
            var v = new List<Vertex>
            {
                new(new(-1, t, 0).Normalized() * radius), new(new(1, t, 0).Normalized() * radius),
                new(new(-1, -t, 0).Normalized() * radius), new(new(1, -t, 0).Normalized() * radius),
                new(new(0, -1, t).Normalized() * radius), new(new(0, 1, t).Normalized() * radius),
                new(new(0, -1, -t).Normalized() * radius), new(new(0, 1, -t).Normalized() * radius),
                new(new(t, 0, -1).Normalized() * radius), new(new(t, 0, 1).Normalized() * radius),
                new(new(-t, 0, -1).Normalized() * radius), new(new(-t, 0, 1).Normalized() * radius),
            };
            var f = new List<Face>
            {
                new(0,11,5), new(0,5,1), new(0,1,7), new(0,7,10), new(0,10,11),
                new(1,5,9), new(5,11,4), new(11,10,2), new(10,7,6), new(7,1,8),
                new(3,9,4), new(3,4,2), new(3,2,6), new(3,6,8), new(3,8,9),
                new(4,9,5), new(2,4,11), new(6,2,10), new(8,6,7), new(9,8,1)
            };
            var mesh = new Mesh(name, v, f);
            for (int i = 0; i < subdivisions; i++) mesh.Subdivide();
            foreach (var vert in mesh.Vertices) vert.Position = vert.Position.Normalized() * radius;
            mesh.CalculateNormals();
            return mesh;
        }

        public static Mesh CreateSuzanne(string name = "Suzanne") => CreateSphere(0.5f, 16, 12, name); // Placeholder

        public static Mesh CreateFromVertices(string name, Vector3[] positions, int[]? indices = null)
        {
            var verts = new List<Vertex>();
            foreach (var p in positions) verts.Add(new Vertex(p));
            var faces = new List<Face>();
            if (indices != null)
            {
                for (int i = 0; i < indices.Length; i += 3)
                    faces.Add(new Face(indices[i], indices[i + 1], indices[i + 2]));
            }
            return new Mesh(name, verts, faces);
        }
    }
}
