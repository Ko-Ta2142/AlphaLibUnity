using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AlphaLib
{
    public struct MeshBatchVertex
    {
        public float vx,vy,vz;
        public Vector2 uv1;
        public Vector2 uv2;
        public Color color;

        public void Clear()
        {
            vx = 0;
            vy = 0;
            vz = 0;

            color = Color.white;

            uv1 = new Vector2(0, 0);
            uv2 = new Vector2(0, 0);
        }
    }
    public class MeshBatchData
    {
        public MeshBatchVertex[] Vertex = null;
        public int[] Triangle;
        public Vector3 CenterPoint;
        public bool HasVertexColor = false;

        public MeshBatchData()
        {
            // empty
        }
        public MeshBatchData(Mesh m)
        {
            Build(m);
        }
        public MeshBatchData(Sprite s)
        {
            var m = Misc.SpriteToMesh(s);
            Build(m);
        }

        public void Build(Mesh m)
        {
            HasVertexColor = false;
            Vertex = null;

            if (m == null) return;

            CenterPoint = m.bounds.center;

            // buffer
            var vn = m.vertexCount;
            Vertex = new MeshBatchVertex[vn];
            for (int i = 0; i < Vertex.Length; i++)
            {
                Vertex[i].Clear();
            }

            // vertex
            {
                var v = new List<Vector3>();
                m.GetVertices(v);
                for (int i = 0; i < v.Count; i++)
                {
                    Vertex[i].vx = v[i].x;
                    Vertex[i].vy = v[i].y;
                    Vertex[i].vz = v[i].z;
                }
            }
            // uv1
            if (m.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0))
            {
                var uv = new List<Vector2>();
                m.GetUVs(0, uv);
                for (int i = 0; i < uv.Count; i++)
                {
                    Vertex[i].uv1 = uv[i];
                }
            }
            // uv2
            if (m.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord1))
            {
                var uv = new List<Vector2>();
                m.GetUVs(1, uv);
                for (int i = 0; i < uv.Count; i++)
                {
                    Vertex[i].uv2 = uv[i];
                }
            }
            else
            {
                // copy from uv1
                for (int i = 0; i < Vertex.Length; i++)
                {
                    Vertex[i].uv2 = Vertex[i].uv1;
                }
            }
            // diffuse
            if (m.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color))
            {
                HasVertexColor = true;

                var c = new List<Color>();
                m.GetColors(c);
                for (int i = 0; i < c.Count; i++)
                {
                    Vertex[i].color = c[i];
                }
            }
            // triangle
            {
                var uv = new List<int>();
                Triangle = m.GetTriangles(0);
            }
        }
    }

    public unsafe class MeshBatcher
    {
        public Material Material = null;
        public MaterialPropertyBlock Block = null;
        public Mesh Mesh = null;

        // mesh data buffer
        // * all struct
        public int VertexCount { get; protected set; }
        public int IndexCount { get; protected set; }

        public static readonly int[] TriangleIndices = new int[3] { 0, 1, 2 };
        public static readonly int[] PlaneIndices = new int[6] { 0, 1, 2, 0, 2, 3 };

        protected int VertexCapacity;
        protected int TriangleCapacity;
        protected Vector3[] cVertex = null;
        protected Vector2[] cUV1 = null;        // main texture
        protected Vector2[] cUV2 = null;        // sub texture(not use)
        protected Color[] cColor = null;        // diffuse color
        protected Vector4[] cTintColor = null;  // tint.rgb + alpha2
        protected int[] cTriangle = null;          // index list
        // plane only mode : index optimize
        //protected bool OnlyPlaneMode;
        //protected int[] cTriangle = null;       // fixed plane triangle list. 0,1,2, 0,2,3, ...

        // calc buffer
        protected Vector3[] v3Buffer = new Vector3[16];
        protected Vector2[] v2Buffer = new Vector2[16];
        // template
        protected Vector3[] PlaneMesh = new Vector3[4];

        public MeshBatcher(int vertexcapacity = 64 * 4, int trianglecapacity = 64 * 6)
        {
            VertexCapacity = vertexcapacity;
            TriangleCapacity = trianglecapacity;

            Clear(true);

            PlaneMesh[0] = new Vector3(0, 1, 0);
            PlaneMesh[1] = new Vector3(1, 1, 0);
            PlaneMesh[2] = new Vector3(1, 0, 0);
            PlaneMesh[3] = new Vector3(0, 0, 0);
        }
        public void CheckCapacity(int usagevertex, int usageindex)
        {
            if (VertexCount + usagevertex >= cVertex.Length) ResizeVertex(VertexCount + usagevertex);
            if (IndexCount + usageindex >= cTriangle.Length) ResizeIndex(IndexCount + usageindex);
        }
        protected void ResizeVertex(int count)
        {
            int n = 128;
            while (n < count) n = n * 2;

            Array.Resize<Vector3>(ref cVertex, n);
            Array.Resize<Vector2>(ref cUV1, n);
            Array.Resize<Vector2>(ref cUV2, n);
            Array.Resize<Color>(ref cColor, n);
            Array.Resize<Vector4>(ref cTintColor, n);
        }
        protected void ResizeIndex(int count)
        {
            int n = 128;
            while (n < count) n = n * 2;

            Array.Resize<int>(ref cTriangle, n);
        }
        public void Clear(bool initialize = false)
        {
            VertexCount = 0;
            IndexCount = 0;

            if (initialize)
            {
                ResizeVertex(VertexCapacity);
                ResizeIndex(TriangleCapacity);
            }
        }
        public void DrawArray(ref Matrix4x4 m, Vector3[] v, Vector2[] uv, Color[] color, Color[] tintcolor, int[] triangle)
        {
            var vn = v.Length;
            var tn = triangle.Length;

            if (vn < 3) return;
            if (tn < 3) return;

            CheckCapacity(vn, tn);

            {
                var idx = VertexCount;
                for (int i = 0; i < vn; i++)
                {
                    var tv = Misc.v3Transform(ref v[i], ref m);
                    cVertex[idx] = tv;
                    cUV1[idx] = uv[i];
                    cUV2[idx] = Vector2.zero;
                    cColor[idx] = color[i];
                    cTintColor[idx] = tintcolor[i];
                    cTintColor[idx].w = 0.0f;
                    idx++;
                }
            }
            {
                var idx = IndexCount;
                var vidx = VertexCount;
                for (int i = 0; i < tn; i++)
                {
                    cTriangle[idx + 0] = vidx + triangle[i];
                    idx++;
                }
            }

            VertexCount += vn;
            IndexCount += tn;
        }
        public void DrawArray(ref Matrix4x4 m, Vector3[] v, Vector2[] uv, ref Color color, ref Color tintcolor, int[] triangle)
        {
            var c = new Color[4];
            var t = new Color[4];
            for (int i = 0; i < 4; i++)
            {
                c[i] = color;
                t[i] = tintcolor;
            }

            DrawArray(ref m, v, uv, c, t, triangle);
        }
        public void DrawMesh(ref Matrix4x4 m, MeshBatchData mesh)
        {
            DrawMesh(ref m, mesh, Color.white, Color.black);
        }
        public unsafe void DrawMesh(ref Matrix4x4 m, MeshBatchData mesh, Color color, Color tintcolor)
        {
            if (mesh == null) return;

            var vn = mesh.Vertex.Length;
            var tn = mesh.Triangle.Length;
            tintcolor.a = 0.0f;

            if (vn < 3) return;
            if (tn < 3) return;

            CheckCapacity(vn, tn);

            fixed (MeshBatchVertex* meshV_ = &mesh.Vertex[0])
            {
                fixed (Vector3* outV_ = &cVertex[VertexCount])
                {
                    fixed (Vector2* outUV1_ = &cUV1[VertexCount])
                    {
                        fixed (Vector2* outUV2_ = &cUV2[VertexCount])
                        {
                            fixed (Color* outColor_ = &cColor[VertexCount])
                            {
                                fixed (Vector4* outTintColor_ = &cTintColor[VertexCount])
                                {
                                    var meshV = meshV_;
                                    var outV = outV_;
                                    var outUV1 = outUV1_;
                                    var outUV2 = outUV2_;
                                    var outColor = outColor_;
                                    var outTintColor = outTintColor_;

                                    for (int i = 0; i < vn; i++)
                                    {
                                        float x = (*meshV).vx;
                                        float y = (*meshV).vy;
                                        float z = (*meshV).vz;
                                        (*outV).x = x * m.m00 + y * m.m01 + z * m.m02 + m.m03;
                                        (*outV).y = x * m.m10 + y * m.m11 + z * m.m12 + m.m13;
                                        (*outV).z = x * m.m20 + y * m.m21 + z * m.m22 + m.m23;

                                        *outUV1 = (*meshV).uv1;
                                        *outUV2 = (*meshV).uv2;

                                        if (mesh.HasVertexColor)
                                        {
                                            (*outColor).a = (*meshV).color.a * color.a;
                                            (*outColor).r = (*meshV).color.r * color.r;
                                            (*outColor).g = (*meshV).color.g * color.g;
                                            (*outColor).b = (*meshV).color.b * color.b;
                                        }
                                        else
                                        {
                                            (*outColor) = color;
                                        }
                                        (*outTintColor) = tintcolor;

                                        meshV++;
                                        outV++;
                                        outUV1++;
                                        outUV2++;
                                        outColor++;
                                        outTintColor++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            fixed(int* outIndex_ = &cTriangle[IndexCount]) 
            {
                fixed (int* meshIndex_ = &mesh.Triangle[0])
                {
                    var outIndex = outIndex_;
                    var meshIndex = meshIndex_;
                    var offset = VertexCount;

                    for (int i = 0; i < tn; i++)
                    {
                        *outIndex = offset + *meshIndex;
                        outIndex++;
                        meshIndex++;
                    }
                }
            }

            VertexCount += vn;
            IndexCount += tn;
        }
        public void DrawPlane(ref PlaneSetting setting, ALMaterial mat)
        {
            DrawPlane(ref setting, mat, Color.white, Color.black);
        }
        public void DrawPlane(ref PlaneSetting setting, ALMaterial mat, Color color, Color tintcolor)
        {
            Matrix4x4 m = setting.ComputeMatrix();
            UVSlider slider = setting.ComputeUV();

            slider.GetUV(v2Buffer, mat);

            CheckCapacity(4, 6);

            for (int i = 0; i < 4; i++)
            {
                var idx = VertexCount + i;
                var tv = Misc.v3Transform(ref PlaneMesh[i], ref m);

                cVertex[idx] = tv;
                cUV1[idx] = v2Buffer[i];
                cUV2[idx] = v2Buffer[i + 4];
                cColor[idx] = color;
                cTintColor[idx] = tintcolor;
            }

            {
                var idx = IndexCount;
                var vidx = VertexCount;
                cTriangle[idx + 0] = vidx + 0;
                cTriangle[idx + 1] = vidx + 1;
                cTriangle[idx + 2] = vidx + 2;
                cTriangle[idx + 3] = vidx + 0;
                cTriangle[idx + 4] = vidx + 2;
                cTriangle[idx + 5] = vidx + 3;
            }

            VertexCount += 4;
            IndexCount += 6;
        }
        public void Build(bool autoclear = true, bool calcbounds = false)
        {
            if (Mesh == null)
            {
                Mesh = new Mesh();
                Mesh.RecalculateBounds();
            }

            Mesh.MarkDynamic();

            if (VertexCount < 3)
            {
                if (Mesh.vertexCount != 0) Mesh.Clear();
            }
            else
            {
                // optimize
                if ((Mesh.vertexCount != VertexCount) || (Mesh.GetIndexCount(0) != IndexCount))
                {
                    Mesh.Clear();
                }

                Mesh.SetVertices(cVertex, 0, VertexCount);
                Mesh.SetUVs(0, cUV1, 0, VertexCount);
                Mesh.SetUVs(1, cUV2, 0, VertexCount);
                Mesh.SetColors(cColor, 0, VertexCount);
                Mesh.SetUVs(2, cTintColor, 0, VertexCount);
                Mesh.SetTriangles(cTriangle, 0, IndexCount, 0, calcbounds);
            }

            //Mesh.MarkModified();
            Mesh.UploadMeshData(false); // disable free system memory flag. I hope to use the cache...
            if (autoclear) Clear();
        }
    }

    public class MeshBatcherLog
    {
        public Matrix4x4 matrix;

        public int polycount;
        public Vector3[] vertex = new Vector3[4];    // vertex
        public Color color;  
        public Color tintcolor;
        public Vector2[] uv1 = new Vector2[4];
        public Vector2[] uv2 = new Vector2[4];
        public Vector3 sortpoint;
        public float priority;
        public double sortvalue;

        public MeshBatchData meshdata;

        public MeshBatcherLog()
        {
            Clear();
        }

        public void Clear()
        {
            priority = 0;
            polycount = 0;
            meshdata = null;
            sortpoint = Vector3.zero;
        }
    }
    public class MeshBatcherLogComp : IComparer<MeshBatcherLog>
    {
        public int Compare(MeshBatcherLog x, MeshBatcherLog y)
        {
            if (x.sortvalue > y.sortvalue) return -1;
            if (x.sortvalue < y.sortvalue) return +1;
            
            return 0;
        }
    }
    public class MeshBatcherSortable
    { 
        public int Count { get { return Logs.Count; } }
        protected LogStackArray<MeshBatcherLog> Logs;
        public MeshBatcher Batcher = null;
        public Mesh Mesh { get { return Batcher.Mesh; } }
        public bool SortEnable = true;

        public MeshBatcherSortable(int capacity = 128)
        {
            Batcher = new MeshBatcher();
            Logs = new LogStackArray<MeshBatcherLog>(capacity);
        }
        public void Clear(bool initialize = false)
        {
            Logs.Clear(initialize);
        }
        public void DrawPolygon(ref Matrix4x4 m, Vector3[] v, Vector2[] uv, Color color, Color tintcolor, float priority=0)
        {
            var n = v.Length;
            if (n != 3)
                if (n != 4) return;

            var log = Logs.GetTop();
            log.Clear();

            log.polycount = n;
            log.matrix = m;

            Vector3 p = Vector3.zero;
            for (int i = 0; i < n; i++)
            {
                log.vertex[i] = v[i];
                log.uv1[i] = uv[i];
                log.uv2[i] = uv[i]; 

                p += v[i];
            }
            log.color = color;
            log.tintcolor = tintcolor;

            // priority
            log.sortpoint = Misc.v3Transform(ref p, ref m);
            log.priority = priority;

            Logs.Commit();
        }
        public void DrawPlane(ref PlaneSetting setting, ALMaterial mat, float priority = 0)
        {
            DrawPlane(ref setting, mat, Color.white, Color.black, priority);
        }
        public void DrawPlane(ref PlaneSetting setting, ALMaterial mat, Color color, Color tintcolor, float priority = 0)
        {
            Vector2[] uvs = new Vector2[8];
            Matrix4x4 m = setting.ComputeMatrix();
            UVSlider slider = setting.ComputeUV();

            slider.GetUV(uvs, mat);

            var log = Logs.GetTop();
            log.Clear();

            log.polycount = 4;
            log.matrix = m;

            log.vertex[0] = new Vector3(0, 1, 0);
            log.vertex[1] = new Vector3(1, 1, 0);
            log.vertex[2] = new Vector3(1, 0, 0);
            log.vertex[3] = new Vector3(0, 0, 0);
            for (int i = 0; i < 4; i++)
            {
                log.uv1[i] = uvs[i];
                log.uv2[i] = uvs[i + 4];
            }
            log.color = color;
            log.tintcolor = tintcolor;

            // priority
            log.sortpoint = new Vector3(0.5f, 0.5f, 0);
            log.priority = priority;

            Logs.Commit();
        }
        public void DrawMesh(ref Matrix4x4 m, MeshBatchData mesh, float priority = 0)
        {
            DrawMesh(ref m, mesh, Color.white,Color.black, priority);
        }
        public void DrawMesh(ref Matrix4x4 m, MeshBatchData mesh, Color color, Color tintcolor, float priority = 0)
        {
            if (mesh == null) return;
            if (mesh.Vertex.Length < 3) return;
            if (mesh.Triangle.Length < 3) return;

            var log = Logs.GetTop();
            log.Clear();

            log.matrix = m;
            log.meshdata = mesh;
            log.color = color;
            log.tintcolor = tintcolor;

            log.sortpoint = mesh.CenterPoint;
            log.priority = priority;

            Logs.Commit();
        }
        public void Build(bool autoclear = true)
        {
            var n = Logs.Count;

            // sort
            if (SortEnable)
            {
                // calc sorting point
                for (int i = 0; i < n; i++)
                {
                    var log = Logs.Data[i];
                    var v = Misc.v3Transform(ref log.sortpoint, ref log.matrix);
                    log.sortvalue = 65536 * log.priority + v.z;
                }

                Array.Sort<MeshBatcherLog>(Logs.Data, 0, Logs.Count, new MeshBatcherLogComp());
            }

            // build
            for (int i = 0; i < n; i++)
            {
                var log = Logs.Data[i];
                
                // polygon
                if (log.polycount == 3)
                {
                    Batcher.DrawArray(ref log.matrix, log.vertex, log.uv1, ref log.color, ref log.tintcolor, MeshBatcher.TriangleIndices);
                }
                if (log.polycount == 4)
                {
                    Batcher.DrawArray(ref log.matrix, log.vertex, log.uv1, ref log.color, ref log.tintcolor, MeshBatcher.PlaneIndices);
                }
                // mesh
                if (log.meshdata != null)
                {
                    var mesh = log.meshdata;
                    Batcher.DrawMesh(ref log.matrix, log.meshdata, log.color, log.tintcolor);
                }
            }

            Batcher.Build();
            if (autoclear) Clear();
        }
    }

    
}