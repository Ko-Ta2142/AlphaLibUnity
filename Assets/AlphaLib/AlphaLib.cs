using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AlphaLib
{
    /// <summary>
    /// Src x Dest blending mode. 
    /// NA is disable alpha color.
    /// </summary>
    public enum BlendMode
    {
        Pass,
        Normal,NANormal,PMA,
        Multiply, NAMultiply,
        Screen,NAScreen,
        Add,NAAdd,
        //ColorDodge,NAColorDodge,
        Max, NAMax,
        Sub,NASub,
        //ColorBurn,NAColorBurn,
        Min,NAMin,
        Mod2,NAMod2,
        Pow2,NAPow2,
        Exclusion,NAExclusion,
        LinearLight, NALinearLight
    }
    /// <summary>
    /// Shader mode. 
    /// single : Single texture shader.
    /// batch : Single texture bacher shader. include the uv,color,tintcolor.
    /// blend,fade,mask... : Two texture blend shader.
    /// </summary>
    public enum ShaderMode
    {
        Pass,
        Single,
        SingleBatch,
        MultiBlend,
        MultiFade,
        MultiMask,
        MultiOverlap,
        MultiExtract
    }
    public class Debug
    {
        public static bool LogEnable = true;

        public static void Error(string mes)
        {
            if (!LogEnable) return;
            UnityEngine.Debug.LogError(mes);
        }
        public static void Warning(string mes)
        {
            if (!LogEnable) return;
            UnityEngine.Debug.LogWarning(mes);
        }
        public static void Log(string mes)
        {
            if (!LogEnable) return;
            UnityEngine.Debug.Log(mes);
        }
    }
    // container
    public class LogStackArray<T>
        where T : class,new()
    {
        public int Capacity;
        public int Count;
        public T[] Data;
        public LogStackArray(int capacity = 8)
        {
            Capacity = capacity;

            Count = 0;
            Clear(true);
        }
        public void Clear(bool initialize = false)
        {
            if (initialize)
            {
                Data = null;
                ResizeAndCreate(Capacity);
            }
            Count = 0;
        }
        public T GetTop()
        {
            // resize
            {
                var need = Count + 1;
                if (need > Data.Length)
                {
                    var n = 32;
                    while (n < need) n = n * 2;
                    ResizeAndCreate(n);
                }
            }
            return Data[Count];
        }
        public void Commit()
        {
            Count++;
        }
        public void Cancel()
        {
            //
        }

        protected void ResizeAndCreate(int length)
        {
            var n = 0;
            if (Data != null) n = Data.Length;

            Array.Resize<T>(ref Data, length);

            if (n < length)
            {
                for (int i = n; i < length; i++)
                {
                    Data[i] = new T();
                }
            }
        }
    }
    // misc (shader & matrix) static class
    public class Misc
    {
        // common shaders materials
        public static Dictionary<string, Shader> CacheShaders = new Dictionary<string, Shader>();
        public static Dictionary<ShaderMode, Dictionary<BlendMode, Material>> CacheMaterials = new Dictionary<ShaderMode, Dictionary<BlendMode, Material>>();   // :Q
        // array chache
        public static List<Vector3> Vector3Buf = new List<Vector3>(32);
        public static List<Vector2> Vector2Buf = new List<Vector2>(32);
        public static List<Vector4> Vector4Buf = new List<Vector4>(32);
        public static List<int> RegionIndexBuf = new List<int>(32);
        public static List<Color> ColorBuf = new List<Color>(32);

        protected static bool IsSetup = false;
        public static void Setup()
        {
            if (IsSetup) return;

            // cache buffer
            RegionIndexBuf.Add(0);
            RegionIndexBuf.Add(1);
            RegionIndexBuf.Add(2);
            RegionIndexBuf.Add(0);
            RegionIndexBuf.Add(2);
            RegionIndexBuf.Add(3);

            IsSetup = true;
        }
        // array
        public static void ArrayResizeAndCreate<T>(ref T[] array, int length)
            where T : class,new()
        {
            var n = 0;
            if (array != null) n = array.Length;

            Array.Resize<T>(ref array,length);

            if (n < length)
            {
                for (int i = n; i < length; i++)
                {
                    array[i] = new T();
                }
            }
        }
        // misc
        public static Color RGBtoColor(Int32 rgb, float alpha)
        {
            var r = (rgb >> 16) * (1.0f / 255.0f);
            var g = (rgb >> 16) * (1.0f / 255.0f);
            var b = (rgb >> 16) * (1.0f / 255.0f);

            return new Color(r, g, b, alpha);
        }
        // mesh
        public static void CreatePlaneMesh(Mesh m, float size, bool centering)
        {
            var off = size / 2.0f;
            if (centering) off = 0.0f;

            var p = size / 2.0f;
            Vector3Buf.Clear();
            Vector3Buf.Add(new Vector3(-p+off, +p+off, 0));
            Vector3Buf.Add(new Vector3(+p+off, +p+off, 0));
            Vector3Buf.Add(new Vector3(+p+off, -p+off, 0));
            Vector3Buf.Add(new Vector3(-p+off, -p+off, 0));
            m.SetVertices(Vector3Buf);

            Vector2Buf.Clear();
            Vector2Buf.Add(new Vector2(0, 1));
            Vector2Buf.Add(new Vector2(1, 1));
            Vector2Buf.Add(new Vector2(1, 0));
            Vector2Buf.Add(new Vector2(0, 0));
            m.SetUVs(0, Vector2Buf);
            m.SetUVs(1, Vector2Buf);

            m.SetTriangles(RegionIndexBuf, 0);
        }
        public static Mesh CreateTriangleMesh()
        {
            var m = new Mesh();

            var v = new List<Vector3>(3);
            var uv = new List<Vector2>(3);
            var color = new List<Color>(3);
            var idx = new List<int>(3);

            var p = 0.01f * 100.0f / 2.0f;
            v.Add(new Vector3(-p, +p, 0));
            v.Add(new Vector3(+p, +p, 0));
            v.Add(new Vector3(+p, -p, 0));
            m.SetVertices(v);

            uv.Add(new Vector2(0, 1));
            uv.Add(new Vector2(1, 1));
            uv.Add(new Vector2(1, 0));
            m.SetUVs(0, uv);
            m.SetUVs(1, uv);

            idx.Add(0);
            idx.Add(1);
            idx.Add(2);
            m.SetTriangles(idx,0);

            return m;
        }
        public static Mesh SpriteToMesh(Sprite sprite)
        {
            var mesh = new Mesh();
            {
                var v = Array.ConvertAll(sprite.vertices, c => (Vector3)c);
                mesh.vertices = v;
            }
            {
                var uv = sprite.uv;
                mesh.uv = uv;
                mesh.uv2 = uv;
            }
            {
                var tri = Array.ConvertAll(sprite.triangles, c => (int)c);
                mesh.triangles = tri;
            }

            return mesh;
        }
        // meterial
        public static ALMaterial CreateMaterial(BlendMode blend, ShaderMode shader, Texture2D maintex = null, Texture2D subtex = null)
        {
            // shader mode
            Dictionary<BlendMode, Material> mats = null;
            if (!CacheMaterials.TryGetValue(shader, out mats))
            {
                // create
                mats = new Dictionary<BlendMode, Material>();
                CacheMaterials[shader] = mats;
            }

            // blend mode
            Material m = null;
            if (!mats.TryGetValue(blend, out m))
            {
                // base name
                var basename = "Single";
                switch (shader)
                {
                    case ShaderMode.Single:
                        basename = "Single";
                        break;
                    case ShaderMode.SingleBatch:
                        basename = "SingleBatch";
                        break;
                    case ShaderMode.MultiBlend:
                    case ShaderMode.MultiFade:
                    case ShaderMode.MultiMask:
                    case ShaderMode.MultiOverlap:
                    case ShaderMode.MultiExtract:
                        basename = "Multi";
                        break;
                }

                // sub name (blend)
                var subname = "_Common";
                switch (blend)
                {
                    case BlendMode.PMA:
                        subname = "_PMA";
                        break;
                }

                // load shader and material state set.
                m = CreateMaterial(basename + subname, blend, shader);
                if (m == null) return null;

                mats[blend] = m;
            }

            var mat = new ALMaterial(m);
            mat.MainTexture = maintex;
            mat.SubTexture = subtex;

            return mat;
        }
        public static Material CreateMaterial(string shadername, BlendMode blend = BlendMode.Pass, ShaderMode shader = ShaderMode.Pass)
        {
            var name = "AlphaLib/" + shadername;

            // shader cache
            Shader s = null;
            if (!CacheShaders.TryGetValue(name, out s))
            {
                s = Shader.Find(name);

                if (s == null)
                {
                    AlphaLib.Debug.Error("AlphaLib : Do not found the shader : " + name);
                    return null;
                }

                CacheShaders[name] = s;
            }

            // make material
            var m = new Material(s);
            m.renderQueue = -1; // from shader

            switch (blend)
            {
                case BlendMode.Pass:
                    break;
                case BlendMode.NANormal:
                case BlendMode.Normal:
                    m.SetInt("_srcblend", (int)UnityEngine.Rendering.BlendMode.One);
                    m.SetInt("_destblend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    m.SetInt("_blendop", (int)UnityEngine.Rendering.BlendOp.Add);
                    m.SetColor("_transcolor", Color.black);
                    break;
                case BlendMode.PMA:
                    m.SetInt("_srcblend", (int)UnityEngine.Rendering.BlendMode.One);
                    m.SetInt("_destblend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    m.SetInt("_blendop", (int)UnityEngine.Rendering.BlendOp.Add);
                    m.SetColor("_transcolor", Color.black);
                    break;
                case BlendMode.NAScreen:
                case BlendMode.Screen:
                    m.SetInt("_srcblend", (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor);
                    m.SetInt("_destblend", (int)UnityEngine.Rendering.BlendMode.One);
                    m.SetInt("_blendop", (int)UnityEngine.Rendering.BlendOp.Add);
                    m.SetColor("_transcolor", Color.black);
                    break;
                case BlendMode.NAAdd:
                case BlendMode.Add:
                    m.SetInt("_srcblend", (int)UnityEngine.Rendering.BlendMode.One);
                    m.SetInt("_destblend", (int)UnityEngine.Rendering.BlendMode.One);
                    m.SetInt("_blendop", (int)UnityEngine.Rendering.BlendOp.Add);
                    m.SetColor("_transcolor", Color.black);
                    break;
                case BlendMode.NAMultiply:
                case BlendMode.Multiply:
                    m.SetInt("_srcblend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                    m.SetInt("_destblend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    m.SetInt("_blendop", (int)UnityEngine.Rendering.BlendOp.Add);
                    m.SetColor("_transcolor", Color.white);
                    break;
                case BlendMode.NASub:
                case BlendMode.Sub:
                    m.SetInt("_srcblend", (int)UnityEngine.Rendering.BlendMode.One);
                    m.SetInt("_destblend", (int)UnityEngine.Rendering.BlendMode.One);
                    m.SetInt("_blendop", (int)UnityEngine.Rendering.BlendOp.ReverseSubtract);
                    m.SetColor("_transcolor", Color.black);
                    break;
                case BlendMode.NAMin:
                case BlendMode.Min:
                    m.SetInt("_srcblend", (int)UnityEngine.Rendering.BlendMode.One);
                    m.SetInt("_destblend", (int)UnityEngine.Rendering.BlendMode.One);
                    m.SetInt("_blendop", (int)UnityEngine.Rendering.BlendOp.Min);
                    m.SetColor("_transcolor", Color.white);
                    break;
                case BlendMode.NAMax:
                case BlendMode.Max:
                    m.SetInt("_srcblend", (int)UnityEngine.Rendering.BlendMode.One);
                    m.SetInt("_destblend", (int)UnityEngine.Rendering.BlendMode.One);
                    m.SetInt("_blendop", (int)UnityEngine.Rendering.BlendOp.Max);
                    m.SetColor("_transcolor", Color.black);
                    break;
                case BlendMode.NAMod2:
                case BlendMode.Mod2:
                    m.SetInt("_srcblend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                    m.SetInt("_destblend", (int)UnityEngine.Rendering.BlendMode.SrcColor);
                    m.SetInt("_blendop", (int)UnityEngine.Rendering.BlendOp.Add);
                    m.SetColor("_transcolor", Color.gray);
                    break;
                case BlendMode.NAPow2:
                case BlendMode.Pow2:
                    m.SetInt("_srcblend", (int)UnityEngine.Rendering.BlendMode.SrcColor);
                    m.SetInt("_destblend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                    m.SetInt("_blendop", (int)UnityEngine.Rendering.BlendOp.Add);
                    m.SetColor("_transcolor", Color.gray);
                    break;
                case BlendMode.NAExclusion:
                case BlendMode.Exclusion:
                    m.SetInt("_srcblend", (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor);
                    m.SetInt("_destblend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor);
                    m.SetInt("_blendop", (int)UnityEngine.Rendering.BlendOp.Add);
                    m.SetColor("_transcolor", Color.black);
                    break;
                case BlendMode.NALinearLight:
                case BlendMode.LinearLight:
                    m.EnableKeyword("_CALC_LINEARLIGHT");
                    m.SetInt("_srcblend", (int)UnityEngine.Rendering.BlendMode.One);
                    m.SetInt("_destblend", (int)UnityEngine.Rendering.BlendMode.One);
                    m.SetInt("_blendop", (int)UnityEngine.Rendering.BlendOp.ReverseSubtract);
                    m.SetColor("_transcolor", Color.gray);
                    break;
                /*
                case BlendMode.NAColorDodge:
                case BlendMode.ColorDodge:
                    m.EnableKeyword("_CALC_COLORDODGE");
                    m.SetInt("_srcblend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                    m.SetInt("_destblend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    m.SetInt("_blendop", (int)UnityEngine.Rendering.BlendOp.Add);
                    m.SetColor("_transcolor", Color.black);
                    break;
                case BlendMode.NAColorBurn:
                case BlendMode.ColorBurn:
                    m.EnableKeyword("_CALC_COLORBURN");
                    m.SetInt("_srcblend", (int)UnityEngine.Rendering.BlendMode.One);
                    m.SetInt("_destblend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor);
                    m.SetInt("_blendop", (int)UnityEngine.Rendering.BlendOp.Add);
                    m.SetColor("_transcolor", Color.white);
                    break;
                */
            }
            // non alpha
            switch (blend)
            {
                case BlendMode.NANormal:
                case BlendMode.NAScreen:
                case BlendMode.NAAdd:
                case BlendMode.NASub:
                case BlendMode.NAMultiply:
                case BlendMode.NAMin:
                case BlendMode.NAMax:
                case BlendMode.NAMod2:
                case BlendMode.NAPow2:
                case BlendMode.NAExclusion:
                case BlendMode.NALinearLight:
                //case BlendMode.NAColorBurn:
                //case BlendMode.NAColorDodge:
                    m.SetFloat("_nonalpha", 1.0f);
                    break;
            }
            // multi texture blend
            switch (shader)
            {
                case ShaderMode.Pass:
                case ShaderMode.Single:
                case ShaderMode.SingleBatch:
                    break;
                case ShaderMode.MultiBlend:
                    m.EnableKeyword("_MULTI_BLEND");
                    break;
                case ShaderMode.MultiMask:
                    m.EnableKeyword("_MULTI_MASK");
                    break;
                case ShaderMode.MultiFade:
                    m.EnableKeyword("_MULTI_FADE");
                    break;
                case ShaderMode.MultiOverlap:
                    m.EnableKeyword("_MULTI_OVERLAP");
                    break;
                case ShaderMode.MultiExtract:
                    m.EnableKeyword("_MULTI_EXTRACT");
                    break;
            }

            return m;
        }
        
        public static ALTexture CreateTextureFromResource(string filename)
        {
            var tex = Resources.Load<Texture2D>(filename);
            return new ALTexture(tex);
        }

        // matrix
        public static void m43PushMatrix(ref Matrix4x4 m1, ref Matrix4x4 m2)
        {
            float v1 = m1.m00;
            float v2 = m1.m10;
            float v3 = m1.m20;
            //float v4 = m1.m30;
            m1.m00 = m2.m00 * v1 + m2.m01 * v2 + m2.m02 * v3;// + m2.m03 * v4;
            m1.m10 = m2.m10 * v1 + m2.m11 * v2 + m2.m12 * v3;// + m2.m13 * v4;
            m1.m20 = m2.m20 * v1 + m2.m21 * v2 + m2.m22 * v3;// + m2.m23 * v4;
            //m1.m30 = m2.m30 * v1 + m2.m31 * v2 + m2.m32 * v3 + m2.m33 * v4;

            v1 = m1.m01;
            v2 = m1.m11;
            v3 = m1.m21;
            //v4 = m1.m31;
            m1.m01 = m2.m00 * v1 + m2.m01 * v2 + m2.m02 * v3;// + m2.m03 * v4;
            m1.m11 = m2.m10 * v1 + m2.m11 * v2 + m2.m12 * v3;// + m2.m13 * v4;
            m1.m21 = m2.m20 * v1 + m2.m21 * v2 + m2.m22 * v3;// + m2.m23 * v4;
            //m1.m31 = m2.m30 * v1 + m2.m31 * v2 + m2.m32 * v3 + m2.m33 * v4;

            v1 = m1.m02;
            v2 = m1.m12;
            v3 = m1.m22;
            //v4 = m1.m32;
            m1.m02 = m2.m00 * v1 + m2.m01 * v2 + m2.m02 * v3;// + m2.m03 * v4;
            m1.m12 = m2.m10 * v1 + m2.m11 * v2 + m2.m12 * v3;// + m2.m13 * v4;
            m1.m22 = m2.m20 * v1 + m2.m21 * v2 + m2.m22 * v3;// + m2.m23 * v4;
            //m1.m32 = m2.m30 * v1 + m2.m31 * v2 + m2.m32 * v3 + m2.m33 * v4;

            v1 = m1.m03;
            v2 = m1.m13;
            v3 = m1.m23;
            //float v4 = m1.m33;
            m1.m03 = m2.m00 * v1 + m2.m01 * v2 + m2.m02 * v3 + m2.m03;// * v4;
            m1.m13 = m2.m10 * v1 + m2.m11 * v2 + m2.m12 * v3 + m2.m13;// * v4;
            m1.m23 = m2.m20 * v1 + m2.m21 * v2 + m2.m22 * v3 + m2.m23;// * v4;
            //m1.m33 = m2.m30 * v1 + m2.m31 * v2 + m2.m32 * v3 + m2.m33 * v4;
        }
        public static void m43PushMatrixBack(ref Matrix4x4 m2, ref Matrix4x4 m1)
        {
            float v1 = m1.m00;
            float v2 = m1.m10;
            float v3 = m1.m20;
            //float v4 = m1.m30;
            m1.m00 = m2.m00 * v1 + m2.m01 * v2 + m2.m02 * v3;// + m2.m03 * v4;
            m1.m10 = m2.m10 * v1 + m2.m11 * v2 + m2.m12 * v3;// + m2.m13 * v4;
            m1.m20 = m2.m20 * v1 + m2.m21 * v2 + m2.m22 * v3;// + m2.m23 * v4;
            //m1.m30 = m2.m30 * v1 + m2.m31 * v2 + m2.m32 * v3 + m2.m33 * v4;

            v1 = m1.m01;
            v2 = m1.m11;
            v3 = m1.m21;
            //v4 = m1.m31;
            m1.m01 = m2.m00 * v1 + m2.m01 * v2 + m2.m02 * v3;// + m2.m03 * v4;
            m1.m11 = m2.m10 * v1 + m2.m11 * v2 + m2.m12 * v3;// + m2.m13 * v4;
            m1.m21 = m2.m20 * v1 + m2.m21 * v2 + m2.m22 * v3;// + m2.m23 * v4;
            //m1.m31 = m2.m30 * v1 + m2.m31 * v2 + m2.m32 * v3 + m2.m33 * v4;

            v1 = m1.m02;
            v2 = m1.m12;
            v3 = m1.m22;
            //v4 = m1.m32;
            m1.m02 = m2.m00 * v1 + m2.m01 * v2 + m2.m02 * v3;// + m2.m03 * v4;
            m1.m12 = m2.m10 * v1 + m2.m11 * v2 + m2.m12 * v3;// + m2.m13 * v4;
            m1.m22 = m2.m20 * v1 + m2.m21 * v2 + m2.m22 * v3;// + m2.m23 * v4;
            //m1.m32 = m2.m30 * v1 + m2.m31 * v2 + m2.m32 * v3 + m2.m33 * v4;

            v1 = m1.m03;
            v2 = m1.m13;
            v3 = m1.m23;
            //float v4 = m1.m33;
            m1.m03 = m2.m00 * v1 + m2.m01 * v2 + m2.m02 * v3 + m2.m03;// * v4;
            m1.m13 = m2.m10 * v1 + m2.m11 * v2 + m2.m12 * v3 + m2.m13;// * v4;
            m1.m23 = m2.m20 * v1 + m2.m21 * v2 + m2.m22 * v3 + m2.m23;// * v4;
            //m1.m33 = m2.m30 * v1 + m2.m31 * v2 + m2.m32 * v3 + m2.m33 * v4;
        }
        public static void m44PushMatrix(ref Matrix4x4 m1, ref Matrix4x4 m2)
        {
            float v1 = m1.m00;
            float v2 = m1.m10;
            float v3 = m1.m20;
            float v4 = m1.m30;
            m1.m00 = m2.m00 * v1 + m2.m01 * v2 + m2.m02 * v3 + m2.m03 * v4;
            m1.m10 = m2.m10 * v1 + m2.m11 * v2 + m2.m12 * v3 + m2.m13 * v4;
            m1.m20 = m2.m20 * v1 + m2.m21 * v2 + m2.m22 * v3 + m2.m23 * v4;
            m1.m30 = m2.m30 * v1 + m2.m31 * v2 + m2.m32 * v3 + m2.m33 * v4;

            v1 = m1.m01;
            v2 = m1.m11;
            v3 = m1.m21;
            v4 = m1.m31;
            m1.m01 = m2.m00 * v1 + m2.m01 * v2 + m2.m02 * v3 + m2.m03 * v4;
            m1.m11 = m2.m10 * v1 + m2.m11 * v2 + m2.m12 * v3 + m2.m13 * v4;
            m1.m21 = m2.m20 * v1 + m2.m21 * v2 + m2.m22 * v3 + m2.m23 * v4;
            m1.m31 = m2.m30 * v1 + m2.m31 * v2 + m2.m32 * v3 + m2.m33 * v4;

            v1 = m1.m02;
            v2 = m1.m12;
            v3 = m1.m22;
            v4 = m1.m32;
            m1.m02 = m2.m00 * v1 + m2.m01 * v2 + m2.m02 * v3 + m2.m03 * v4;
            m1.m12 = m2.m10 * v1 + m2.m11 * v2 + m2.m12 * v3 + m2.m13 * v4;
            m1.m22 = m2.m20 * v1 + m2.m21 * v2 + m2.m22 * v3 + m2.m23 * v4;
            m1.m32 = m2.m30 * v1 + m2.m31 * v2 + m2.m32 * v3 + m2.m33 * v4;

            v1 = m1.m03;
            v2 = m1.m13;
            v3 = m1.m23;
            v4 = m1.m33;
            m1.m03 = m2.m00 * v1 + m2.m01 * v2 + m2.m02 * v3 + m2.m03 * v4;
            m1.m13 = m2.m10 * v1 + m2.m11 * v2 + m2.m12 * v3 + m2.m13 * v4;
            m1.m23 = m2.m20 * v1 + m2.m21 * v2 + m2.m22 * v3 + m2.m23 * v4;
            m1.m33 = m2.m30 * v1 + m2.m31 * v2 + m2.m32 * v3 + m2.m33 * v4;
        }
        public static Vector3 v3Transform(ref Vector3 v, ref Matrix4x4 m)
        {
            Vector3 o;
            o.x = v.x * m.m00 + v.y * m.m01 + v.z * m.m02 + m.m03;
            o.y = v.x * m.m10 + v.y * m.m11 + v.z * m.m12 + m.m13;
            o.z = v.x * m.m20 + v.y * m.m21 + v.z * m.m22 + m.m23;
            return o;
        }
        public static void m4PushMove(ref Matrix4x4 m, float x, float y, float z)
        {
            if ((x == 0.0f) && (y == 0.0f) && (z == 0.0f)) return;
            //      1         2         3        4
            // 1                                 x
            // 2                                 y
            // 3                                 z
            // 4                              
            float v4 = m.m30;
            m.m00 += v4 * x;
            m.m10 += v4 * y;
            m.m20 += v4 * z;

            v4 = m.m31;
            m.m01 += v4 * x;
            m.m11 += v4 * y;
            m.m21 += v4 * z;

            v4 = m.m32;
            m.m02 += v4 * x;
            m.m12 += v4 * y;
            m.m22 += v4 * z;

            v4 = m.m33;
            m.m03 += v4 * x;
            m.m13 += v4 * y;
            m.m23 += v4 * z;
        }
        public static void m4PushRotateX(ref Matrix4x4 m, float r)
        {
            if (r == 0.0f) return;

            //      1         2         3        4
            // 1
            // 2              c         -s
            // 3              s         c
            // 4
            r = r * (1.0f / 360.0f);
            float s = (float)Math.Sin(r * 2.0f * Math.PI);
            float c = (float)Math.Cos(r * 2.0f * Math.PI);

            float v2 = m.m10;
            float v3 = m.m20;
            m.m10 = v2 * c + v3 * -s;
            m.m20 = v2 * s + v3 * c;

            v2 = m.m11;
            v3 = m.m21;
            m.m11 = v2 * c + v3 * -s;
            m.m21 = v2 * s + v3 * c;

            v2 = m.m12;
            v3 = m.m22;
            m.m12 = v2 * c + v3 * -s;
            m.m22 = v2 * s + v3 * c;

            v2 = m.m13;
            v3 = m.m23;
            m.m13 = v2 * c + v3 * -s;
            m.m23 = v2 * s + v3 * c;
        }
        public static void m4PushRotateY(ref Matrix4x4 m, float r)
        {
            if (r == 0.0f) return;

            //      1         2         3        4
            // 1    c                   s
            // 2
            // 3    -s                  c
            // 4
            r = r * (1.0f / 360.0f);
            float s = (float)Math.Sin(r * 2.0f * Math.PI);
            float c = (float)Math.Cos(r * 2.0f * Math.PI);

            float v1 = m.m00;
            float v3 = m.m20;
            m.m00 = v1 * c + v3 * s;
            m.m20 = v1 * -s + v3 * c;

            v1 = m.m01;
            v3 = m.m21;
            m.m01 = v1 * c + v3 * s;
            m.m21 = v1 * -s + v3 * c;

            v1 = m.m02;
            v3 = m.m22;
            m.m02 = v1 * c + v3 * s;
            m.m22 = v1 * -s + v3 * c;

            v1 = m.m03;
            v3 = m.m23;
            m.m03 = v1 * c + v3 * s;
            m.m23 = v1 * -s + v3 * c;
        }
        public static void m4PushRotateZ(ref Matrix4x4 m, float r)
        {
            if (r == 0.0f) return;

            //      1         2         3        4
            // 1    c         -s
            // 2    s         c
            // 3
            // 4
            r = r * (1.0f / 360.0f);
            float s = (float)Math.Sin(r * 2.0f * Math.PI);
            float c = (float)Math.Cos(r * 2.0f * Math.PI);

            float v1 = m.m00;
            float v2 = m.m10;
            m.m00 = v1 * c  + v2 * s;
            m.m10 = v1 * -s + v2 * c;

            v1 = m.m01;
            v2 = m.m11;
            m.m01 = v1 * c  + v2 * s;
            m.m11 = v1 * -s + v2 * c;

            v1 = m.m02;
            v2 = m.m12;
            m.m02 = v1 * c  + v2 * s;
            m.m12 = v1 * -s + v2 * c;

            v1 = m.m03;
            v2 = m.m13;
            m.m03 = v1 * c  + v2 * s;
            m.m13 = v1 * -s + v2 * c;
        }
        public static void m4PushScale(ref Matrix4x4 m, float x, float y, float z)
        {
            if ((x == 1.0f) && (y == 1.0f) && (z == 1.0f)) return;

            //      1         2         3        4
            // 1    x
            // 2              y
            // 3                        z
            // 4                                 1.0
            m.m00 *= x;
            m.m10 *= y;
            m.m20 *= z;

            m.m01 *= x;
            m.m11 *= y;
            m.m21 *= z;

            m.m02 *= x;
            m.m12 *= y;
            m.m22 *= z;

            m.m03 *= x;
            m.m13 *= y;
            m.m23 *= z;
        }
    }
    // material
    public class ALMaterial
    {
        public UnityEngine.Material Material = null;
        public UnityEngine.Texture2D MainTexture = null;
        public UnityEngine.Texture2D SubTexture = null;

        // virtual size
        public float VirtualMainWidth = -1;     // -1 : auto
        public float VirtualMainHeight = -1;
        public float VirtualSubWidth = -1;     // -1 : auto
        public float VirtualSubHeight = -1;

        // shader property id
        public int ID_maintex;
        public int ID_subtex;
        public int ID_maintex_ST;
        public int ID_subtex_ST;
        public int ID_color;
        public int ID_tintcolor;
        public int ID_option;
        public int ID_invsubtex;

        public ALMaterial(Material mat)
        {
            Material = mat;

            SetupPropertyID();
        }

        public Vector2 GetVirtualMainSize()
        {
            if (MainTexture == null) return new Vector2(1.0f, 1.0f);

            float w = MainTexture.width;
            float h = MainTexture.height;
            if (VirtualMainWidth > 0) w = VirtualMainWidth;
            if (VirtualMainHeight > 0) h = VirtualMainHeight;

            return new Vector2(w, h);
        }
        public Vector2 GetVirtualSubSize()
        {
            if (SubTexture == null) return new Vector2(1.0f, 1.0f);

            float w = SubTexture.width;
            float h = SubTexture.height;
            if (VirtualSubWidth > 0) w = VirtualSubWidth;
            if (VirtualSubHeight > 0) h = VirtualSubHeight;

            return new Vector2(w, h);
        }

        public void SetupPropertyID()
        {
            var shader = Material.shader;
            if (shader == null) return;

            ID_maintex = Shader.PropertyToID("_maintex");
            ID_subtex = Shader.PropertyToID("_subtex");
            ID_maintex_ST = Shader.PropertyToID("_maintex_ST");
            ID_subtex_ST = Shader.PropertyToID("_subtex_ST");
            ID_color = Shader.PropertyToID("_color");
            ID_tintcolor = Shader.PropertyToID("_tintcolor");
            ID_option = Shader.PropertyToID("_option");
            ID_invsubtex = Shader.PropertyToID("_invsubtex");
        }
        public void SetVirtualSize(int w, int h)
        {
            VirtualMainWidth = w;
            VirtualMainHeight = h;
            VirtualSubWidth = w;
            VirtualSubHeight = h;
        }
        public void SetVirtualSize(int mainWidth, int mainHeight, int subWidth, int subHeight)
        {
            VirtualMainWidth = mainWidth;
            VirtualMainHeight = mainHeight;
            VirtualSubWidth = subWidth;
            VirtualSubHeight = subHeight;
        }
    }

    // texture pixel to uv controler struct
    public enum UVSliderMode
    {
        Area,
        DirectUV
    }
    public struct UVSlider
    {
        public static bool InvertY = true;

        public UVSliderMode Mode;
        public float sx1, sy1, sx2, sy2;    // main texture area
        public float sx3, sy3, sx4, sy4;    // sub texture area

        public void Clear()
        {
            sx1 = 0;
            sy1 = 0;
            sx2 = 0;
            sy2 = 0;
            sx3 = 0;
            sy3 = 0;
            sx4 = 0;
            sy4 = 0;
        }
        public void SetPropertyBlock(ALMaterial mat, MaterialPropertyBlock block)
        {
            // compute UV slider Vector4 (shader texture_ST)
            // v.x : scale uv.x
            // v.y : scale uv.y
            // v.z : offset uv.x
            // v.w : offset uv.y

            if (mat == null) return;

            switch (Mode)
            {
                case UVSliderMode.Area:
                    // main texture
                    {
                        var size = mat.GetVirtualMainSize();
                        var aw = 1.0f / size.x;
                        var ah = 1.0f / size.y;
                        if (InvertY)
                            block.SetVector(mat.ID_maintex_ST, new Vector4((sx2 - sx1) * aw, (sy2 - sy1) * ah, sx1 * aw, 1.0f - sy1 * ah));
                        else
                            block.SetVector(mat.ID_maintex_ST, new Vector4((sx2 - sx1) * aw, (sy2 - sy1) * ah, sx1 * aw, sy1 * ah));
                    }
                    // sub texture
                    {
                        var size = mat.GetVirtualSubSize();
                        var aw = 1.0f / size.x;
                        var ah = 1.0f / size.y;
                        if (InvertY)
                            block.SetVector(mat.ID_subtex_ST, new Vector4((sx4 - sx3) * aw, (sy4 - sy3) * ah, sx3 * aw, 1.0f - sy3 * ah));
                        else
                            block.SetVector(mat.ID_subtex_ST, new Vector4((sx4 - sx3) * aw, (sy4 - sy3) * ah, sx3 * aw, sy3 * ah));
                    }
                    break;
                case UVSliderMode.DirectUV:
                    // main texture
                    {
                        if (InvertY)
                            block.SetVector(mat.ID_maintex_ST, new Vector4(sx2, sy2, sx1, 1.0f - sy1));
                        else
                            block.SetVector(mat.ID_maintex_ST, new Vector4(sx2, sy2, sx1, sy1));
                    }
                    // sub texture
                    {
                        if (InvertY)
                            block.SetVector(mat.ID_subtex_ST, new Vector4(sx4, sy4, sx3, 1.0f - sy3));
                        else
                            block.SetVector(mat.ID_subtex_ST, new Vector4(sx4, sy4, sx3, sy3));
                    }
                    break;
            } 
        }
        public void GetUV(Vector2[] uv, ALMaterial mat)
        {
            if (mat == null) return;
            if (uv.Length < 8) return;

            switch (Mode)
            {
                case UVSliderMode.Area:
                    // main texture
                    {
                        var size = mat.GetVirtualMainSize();
                        var aw = 1.0f / size.x;
                        var ah = 1.0f / size.y;
                        if (InvertY)
                        {
                            uv[0].x = sx1 * aw;
                            uv[0].y = 1.0f - sy1 * ah;
                            uv[2].x = sx2 * aw;
                            uv[2].y = 1.0f - sy2 * ah;
                        }
                        else
                        {
                            uv[0].x = sx1 * aw;
                            uv[0].y = sy1 * ah;
                            uv[2].x = sx2 * aw;
                            uv[2].y = sy2 * ah;
                        }
                        uv[1].x = uv[2].x;
                        uv[1].y = uv[0].y;
                        uv[3].x = uv[0].x;
                        uv[3].y = uv[2].y;
                    }
                    // sub texture
                    {
                        var size = mat.GetVirtualSubSize();
                        var aw = 1.0f / size.x;
                        var ah = 1.0f / size.y;
                        if (InvertY)
                        {
                            uv[4].x = sx1 * aw;
                            uv[4].y = sy1 * ah;
                            uv[6].x = sx2 * aw;
                            uv[6].y = sy2 * ah;
                        }
                        else
                        {
                            uv[4].x = sx1 * aw;
                            uv[4].y = 1.0f - sy1 * ah;
                            uv[6].x = sx2 * aw;
                            uv[6].y = 1.0f - sy2 * ah;
                        }
                        uv[5].x = uv[6].x;
                        uv[5].y = uv[4].y;
                        uv[7].x = uv[4].x;
                        uv[7].y = uv[6].y;
                    }
                    break;
                case UVSliderMode.DirectUV:
                    // main texture
                    {
                        uv[0].x = sx1;
                        uv[0].y = sy1;
                        uv[2].x = uv[0].x + 1.0f * sx2;
                        uv[2].y = uv[0].y + 1.0f * sy2;
                        uv[1].x = uv[2].x;
                        uv[1].y = uv[0].y;
                        uv[3].x = uv[0].x;
                        uv[3].y = uv[2].y;
                    }
                    // sub texture
                    {
                        uv[4].x = sx1;
                        uv[4].y = sy1;
                        uv[6].x = uv[4].x + 1.0f * sx2;
                        uv[6].y = uv[4].y + 1.0f * sy2;
                        uv[5].x = uv[6].x;
                        uv[5].y = uv[4].y;
                        uv[7].x = uv[4].x;
                        uv[7].y = uv[6].y;
                    }
                    break;
            }
        }
    }
    // plane controler struct
    public enum PlaneSettingMode
    {
        Area,       // w,h = dx2-dx1,dy2-dy1
        Size,       // w,h = dx2,dy2
        SrcScale     // w,h = dx2*(sx2-sx1),dy2*(sy2-sy1)
    }
    public struct PlaneSetting
    {
        public static float SrcScaleRatio = 0.01f; // 1pixel = 0.01xyz on Unity
        public static PlaneSettingMode DefaultDestMode = PlaneSettingMode.Size;
        public static PlaneSettingMode DefaultSrcMode = PlaneSettingMode.Size;
        public static float DefaultAlignX = 0.5f;
        public static float DefaultAlignY = 0.5f;

        public PlaneSettingMode DestMode,SrcMode;

        public float dx1, dy1;
        public float dx2, dy2;
        public float dz1;
        public float drx, dry, drz;
        public float sx1, sy1;
        public float sx2, sy2;
        public float alignx, aligny;
        // sub texture
        public float sx3, sy3;
        public float sx4, sy4;

        public void Clear()
        {
            DestMode = DefaultDestMode;
            SrcMode = DefaultSrcMode;

            alignx = DefaultAlignX;
            aligny = DefaultAlignY;

            dx1 = 0;
            dy1 = 0;
            dz1 = 0;

            dx2 = 0;
            dy2 = 0;

            drx = 0;
            dry = 0;
            drz = 0;

            sx1 = 0;
            sy1 = 0;
            sx2 = 0;
            sy2 = 0;

            sx3 = 0;
            sy3 = 0;
            sx4 = 0;
            sy4 = 0;
        }

        public Matrix4x4 ComputeMatrix()
        {
            // w,h
            float w = 1, h = 1;
            Matrix4x4 m = Matrix4x4.identity;
            switch (DestMode)
            {
                case PlaneSettingMode.Area:
                    w = dx2 - dy1;
                    h = dy2 - dy1;
                    // align
                    Misc.m4PushMove(ref m, -alignx, -aligny, 0);
                    Misc.m4PushScale(ref m, w, h, 1);
                    if (drz != 0) Misc.m4PushRotateZ(ref m, drz);
                    if (dry != 0) Misc.m4PushRotateY(ref m, dry);
                    if (drx != 0) Misc.m4PushRotateX(ref m, drx);
                    Misc.m4PushMove(ref m, +alignx, +aligny, 0);    // revert 
                    Misc.m4PushMove(ref m, dx1, dy1, dz1);
                    break;
                case PlaneSettingMode.Size:
                    w = dx2;
                    h = dy2;
                    Misc.m4PushMove(ref m, -alignx, -aligny, 0);
                    Misc.m4PushScale(ref m, w, h, 1);
                    if (drz != 0) Misc.m4PushRotateZ(ref m, drz);
                    if (dry != 0) Misc.m4PushRotateY(ref m, dry);
                    if (drx != 0) Misc.m4PushRotateX(ref m, drx);
                    Misc.m4PushMove(ref m, dx1, dy1, dz1);
                    break;
                case PlaneSettingMode.SrcScale:
                    switch (SrcMode)
                    {
                        case PlaneSettingMode.Area:
                            w = sx2 - sx1;
                            h = sy2 - sy1;
                            break;
                        case PlaneSettingMode.Size:
                        case PlaneSettingMode.SrcScale:
                            w = sx2;
                            h = sy2;
                            break;
                    }
                    w = dx2 * w * SrcScaleRatio;
                    h = dy2 * h * SrcScaleRatio;
                    Misc.m4PushMove(ref m, -alignx, -aligny, 0);
                    Misc.m4PushScale(ref m, w, h, 1);
                    if (drz != 0) Misc.m4PushRotateZ(ref m, drz);
                    if (dry != 0) Misc.m4PushRotateY(ref m, dry);
                    if (drx != 0) Misc.m4PushRotateX(ref m, drx);
                    Misc.m4PushMove(ref m, dx1, dy1, dz1);
                    break;
            }
            return m;
        }
        public UVSlider ComputeUV()
        {
            var slider = new UVSlider();
            slider.Mode = UVSliderMode.Area;
            switch (SrcMode)
            {
                case PlaneSettingMode.Area:
                    slider.sx1 = sx1;
                    slider.sy1 = sy1;
                    slider.sx2 = sx2;
                    slider.sy2 = sy2;
                    slider.sx3 = sx3;
                    slider.sy3 = sy3;
                    slider.sx4 = sx4;
                    slider.sy4 = sy4;
                    break;
                case PlaneSettingMode.Size:
                case PlaneSettingMode.SrcScale:
                    slider.sx1 = sx1;
                    slider.sy1 = sy1;
                    slider.sx2 = sx1 + sx2;
                    slider.sy2 = sy1 + sy2;
                    slider.sx3 = sx3;
                    slider.sy3 = sy3;
                    slider.sx4 = sx3 + sx4;
                    slider.sy4 = sy3 + sy4;
                    break;
            }
            return slider;
        }
    }
    // alphalib main renderer
    public class LogData
    {
        public Matrix4x4 matrix;
        public Material[] materials = new Material[4];
        public MaterialPropertyBlock block = new MaterialPropertyBlock();

        //public UnityEngine.Mesh dynamicmesh = null;
        public UnityEngine.Mesh refmesh = null;

        public int logindex = 0;
        public float alpha = 1.0f;    // alpha
        public Vector3 sortpoint = new Vector3(0, 0, 0);
        public float priority = 0.0f;
        public double sortvalue;


        public void ClearRef()
        {
            materials[0] = null;
            materials[1] = null;
            materials[2] = null;
            materials[3] = null;

            //dynamicmesh = null;
            refmesh = null;
        }
    }
    public class LogDataComp : IComparer<LogData>
    {
        public int Compare(LogData x, LogData y)
        {
            if (x.sortvalue > y.sortvalue) return -1;
            if (x.sortvalue < y.sortvalue) return +1;

            return 0;
        }
    }
    public class Renderer
    {
        // log data
        public int Count { get { return Logs.Count; } }
        public int Capacity;
        public LogStackArray<LogData> Logs;
        protected void ClearLogs(bool initialize)
        {
            Logs.Clear(initialize);
        }
        protected Texture2D NullWhite(Texture2D tex)
        {
            if (tex == null) return Texture2D.whiteTexture;
            return tex;
        }
        // cache
        public Mesh PlaneMesh;
        public Mesh DummyModel;
        // constructor
        public Renderer(int capacity = 32)
        {
            Capacity = capacity;
            Logs = new LogStackArray<LogData>(capacity);

            // load shaders / materials (singleton)
            Misc.Setup();

            PlaneMesh = new Mesh();
            Misc.CreatePlaneMesh(PlaneMesh, 1.0f, false);

            DummyModel = new Mesh();
            Misc.CreatePlaneMesh(DummyModel, 1.0f, true);

            ClearLogs(true);
        }
        // basic
        protected bool NextClearFlag = false;
        public void Clear(bool capacityclear = false)
        {
            ClearLogs(capacityclear);
            NextClearFlag = false;
        }
        public void NextClear()
        {
            NextClearFlag = true;
        }
        public void Update()
        {
            // do nextclear
            if (NextClearFlag) Clear();
        }
        public void DoSort(ref Matrix4x4 m)
        {
            // calc sort value
            var n = Count;
            for (var i = 0; i < n; i++)
            {
                var log = Logs.Data[i];

                var v = Misc.v3Transform(ref log.sortpoint, ref log.matrix);
                v = Misc.v3Transform(ref v, ref m);

                log.sortvalue = log.priority * 65536 + v.z;
            }

            // sort
            Array.Sort<LogData>(Logs.Data, 0, Logs.Count, new LogDataComp());
        }
        // draw
        public LogData DrawMesh(ref Matrix4x4 m, Mesh mesh, Material mat)
        {
            Material[] mats = new Material[4];

            mats[0] = mat;
            mats[1] = mat;
            mats[2] = mat;
            mats[3] = mat;

            return DrawMesh(ref m, mesh, mats);
        }
        public LogData DrawMesh(ref Matrix4x4 m, Mesh mesh, Material[] mats)
        {
            // next clear
            if (NextClearFlag) Clear();

            var log = Logs.GetTop();

            log.matrix = m; // hard copy

            {
                var n = mats.Length;
                if (n > 0) log.materials[0] = mats[0];
                if (n > 1) log.materials[1] = mats[1];
                if (n > 2) log.materials[2] = mats[2];
                if (n > 3) log.materials[3] = mats[3];
            }

            //log.dynamicmesh = null;
            log.refmesh = mesh;

            log.logindex = Count;
            log.alpha = 1.0f;
            log.sortpoint = new Vector3(0, 0, 0);

            Logs.Commit();

            log.block.Clear();
            return log;
        }
        public LogData DrawMesh(ref Matrix4x4 m, Mesh mesh, ALMaterial mat)
        {
            return DrawMesh(ref m, mesh, mat, Color.white, Color.black);
        }
        public LogData DrawMesh(ref Matrix4x4 m, Mesh mesh, ALMaterial mat, Color color, Color tintcolor)
        {
            // next clear
            if (NextClearFlag) Clear();
            if (mesh == null) return null;
            if (mesh.vertexCount == 0) return null;

            var log = Logs.GetTop();

            log.block.Clear();
            if (mat.MainTexture != null) log.block.SetTexture(mat.ID_maintex, mat.MainTexture);
            if (mat.SubTexture != null) log.block.SetTexture(mat.ID_subtex, mat.SubTexture);

            log.matrix = m; // hard copy
            log.materials[0] = mat.Material;
            log.materials[1] = mat.Material;
            log.materials[2] = mat.Material;
            log.materials[3] = mat.Material;

            if (mesh == null) mesh = DummyModel;
            log.refmesh = mesh;

            log.logindex = Count;
            log.alpha = color.a;
            log.sortpoint = new Vector3(0, 0, 0);

            Logs.Commit();
            return log;
        }
        public LogData DrawMeshSlider(
            ref Matrix4x4 m, 
            Mesh mesh, 
            ALMaterial mat, 
            ref UVSlider slider,
            Single multiOption = 0.0f
        ) {
            return DrawMeshSlider(ref m, mesh, mat, ref slider, Color.white, Color.black, multiOption);
        }
        public LogData DrawMeshSlider(
            ref Matrix4x4 m, 
            Mesh mesh, 
            ALMaterial mat, 
            ref UVSlider slider, 
            Color color, Color tintcolor,
            Single multiOption = 0.0f,
            Single multiInverse = 0.0f
        ) {
            // next clear
            if (NextClearFlag) Clear();
            if (mesh == null) return null;
            if (mesh.vertexCount == 0) return null;

            var log = Logs.GetTop();

            log.block.Clear();
            if (mat.MainTexture != null) log.block.SetTexture(mat.ID_maintex, mat.MainTexture);
            if (mat.SubTexture != null) log.block.SetTexture(mat.ID_subtex, mat.SubTexture);

            log.block.SetColor(mat.ID_color, color);
            log.block.SetColor(mat.ID_tintcolor, tintcolor);
            log.block.SetFloat(mat.ID_option, multiOption);
            log.block.SetFloat(mat.ID_invsubtex, multiInverse);

            slider.SetPropertyBlock(mat,log.block);

            log.matrix = m; // hard copy
            log.materials[0] = mat.Material;
            log.materials[1] = mat.Material;
            log.materials[2] = mat.Material;
            log.materials[3] = mat.Material;

            //log.dynamicmesh = null;
            if (mesh == null) mesh = DummyModel;
            log.refmesh = mesh;

            log.logindex = Count;
            log.alpha = color.a;
            log.sortpoint = new Vector3(0, 0, 0);

            Logs.Commit();
            return log;
        }
        public LogData DrawPlane(
            ALMaterial mat,
            ref PlaneSetting setting,
            Single multiOption = 0.0f,
            Single multiInverse = 0.0f
        ) {
            return DrawPlane(mat, ref setting, Color.white, Color.black, multiOption, multiInverse);
        }
        public LogData DrawPlane(
            ALMaterial mat,
            ref PlaneSetting setting,
            Color color,
            Color tintcolor,
            Single multiOption = 0.0f,
            Single multiInverse = 0.0f
        ) {
            Matrix4x4 m = setting.ComputeMatrix();
            UVSlider slider = setting.ComputeUV();
            return DrawMeshSlider(ref m, PlaneMesh, mat, ref slider, color, tintcolor, multiOption, multiInverse);
        }

    }
}

