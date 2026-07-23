using Easy3D.Core;
using Easy3D.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Easy3D.Exporters
{
    /// <summary>
    /// صيغ التصدير المدعومة
    /// </summary>
    public enum ExportFormat
    {
        OBJ,        // Wavefront OBJ
        STL,        // Stereolithography
        GLTF,       // GL Transmission Format
        GLB,        // GL Binary
        FBX,        // Autodesk FBX (ASCII)
        PLY,        // Stanford PLY
        ThreeJS,    // Three.js JSON
        USD,        // Universal Scene Description
        COLLADA,    // COLLADA DAE
        X3D,        // X3D
        VRML,       // VRML
        OFF,        // Object File Format
        AMF,        // Additive Manufacturing Format
        JSON,       // Easy3D JSON
        CSV         // CSV Points
    }

    /// <summary>
    /// مدير التصدير
    /// </summary>
    public static class ExportManager
    {
        public static readonly Dictionary<ExportFormat, string> Extensions = new()
        {
            { ExportFormat.OBJ, ".obj" },
            { ExportFormat.STL, ".stl" },
            { ExportFormat.GLTF, ".gltf" },
            { ExportFormat.GLB, ".glb" },
            { ExportFormat.FBX, ".fbx" },
            { ExportFormat.PLY, ".ply" },
            { ExportFormat.ThreeJS, ".json" },
            { ExportFormat.USD, ".usd" },
            { ExportFormat.COLLADA, ".dae" },
            { ExportFormat.X3D, ".x3d" },
            { ExportFormat.VRML, ".wrl" },
            { ExportFormat.OFF, ".off" },
            { ExportFormat.AMF, ".amf" },
            { ExportFormat.JSON, ".e3d" },
            { ExportFormat.CSV, ".csv" }
        };

        public static readonly Dictionary<ExportFormat, string> Descriptions = new()
        {
            { ExportFormat.OBJ, "Wavefront OBJ (*.obj)" },
            { ExportFormat.STL, "Stereolithography (*.stl)" },
            { ExportFormat.GLTF, "GL Transmission Format (*.gltf)" },
            { ExportFormat.GLB, "GL Binary (*.glb)" },
            { ExportFormat.FBX, "Autodesk FBX (*.fbx)" },
            { ExportFormat.PLY, "Stanford PLY (*.ply)" },
            { ExportFormat.ThreeJS, "Three.js JSON (*.json)" },
            { ExportFormat.USD, "Universal Scene Description (*.usd)" },
            { ExportFormat.COLLADA, "COLLADA (*.dae)" },
            { ExportFormat.X3D, "X3D (*.x3d)" },
            { ExportFormat.VRML, "VRML (*.wrl)" },
            { ExportFormat.OFF, "Object File Format (*.off)" },
            { ExportFormat.AMF, "Additive Manufacturing (*.amf)" },
            { ExportFormat.JSON, "Easy3D Project (*.e3d)" },
            { ExportFormat.CSV, "CSV Points (*.csv)" }
        };

        /// <summary>
        /// تصدير المشهد
        /// </summary>
        public static void Export(Scene scene, string filePath, ExportFormat format)
        {
            string content = format switch
            {
                ExportFormat.OBJ => ExportOBJ(scene),
                ExportFormat.STL => ExportSTL(scene),
                ExportFormat.GLTF => ExportGLTF(scene, false),
                ExportFormat.GLB => ExportGLTF(scene, true),
                ExportFormat.FBX => ExportFBX(scene),
                ExportFormat.PLY => ExportPLY(scene),
                ExportFormat.ThreeJS => ExportThreeJS(scene),
                ExportFormat.USD => ExportUSD(scene),
                ExportFormat.COLLADA => ExportCOLLADA(scene),
                ExportFormat.X3D => ExportX3D(scene),
                ExportFormat.VRML => ExportVRML(scene),
                ExportFormat.OFF => ExportOFF(scene),
                ExportFormat.AMF => ExportAMF(scene),
                ExportFormat.JSON => scene.Serialize(),
                ExportFormat.CSV => ExportCSV(scene),
                _ => throw new NotSupportedException($"Format {format} not supported")
            };

            File.WriteAllText(filePath, content, Encoding.UTF8);
        }

        /// <summary>
        /// استيراد ملف
        /// </summary>
        public static Scene Import(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            return ext switch
            {
                ".obj" => ImportOBJ(filePath),
                ".stl" => ImportSTL(filePath),
                ".gltf" or ".glb" => ImportGLTF(filePath),
                ".ply" => ImportPLY(filePath),
                ".e3d" => ImportJSON(filePath),
                _ => throw new NotSupportedException($"Format {ext} not supported for import")
            };
        }

        public static ExportFormat GetFormatFromExtension(string ext) => ext.ToLower() switch
        {
            ".obj" => ExportFormat.OBJ,
            ".stl" => ExportFormat.STL,
            ".gltf" => ExportFormat.GLTF,
            ".glb" => ExportFormat.GLB,
            ".fbx" => ExportFormat.FBX,
            ".ply" => ExportFormat.PLY,
            ".json" => ExportFormat.ThreeJS,
            ".usd" => ExportFormat.USD,
            ".dae" => ExportFormat.COLLADA,
            ".x3d" => ExportFormat.X3D,
            ".wrl" => ExportFormat.VRML,
            ".off" => ExportFormat.OFF,
            ".amf" => ExportFormat.AMF,
            ".e3d" => ExportFormat.JSON,
            ".csv" => ExportFormat.CSV,
            _ => ExportFormat.OBJ
        };

        // ==================== OBJ ====================
        
        public static string ExportOBJ(Scene scene)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Easy3D OBJ Export");
            sb.AppendLine($"# Objects: {scene.Objects.Count}");
            sb.AppendLine($"# Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            int vOff = 0, vtOff = 0, vnOff = 0;
            foreach (var obj in scene.Objects)
            {
                if (!obj.Visible) continue;
                sb.AppendLine($"# {obj.Name}");
                sb.AppendLine($"g {obj.Name}");
                sb.AppendLine($"o {obj.Name}");

                var transform = obj.GetTransformMatrix();
                var rotMatrix = Matrix4x4.Rotation(obj.Rotation);

                foreach (var v in obj.Vertices)
                {
                    var p = transform.TransformPoint(v.Position);
                    sb.AppendLine($"v {p.X:F6} {p.Y:F6} {p.Z:F6}");
                }
                foreach (var v in obj.Vertices)
                    sb.AppendLine($"vt {v.UV.X:F6} {v.UV.Y:F6}");
                foreach (var v in obj.Vertices)
                {
                    var n = rotMatrix.TransformDirection(v.Normal).Normalized();
                    sb.AppendLine($"vn {n.X:F6} {n.Y:F6} {n.Z:F6}");
                }

                foreach (var face in obj.Faces)
                {
                    sb.Append("f");
                    foreach (int idx in face.Indices)
                    {
                        int v = idx + vOff + 1;
                        sb.Append($" {v}/{idx + vtOff + 1}/{idx + vnOff + 1}");
                    }
                    sb.AppendLine();
                }

                vOff += obj.Vertices.Count;
                vtOff += obj.Vertices.Count;
                vnOff += obj.Vertices.Count;
                sb.AppendLine();
            }
            return sb.ToString();
        }

        // ==================== STL ====================
        
        public static string ExportSTL(Scene scene)
        {
            var sb = new StringBuilder();
            sb.AppendLine("solid Easy3D");
            foreach (var obj in scene.Objects)
            {
                if (!obj.Visible) continue;
                var transform = obj.GetTransformMatrix();
                foreach (var face in obj.Faces)
                {
                    var triangles = face.Triangulate();
                    foreach (var tri in triangles)
                    {
                        var v0 = transform.TransformPoint(obj.Vertices[tri[0]].Position);
                        var v1 = transform.TransformPoint(obj.Vertices[tri[1]].Position);
                        var v2 = transform.TransformPoint(obj.Vertices[tri[2]].Position);
                        var n = (v1 - v0).Cross(v2 - v0).Normalized();
                        sb.AppendLine($"  facet normal {n.X:F6} {n.Y:F6} {n.Z:F6}");
                        sb.AppendLine("    outer loop");
                        sb.AppendLine($"      vertex {v0.X:F6} {v0.Y:F6} {v0.Z:F6}");
                        sb.AppendLine($"      vertex {v1.X:F6} {v1.Y:F6} {v1.Z:F6}");
                        sb.AppendLine($"      vertex {v2.X:F6} {v2.Y:F6} {v2.Z:F6}");
                        sb.AppendLine("    endloop");
                        sb.AppendLine("  endfacet");
                    }
                }
            }
            sb.AppendLine("endsolid Easy3D");
            return sb.ToString();
        }

        // ==================== Three.js JSON ====================
        
        public static string ExportThreeJS(Scene scene)
        {
            var meshes = new List<object>();
            foreach (var obj in scene.Objects)
            {
                if (!obj.Visible) continue;
                var transform = obj.GetTransformMatrix();
                var rotMatrix = Matrix4x4.Rotation(obj.Rotation);

                var vertices = new List<float>();
                var normals = new List<float>();
                var uvs = new List<float>();
                var indices = new List<int>();

                foreach (var v in obj.Vertices)
                {
                    var p = transform.TransformPoint(v.Position);
                    vertices.AddRange(new[] { p.X, p.Y, p.Z });
                    var n = rotMatrix.TransformDirection(v.Normal).Normalized();
                    normals.AddRange(new[] { n.X, n.Y, n.Z });
                    uvs.AddRange(new[] { v.UV.X, v.UV.Y });
                }

                foreach (var face in obj.Faces)
                {
                    var tris = face.Triangulate();
                    foreach (var tri in tris)
                        indices.AddRange(tri);
                }

                var mat = obj.Materials.FirstOrDefault();
                meshes.Add(new
                {
                    type = "Mesh",
                    name = obj.Name,
                    geometry = new
                    {
                        type = "BufferGeometry",
                        data = new
                        {
                            attributes = new
                            {
                                position = new { itemSize = 3, type = "Float32Array", array = vertices.ToArray() },
                                normal = new { itemSize = 3, type = "Float32Array", array = normals.ToArray() },
                                uv = new { itemSize = 2, type = "Float32Array", array = uvs.ToArray() }
                            },
                            index = new { type = "Uint32Array", array = indices.ToArray() }
                        }
                    },
                    material = new
                    {
                        type = mat?.ShadingModel == ShadingModel.PBR ? "MeshStandardMaterial" : "MeshPhongMaterial",
                        color = mat?.AlbedoColor.ToHex() ?? "#4a90d9",
                        emissive = mat?.EmissiveColor.ToHex() ?? "#000000",
                        metalness = mat?.Metalness ?? 0,
                        roughness = mat?.Roughness ?? 0.5,
                        opacity = mat?.Opacity ?? 1,
                        transparent = mat?.Transparent ?? false,
                        side = mat?.DoubleSided == true ? "DoubleSide" : "FrontSide"
                    }
                });
            }

            var result = new
            {
                metadata = new
                {
                    version = 4.5,
                    type = "Object",
                    generator = "Easy3D Exporter",
                    date = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
                },
                geometries = meshes.Select(m => ((dynamic)m).geometry).ToArray(),
                materials = meshes.Select(m => ((dynamic)m).material).ToArray(),
                object3d = new
                {
                    type = "Scene",
                    children = meshes.ToArray()
                }
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }

        // ==================== GLTF ====================
        
        public static string ExportGLTF(Scene scene, bool binary)
        {
            var gltf = new
            {
                asset = new { version = "2.0", generator = "Easy3D" },
                scene = 0,
                scenes = new[] { new { name = "Scene", nodes = Enumerable.Range(0, scene.Objects.Count).ToArray() } },
                nodes = scene.Objects.Select((obj, i) => new
                {
                    name = obj.Name,
                    mesh = i,
                    translation = new[] { obj.Position.X, obj.Position.Y, obj.Position.Z },
                    rotation = Quaternion.FromEuler(obj.Rotation),
                    scale = new[] { obj.Scale.X, obj.Scale.Y, obj.Scale.Z }
                }).ToArray(),
                meshes = scene.Objects.Select(obj => new
                {
                    name = obj.Name,
                    primitives = new[] { new { attributes = new { POSITION = 0, NORMAL = 1, TEXCOORD_0 = 2 }, indices = 3, material = 0 } }
                }).ToArray(),
                materials = scene.Objects.Select(obj =>
                {
                    var m = obj.Materials.FirstOrDefault();
                    return new
                    {
                        name = m?.Name ?? "Material",
                        pbrMetallicRoughness = new
                        {
                            baseColorFactor = new[] { m?.AlbedoColor.Rf ?? 0.3f, m?.AlbedoColor.Gf ?? 0.6f, m?.AlbedoColor.Bf ?? 0.85f, m?.Opacity ?? 1f },
                            metallicFactor = m?.Metalness ?? 0,
                            roughnessFactor = m?.Roughness ?? 0.5f
                        },
                        emissiveFactor = new[] { m?.EmissiveColor.Rf ?? 0, m?.EmissiveColor.Gf ?? 0, m?.EmissiveColor.Bf ?? 0 },
                        doubleSided = m?.DoubleSided ?? false
                    };
                }).ToArray()
            };

            return JsonSerializer.Serialize(gltf, new JsonSerializerOptions { WriteIndented = true });
        }

        // ==================== FBX (ASCII) ====================
        
        public static string ExportFBX(Scene scene)
        {
            var sb = new StringBuilder();
            sb.AppendLine("; FBX 7.4.0 project file");
            sb.AppendLine("; Easy3D Export");
            sb.AppendLine("FBXHeaderExtension:  {");
            sb.AppendLine("  FBXHeaderVersion: 1003");
            sb.AppendLine("  FBXVersion: 7400");
            sb.AppendLine($"  CreationTimeStamp:  {{");
            sb.AppendLine($"    DateTime: \"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}\"");
            sb.AppendLine("  }");
            sb.AppendLine("}");
            sb.AppendLine("Objects:  {");
            int id = 1;
            foreach (var obj in scene.Objects)
            {
                sb.AppendLine($"  Model: {id}, \"Model::{obj.Name}\", \"Mesh\" {{");
                sb.AppendLine($"    Version: 232");
                sb.AppendLine($"    Properties70:  {{");
                sb.AppendLine($"      P: \"Lcl Translation\", \"Lcl Translation\", \"\", \"A\",{obj.Position.X},{obj.Position.Y},{obj.Position.Z}");
                sb.AppendLine($"      P: \"Lcl Rotation\", \"Lcl Rotation\", \"\", \"A\",{obj.Rotation.X * 180 / MathF.PI},{obj.Rotation.Y * 180 / MathF.PI},{obj.Rotation.Z * 180 / MathF.PI}");
                sb.AppendLine($"      P: \"Lcl Scaling\", \"Lcl Scaling\", \"\", \"A\",{obj.Scale.X},{obj.Scale.Y},{obj.Scale.Z}");
                sb.AppendLine("    }");
                sb.AppendLine("  }");
                id++;
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

        // ==================== PLY ====================
        
        public static string ExportPLY(Scene scene)
        {
            int totalVerts = scene.Objects.Sum(o => o.Vertices.Count);
            int totalFaces = scene.Objects.Sum(o => o.Faces.Sum(f => f.Triangulate().Count));
            var sb = new StringBuilder();
            sb.AppendLine("ply");
            sb.AppendLine("format ascii 1.0");
            sb.AppendLine($"element vertex {totalVerts}");
            sb.AppendLine("property float x"); sb.AppendLine("property float y"); sb.AppendLine("property float z");
            sb.AppendLine("property float nx"); sb.AppendLine("property float ny"); sb.AppendLine("property float nz");
            sb.AppendLine("property uchar red"); sb.AppendLine("property uchar green"); sb.AppendLine("property uchar blue");
            sb.AppendLine($"element face {totalFaces}");
            sb.AppendLine("property list uchar int vertex_indices");
            sb.AppendLine("end_header");

            foreach (var obj in scene.Objects)
            {
                var t = obj.GetTransformMatrix();
                var r = Matrix4x4.Rotation(obj.Rotation);
                foreach (var v in obj.Vertices)
                {
                    var p = t.TransformPoint(v.Position);
                    var n = r.TransformDirection(v.Normal).Normalized();
                    sb.AppendLine($"{p.X:F6} {p.Y:F6} {p.Z:F6} {n.X:F6} {n.Y:F6} {n.Z:F6} {v.Color.R} {v.Color.G} {v.Color.B}");
                }
            }
            int offset = 0;
            foreach (var obj in scene.Objects)
            {
                foreach (var face in obj.Faces)
                    foreach (var tri in face.Triangulate())
                        sb.AppendLine($"3 {tri[0] + offset} {tri[1] + offset} {tri[2] + offset}");
                offset += obj.Vertices.Count;
            }
            return sb.ToString();
        }

        // ==================== COLLADA ====================
        
        public static string ExportCOLLADA(Scene scene)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<COLLADA xmlns=\"http://www.collada.org/2005/11/COLLADASchema\" version=\"1.4.1\">");
            sb.AppendLine("  <asset><created>2024-01-01</created><modified>2024-01-01</modified><unit name=\"meter\" meter=\"1\"/><up_axis>Y_UP</up_axis></asset>");
            sb.AppendLine("  <library_geometries>");
            foreach (var obj in scene.Objects)
            {
                sb.AppendLine($"    <geometry id=\"{obj.Name}-mesh\" name=\"{obj.Name}\"><mesh>");
                var positions = string.Join(" ", obj.Vertices.SelectMany(v => new[] { v.Position.X, v.Position.Y, v.Position.Z }).Select(f => f.ToString("F6")));
                sb.AppendLine($"      <source id=\"{obj.Name}-positions\"><float_array id=\"{obj.Name}-positions-array\" count=\"{obj.Vertices.Count * 3}\">{positions}</float_array><technique_common><accessor source=\"#{obj.Name}-positions-array\" count=\"{obj.Vertices.Count}\" stride=\"3\"><param name=\"X\" type=\"float\"/><param name=\"Y\" type=\"float\"/><param name=\"Z\" type=\"float\"/></accessor></technique_common></source>");
                sb.AppendLine($"      <vertices id=\"{obj.Name}-vertices\"><input semantic=\"POSITION\" source=\"#{obj.Name}-positions\"/></vertices>");
                sb.AppendLine($"    </mesh></geometry>");
            }
            sb.AppendLine("  </library_geometries>");
            sb.AppendLine("  <library_visual_scenes><visual_scene id=\"Scene\" name=\"Scene\">");
            foreach (var obj in scene.Objects)
                sb.AppendLine($"    <node id=\"{obj.Name}\" name=\"{obj.Name}\" type=\"NODE\"><matrix sid=\"transform\">{obj.Scale.X} 0 0 {obj.Position.X} 0 {obj.Scale.Y} 0 {obj.Position.Y} 0 0 {obj.Scale.Z} {obj.Position.Z} 0 0 0 1</matrix><instance_geometry url=\"#{obj.Name}-mesh\"/></node>");
            sb.AppendLine("  </visual_scene></library_visual_scenes>");
            sb.AppendLine("  <scene><instance_visual_scene url=\"#Scene\"/></scene>");
            sb.AppendLine("</COLLADA>");
            return sb.ToString();
        }

        // ==================== X3D ====================
        
        public static string ExportX3D(Scene scene)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<X3D profile=\"Interchange\" version=\"3.3\"><Scene>");
            foreach (var obj in scene.Objects)
            {
                var coords = string.Join(" ", obj.Vertices.Select(v => $"{v.Position.X:F6} {v.Position.Y:F6} {v.Position.Z:F6}"));
                var normals = string.Join(" ", obj.Vertices.Select(v => $"{v.Normal.X:F6} {v.Normal.Y:F6} {v.Normal.Z:F6}"));
                sb.AppendLine($"  <Shape><Appearance><Material diffuseColor=\"{obj.Materials[0].AlbedoColor.Rf:F3} {obj.Materials[0].AlbedoColor.Gf:F3} {obj.Materials[0].AlbedoColor.Bf:F3}\"/></Appearance>");
                sb.AppendLine($"    <IndexedFaceSet coordIndex=\"{string.Join(" ", obj.Faces.SelectMany(f => f.Indices.Select(i => i)).Select((v, i) => i % 3 == 2 && i > 0 ? v + " -1" : v.ToString()))}\">");
                sb.AppendLine($"      <Coordinate point=\"{coords}\"/><Normal vector=\"{normals}\"/>");
                sb.AppendLine("    </IndexedFaceSet></Shape>");
            }
            sb.AppendLine("</Scene></X3D>");
            return sb.ToString();
        }

        // ==================== VRML ====================
        
        public static string ExportVRML(Scene scene)
        {
            var sb = new StringBuilder();
            sb.AppendLine("#VRML V2.0 utf8");
            sb.AppendLine("# Easy3D Export");
            foreach (var obj in scene.Objects)
            {
                sb.AppendLine($"DEF {obj.Name} Transform {{");
                sb.AppendLine($"  translation {obj.Position.X:F6} {obj.Position.Y:F6} {obj.Position.Z:F6}");
                sb.AppendLine("  children [ Shape {");
                sb.AppendLine("    geometry IndexedFaceSet {");
                var coords = string.Join(", ", obj.Vertices.Select(v => $"{v.Position.X:F6} {v.Position.Y:F6} {v.Position.Z:F6}"));
                sb.AppendLine($"      coord Coordinate {{ point [ {coords} ] }}");
                sb.Append("      coordIndex [ ");
                foreach (var face in obj.Faces)
                    foreach (var tri in face.Triangulate())
                        sb.Append($"{tri[0]}, {tri[1]}, {tri[2]}, -1, ");
                sb.AppendLine("]");
                sb.AppendLine("    }");
                sb.AppendLine($"    appearance Appearance {{ material Material {{ diffuseColor {obj.Materials[0].AlbedoColor.Rf:F3} {obj.Materials[0].AlbedoColor.Gf:F3} {obj.Materials[0].AlbedoColor.Bf:F3} }} }}");
                sb.AppendLine("  } ] }");
            }
            return sb.ToString();
        }

        // ==================== OFF ====================
        
        public static string ExportOFF(Scene scene)
        {
            int totalV = scene.Objects.Sum(o => o.Vertices.Count);
            int totalF = scene.Objects.Sum(o => o.Faces.Sum(f => f.Triangulate().Count));
            var sb = new StringBuilder();
            sb.AppendLine("OFF");
            sb.AppendLine($"{totalV} {totalF} 0");
            foreach (var obj in scene.Objects)
            {
                var t = obj.GetTransformMatrix();
                foreach (var v in obj.Vertices)
                {
                    var p = t.TransformPoint(v.Position);
                    sb.AppendLine($"{p.X:F6} {p.Y:F6} {p.Z:F6}");
                }
            }
            int off = 0;
            foreach (var obj in scene.Objects)
            {
                foreach (var face in obj.Faces)
                    foreach (var tri in face.Triangulate())
                        sb.AppendLine($"3 {tri[0] + off} {tri[1] + off} {tri[2] + off}");
                off += obj.Vertices.Count;
            }
            return sb.ToString();
        }

        // ==================== AMF ====================
        
        public static string ExportAMF(Scene scene)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<amf unit=\"millimeter\" version=\"1.1\">");
            int id = 0;
            foreach (var obj in scene.Objects)
            {
                sb.AppendLine($"  <object id=\"{id}\"><mesh><vertices>");
                var t = obj.GetTransformMatrix();
                for (int i = 0; i < obj.Vertices.Count; i++)
                {
                    var p = t.TransformPoint(obj.Vertices[i].Position);
                    sb.AppendLine($"    <vertex id=\"{i}\"><coordinates><x>{p.X:F6}</x><y>{p.Y:F6}</y><z>{p.Z:F6}</z></coordinates></vertex>");
                }
                sb.AppendLine("  </vertices><volume>");
                int triId = 0;
                foreach (var face in obj.Faces)
                    foreach (var tri in face.Triangulate())
                        sb.AppendLine($"    <triangle id=\"{triId++}\"><v1>{tri[0]}</v1><v2>{tri[1]}</v2><v3>{tri[2]}</v3></triangle>");
                sb.AppendLine("  </volume></mesh></object>");
                id++;
            }
            sb.AppendLine("</amf>");
            return sb.ToString();
        }

        // ==================== USD ====================
        
        public static string ExportUSD(Scene scene)
        {
            var sb = new StringBuilder();
            sb.AppendLine("#usda 1.0");
            sb.AppendLine("(\n    defaultPrim = \"Scene\"\n)");
            sb.AppendLine("def Xform \"Scene\" {");
            foreach (var obj in scene.Objects)
            {
                sb.AppendLine($"    def Mesh \"{obj.Name}\" {{");
                var pts = string.Join(", ", obj.Vertices.Select(v => $"({v.Position.X:F6}, {v.Position.Y:F6}, {v.Position.Z:F6})"));
                sb.AppendLine($"        point3f[] points = [{pts}]");
                var idx = new List<int>(); var counts = new List<int>();
                foreach (var face in obj.Faces) { counts.Add(face.Count); idx.AddRange(face.Indices); }
                sb.AppendLine($"        int[] faceVertexIndices = [{string.Join(", ", idx)}]");
                sb.AppendLine($"        int[] faceVertexCounts = [{string.Join(", ", counts)}]");
                sb.AppendLine("    }");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

        // ==================== CSV ====================
        
        public static string ExportCSV(Scene scene)
        {
            var sb = new StringBuilder();
            sb.AppendLine("X,Y,Z,NX,NY,NZ,U,V,R,G,B,A");
            foreach (var obj in scene.Objects)
            {
                var t = obj.GetTransformMatrix();
                foreach (var v in obj.Vertices)
                {
                    var p = t.TransformPoint(v.Position);
                    sb.AppendLine($"{p.X:F6},{p.Y:F6},{p.Z:F6},{v.Normal.X:F6},{v.Normal.Y:F6},{v.Normal.Z:F6},{v.UV.X:F6},{v.UV.Y:F6},{v.Color.R},{v.Color.G},{v.Color.B},{v.Color.A}");
                }
            }
            return sb.ToString();
        }

        // ==================== Import OBJ ====================
        
        public static Scene ImportOBJ(string filePath)
        {
            var scene = new Scene();
            var verts = new List<Vertex>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var faces = new List<Face>();
            string currentName = Path.GetFileNameWithoutExtension(filePath);

            foreach (var line in File.ReadAllLines(filePath))
            {
                var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                switch (parts[0])
                {
                    case "o" or "g":
                        if (faces.Count > 0)
                        {
                            scene.AddObject(new Mesh3D(currentName, verts, faces));
                            verts = new(); norms = new(); uvs = new(); faces = new();
                        }
                        currentName = parts.Length > 1 ? string.Join(" ", parts[1..]) : "Object";
                        break;
                    case "v" when parts.Length >= 4:
                        verts.Add(new Vertex(new(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]))));
                        break;
                    case "vn" when parts.Length >= 4:
                        norms.Add(new(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3])));
                        break;
                    case "vt" when parts.Length >= 3:
                        uvs.Add(new(float.Parse(parts[1]), float.Parse(parts[2])));
                        break;
                    case "f":
                        var indices = new List<int>();
                        for (int i = 1; i < parts.Length; i++)
                        {
                            var p = parts[i].Split('/');
                            int vi = int.Parse(p[0]) - 1;
                            if (vi >= 0 && vi < verts.Count) indices.Add(vi);
                        }
                        if (indices.Count >= 3) faces.Add(new Face(indices));
                        break;
                }
            }
            if (faces.Count > 0) scene.AddObject(new Mesh3D(currentName, verts, faces));
            return scene;
        }

        // ==================== Import STL ====================
        
        public static Scene ImportSTL(string filePath)
        {
            var scene = new Scene();
            var verts = new List<Vertex>();
            var faces = new List<Face>();
            var lines = File.ReadAllLines(filePath);
            bool ascii = lines.Length > 0 && lines[0].Trim().StartsWith("solid");

            if (ascii)
            {
                var currentVerts = new List<Vector3>();
                foreach (var line in lines)
                {
                    var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0) continue;
                    if (parts[0] == "vertex" && parts.Length >= 4)
                        currentVerts.Add(new(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3])));
                    if (parts[0] == "endloop" && currentVerts.Count >= 3)
                    {
                        int baseIdx = verts.Count;
                        foreach (var v in currentVerts) verts.Add(new Vertex(v));
                        faces.Add(new Face(baseIdx, baseIdx + 1, baseIdx + 2));
                        currentVerts.Clear();
                    }
                }
            }
            if (verts.Count > 0) scene.AddObject(new Mesh3D(Path.GetFileNameWithoutExtension(filePath), verts, faces));
            return scene;
        }

        // ==================== Import GLTF ====================
        
        public static Scene ImportGLTF(string filePath) => ImportJSON(filePath); // Simplified

        // ==================== Import PLY ====================
        
        public static Scene ImportPLY(string filePath)
        {
            var scene = new Scene();
            var verts = new List<Vertex>();
            var faces = new List<Face>();
            var lines = File.ReadAllLines(filePath);
            int vertexCount = 0, faceCount = 0;
            bool headerEnded = false;
            int lineIdx = 0;

            for (; lineIdx < lines.Length; lineIdx++)
            {
                var line = lines[lineIdx].Trim();
                if (line.StartsWith("element vertex")) vertexCount = int.Parse(line.Split(' ')[2]);
                if (line.StartsWith("element face")) faceCount = int.Parse(line.Split(' ')[2]);
                if (line == "end_header") { headerEnded = true; lineIdx++; break; }
            }

            if (headerEnded)
            {
                for (int i = 0; i < vertexCount && lineIdx + i < lines.Length; i++)
                {
                    var p = lines[lineIdx + i].Trim().Split(' ');
                    if (p.Length >= 3) verts.Add(new Vertex(new(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]))));
                }
                lineIdx += vertexCount;
                for (int i = 0; i < faceCount && lineIdx + i < lines.Length; i++)
                {
                    var p = lines[lineIdx + i].Trim().Split(' ');
                    if (p.Length >= 4)
                    {
                        int count = int.Parse(p[0]);
                        var indices = new List<int>();
                        for (int j = 0; j < count; j++) indices.Add(int.Parse(p[1 + j]));
                        faces.Add(new Face(indices));
                    }
                }
            }
            if (verts.Count > 0) scene.AddObject(new Mesh3D(Path.GetFileNameWithoutExtension(filePath), verts, faces));
            return scene;
        }

        // ==================== Import JSON ====================
        
        public static Scene ImportJSON(string filePath)
        {
            var scene = new Scene();
            scene.Deserialize(File.ReadAllText(filePath));
            return scene;
        }
    }
}
