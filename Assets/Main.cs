using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

namespace myapp
{
    enum TestMode {
        BlendTest,
        DrawPlane,
        BatchPlane,
        DrawMesh,
        BatchMesh,
        DrawSprite,
        BatchSprite
    }

    public class Main : MonoBehaviour
    {
        private int DrawCount = 100;
        private TestMode SelectTestMode = TestMode.BlendTest;
        private AlphaLib.BlendMode SelectBlendMode = AlphaLib.BlendMode.Normal;

        private AlphaLib.Renderer AL;
        private AlphaLib.ALMaterial SrcMaterial;
        private Texture2D MainTex, SubTex;
        private Mesh SrcMesh;
        // multi texture
        private AlphaLib.ALMaterial MultiBlendMaterial, MultiMaskMaterial, MultiFadeMaterial, MultiOverlapMaterial, MultiExtractMaterial;
        // batcher
        private AlphaLib.MeshBatchData SrcBatchMesh;
        private AlphaLib.MeshBatcherSortable Batcher = new AlphaLib.MeshBatcherSortable();
        // misc
        private System.Random rnd = new System.Random();
        // gui
        private bool EventLock = false;

        // Start is called before the first frame update
        void Start()
        {
            // AlphaLib
            {
                var c = gameObject.GetComponent<AlphaLib.AlphaLibMeshRenderer>();
                AL = c.ALRenderer;
            }

            // load texture
            MainTex = Resources.Load<Texture2D>("sample");
            SubTex = Resources.Load<Texture2D>("mask");

            EventLock = true;

            // blend
            {
                var o = GameObject.Find("BlendDropdown");
                var c = o.GetComponent<Dropdown>();
                c.options.Clear();
                c.options.Add(new Dropdown.OptionData(AlphaLib.BlendMode.Normal.ToString()));
                c.options.Add(new Dropdown.OptionData(AlphaLib.BlendMode.Screen.ToString()));
                c.options.Add(new Dropdown.OptionData(AlphaLib.BlendMode.Add.ToString()));
                //c.options.Add(new Dropdown.OptionData("ColorDodge"));
                c.options.Add(new Dropdown.OptionData(AlphaLib.BlendMode.Max.ToString()));
                c.options.Add(new Dropdown.OptionData(AlphaLib.BlendMode.Multiply.ToString()));
                c.options.Add(new Dropdown.OptionData(AlphaLib.BlendMode.Sub.ToString()));
                //c.options.Add(new Dropdown.OptionData("InvSub"));
                //c.options.Add(new Dropdown.OptionData("ColorBurn"));
                c.options.Add(new Dropdown.OptionData(AlphaLib.BlendMode.Min.ToString()));
                c.options.Add(new Dropdown.OptionData(AlphaLib.BlendMode.LinearLight.ToString()));
                c.options.Add(new Dropdown.OptionData(AlphaLib.BlendMode.Mod2.ToString()));
                c.options.Add(new Dropdown.OptionData(AlphaLib.BlendMode.Pow2.ToString()));
                c.options.Add(new Dropdown.OptionData(AlphaLib.BlendMode.Exclusion.ToString()));
                c.value = 0;
                c.RefreshShownValue();
            }

            // blend
            {
                var o = GameObject.Find("TestDropdown");
                var c = o.GetComponent<Dropdown>();
                c.options.Clear();
                c.options.Add(new Dropdown.OptionData(TestMode.BlendTest.ToString()));
                c.options.Add(new Dropdown.OptionData(TestMode.DrawPlane.ToString()));
                c.options.Add(new Dropdown.OptionData(TestMode.BatchPlane.ToString()));
                c.options.Add(new Dropdown.OptionData(TestMode.DrawMesh.ToString()));
                c.options.Add(new Dropdown.OptionData(TestMode.BatchMesh.ToString()));
                c.options.Add(new Dropdown.OptionData(TestMode.DrawSprite.ToString()));
                c.options.Add(new Dropdown.OptionData(TestMode.BatchSprite.ToString()));
                c.value = 0;
                c.RefreshShownValue();
            }

            EventLock = false;
        }

        // Update is called once per frame
        void Update()
        {
            /*
            {
                // unity camera sorting test
                var o = GameObject.Find("MainCamera");
                var v = o.transform.position;
                v.x = 1.0f - ((int)(Time.time * 1000) % 2000) / 1000.0f;
                o.transform.position = v;
            }
            */

            switch(SelectTestMode){
                case TestMode.BlendTest:
                    BlendTest();
                    break;
                case TestMode.DrawPlane:
                    DrawPlane();
                    break;
                case TestMode.BatchPlane:
                    BatchPlane();
                    break;
                case TestMode.DrawMesh:
                    DrawMesh();
                    break;
                case TestMode.BatchMesh:
                    BatchMesh();
                    break;
                case TestMode.DrawSprite:
                    DrawSprite();
                    break;
                case TestMode.BatchSprite:
                    BatchSprite();
                    break;
            } 
        }

        // event
        public void BlendDropdownOnSelect(Dropdown sender)
        {
            if (EventLock) return;

            var s = sender.options[sender.value].text;
            var e = (AlphaLib.BlendMode)Enum.Parse(typeof(AlphaLib.BlendMode), s, true);

            SelectBlendMode = e;
            TestReset();
        }
        public void TestDropdownOnSelect(Dropdown sender)
        {
            if (EventLock) return;

            var s = sender.options[sender.value].text;
            var t = (TestMode)Enum.Parse(typeof(TestMode), s, true);

            SelectTestMode = t;
            TestReset();
        }
        public void DrawCountDropdownOnSelect(Dropdown sender)
        {
            if (EventLock) return;

            var s = sender.options[sender.value].text;
            DrawCount = Convert.ToInt32(s); 
        }
        public void TestReset()
        {
            // object refer kill
            AL.Clear(true);

            // clear resource
            SrcMaterial = null;
            SrcMesh = null;
            SrcBatchMesh = null;

            MultiBlendMaterial = null;
            MultiMaskMaterial = null;
            MultiFadeMaterial = null; 
            MultiOverlapMaterial = null;
            MultiExtractMaterial = null;
        }

        // test
        void UnitySortingTest()
        {
            // setup
            if (SrcMaterial == null)
            {
                SrcMaterial = AlphaLib.Misc.CreateMaterial(SelectBlendMode, AlphaLib.ShaderMode.Single, MainTex, null);
                SrcMaterial.SetVirtualSize(256, 256);
            }

            var m = Matrix4x4.identity;

            // plane setting
            var ps = new AlphaLib.PlaneSetting();
            ps.Clear();
            // dest
            ps.DestMode = AlphaLib.PlaneSettingMode.Size;
            ps.alignx = 0.5f;
            ps.aligny = 0.5f;
            ps.drz = 30;
            ps.dx1 = 0;
            ps.dy1 = 0;
            ps.dz1 = 0;
            ps.dx2 = 0.9f;
            ps.dy2 = 0.9f;
            // src
            ps.SrcMode = AlphaLib.PlaneSettingMode.Area;
            ps.sx1 = 0;
            ps.sy1 = 0;
            ps.sx2 = 256;
            ps.sy2 = 256;
            ps.sx3 = 0;
            ps.sy3 = 0;
            ps.sx4 = 256;
            ps.sy4 = 256;

            // colors test
            {
                ps.dx1 = 0.7f * 0;
                ps.dy1 = 4.0f;
                AL.DrawPlane(SrcMaterial, ref ps, new Color(1, 1, 1, 1), new Color(0, 0, 0, 0));

                ps.dx1 = 0.7f * 1;
                AL.DrawPlane(SrcMaterial, ref ps, new Color(1, 0, 0, 1), new Color(0, 0, 0, 0));

                ps.dx1 = 0.7f * 2;
                AL.DrawPlane(SrcMaterial, ref ps, new Color(0, 1, 0, 1), new Color(0, 0, 0, 0));

                ps.dx1 = 0.7f * 3;
                AL.DrawPlane(SrcMaterial, ref ps, new Color(0, 0, 1, 1), new Color(0, 0, 0, 0));

                ps.dx1 = 0.7f * 4;
                AL.DrawPlane(SrcMaterial, ref ps, new Color(0, 0, 0, 1), new Color(1, 1, 1, 0));
            }

        }

        void BlendTest()
        {
            // setup
            if (SrcMaterial == null)
            {
                SrcMaterial = AlphaLib.Misc.CreateMaterial(SelectBlendMode, AlphaLib.ShaderMode.Single, MainTex, null);
                SrcMaterial.SetVirtualSize(256, 256, 256, 256);

                MultiBlendMaterial = AlphaLib.Misc.CreateMaterial(SelectBlendMode, AlphaLib.ShaderMode.MultiBlend, MainTex, SubTex);
                MultiBlendMaterial.SetVirtualSize(256, 256, 256, 256);

                MultiMaskMaterial = AlphaLib.Misc.CreateMaterial(SelectBlendMode, AlphaLib.ShaderMode.MultiMask, MainTex, SubTex);
                MultiMaskMaterial.SetVirtualSize(256, 256, 256, 256);

                MultiFadeMaterial = AlphaLib.Misc.CreateMaterial(SelectBlendMode, AlphaLib.ShaderMode.MultiFade, MainTex, SubTex);
                MultiFadeMaterial.SetVirtualSize(256, 256, 256, 256);

                MultiOverlapMaterial = AlphaLib.Misc.CreateMaterial(SelectBlendMode, AlphaLib.ShaderMode.MultiOverlap, MainTex, SubTex);
                MultiOverlapMaterial.SetVirtualSize(256, 256, 256, 256);

                MultiExtractMaterial = AlphaLib.Misc.CreateMaterial(SelectBlendMode, AlphaLib.ShaderMode.MultiExtract, MainTex, SubTex);
                MultiExtractMaterial.SetVirtualSize(256, 256, 256, 256);
            }

            var m = Matrix4x4.identity;

            // plane setting
            var ps = new AlphaLib.PlaneSetting();
            ps.Clear();
            // dest
            ps.DestMode = AlphaLib.PlaneSettingMode.Size;
            ps.alignx = 0.5f;
            ps.aligny = 0.5f;
            ps.drz = 30;
            ps.dx1 = 0;
            ps.dy1 = 0;
            ps.dz1 = 0;
            ps.dx2 = 0.9f;
            ps.dy2 = 0.9f;
            // src
            ps.SrcMode = AlphaLib.PlaneSettingMode.Area;
            ps.sx1 = 0;
            ps.sy1 = 0;
            ps.sx2 = 256;
            ps.sy2 = 256;
            ps.sx3 = 0;
            ps.sy3 = 0;
            ps.sx4 = 256;
            ps.sy4 = 256;

            // alpha 2
            float alpha2 = (int)(Time.time * 500) % 2000;
            alpha2 = Math.Abs((alpha2 - 1000.0f) / 1000.0f);

            // colors test
            {             
                ps.dx1 = 0.7f * 0;
                ps.dy1 = 4.2f;
                AL.DrawPlane(SrcMaterial, ref ps, new Color(1, 1, 1, 1), new Color(0, 0, 0, 0));

                ps.dx1 = 0.7f * 1;
                AL.DrawPlane(SrcMaterial, ref ps, new Color(1, 0, 0, 1), new Color(0, 0, 0, 0));

                ps.dx1 = 0.7f * 2;
                AL.DrawPlane(SrcMaterial, ref ps, new Color(0, 1, 0, 1), new Color(0, 0, 0, 0));

                ps.dx1 = 0.7f * 3;
                AL.DrawPlane(SrcMaterial, ref ps, new Color(0, 0, 1, 1), new Color(0, 0, 0, 0));

                ps.dx1 = 0.7f * 4;
                AL.DrawPlane(SrcMaterial, ref ps, new Color(0, 0, 0, 1), new Color(1, 1, 1, 0));
            }

            // multi texture
            {
                var color = Color.white;
                var tint = new Color(0, 0, 0, alpha2);

                AlphaLib.LogData log;

                // blend
                ps.dx1 = 2.4f * 0;
                ps.dy1 = 3.0f;
                AL.DrawPlane(MultiBlendMaterial, ref ps, color,tint);
                ps.dx1 = ps.dx1 + 0.9f;
                log = AL.DrawPlane(MultiBlendMaterial, ref ps, color, tint, 0.0f, 1.0f);

                ps.dx1 = 2.4f * 1;
                AL.DrawPlane(MultiMaskMaterial, ref ps, color, tint);
                ps.dx1 = ps.dx1 + 0.9f;
                log = AL.DrawPlane(MultiMaskMaterial, ref ps, color, tint, 0.0f, 1.0f);

                ps.dx1 = 2.4f * 2;
                log = AL.DrawPlane(MultiFadeMaterial, ref ps, color, tint, 0.5f);
                ps.dx1 = ps.dx1 + 0.9f;
                log = AL.DrawPlane(MultiFadeMaterial, ref ps, color, tint, 0.5f, 1.0f);

                ps.dx1 = 2.4f * 0;
                ps.dy1 = 1.8f;
                log = AL.DrawPlane(MultiOverlapMaterial, ref ps, color, tint);
                ps.dx1 = ps.dx1 + 0.9f;
                log = AL.DrawPlane(MultiOverlapMaterial, ref ps, color, tint, 0.0f, 1.0f);

                ps.dx1 = 2.4f * 1;
                log = AL.DrawPlane(MultiExtractMaterial, ref ps, color, tint, 0.5f);
                ps.dx1 = ps.dx1 + 0.9f;
                log = AL.DrawPlane(MultiExtractMaterial, ref ps, color, tint, 0.5f, 1.0f);
            }

        }

        void DrawPlane()
        {
            // setup
            if (SrcMaterial == null)
            {
                SrcMaterial = AlphaLib.Misc.CreateMaterial(SelectBlendMode, AlphaLib.ShaderMode.Single, MainTex, null);
                SrcMaterial.SetVirtualSize(256, 256);
            }

            var m = Matrix4x4.identity;

            for (var i = 0; i < DrawCount; i++)
            {
                var color = new Color((float)rnd.NextDouble(), (float)rnd.NextDouble(), (float)rnd.NextDouble(), 1);
                var tintcolor = new Color(0, 0, 0, 0);

                // plane setting
                var ps = new AlphaLib.PlaneSetting();
                ps.Clear();
                // dest
                ps.DestMode = AlphaLib.PlaneSettingMode.Size;
                ps.alignx = 0.5f;
                ps.aligny = 0.5f;
                ps.drz = Time.time / 2 * 360;
                ps.dx1 = (float)rnd.NextDouble() * 4.0f;
                ps.dy1 = (float)rnd.NextDouble() * 4.0f;
                ps.dx2 = 1.0f;
                ps.dy2 = 1.0f;
                ps.dz1 = (float)rnd.NextDouble() * 5.0f; 
                // src
                ps.SrcMode = AlphaLib.PlaneSettingMode.Area;
                ps.sx1 = 0;
                ps.sy1 = 0;
                ps.sx2 = 256;
                ps.sy2 = 256;

                AL.DrawPlane(SrcMaterial, ref ps, color, tintcolor);
            }
        }
        // batch test
        void BatchPlane()
        {
            // setup
            if (SrcMaterial == null)
            {
                SrcMaterial = AlphaLib.Misc.CreateMaterial(SelectBlendMode, AlphaLib.ShaderMode.SingleBatch, MainTex, null);
                SrcMaterial.SetVirtualSize(256, 256);
            }

            var m = Matrix4x4.identity;

            for (var i = 0; i < DrawCount; i++)
            {
                var color = new Color((float)rnd.NextDouble(), (float)rnd.NextDouble(), (float)rnd.NextDouble(), 1);
                var tintcolor = new Color(0, 0, 0, 0);

                // plane setting
                var ps = new AlphaLib.PlaneSetting();
                ps.Clear();
                // dest
                ps.DestMode = AlphaLib.PlaneSettingMode.Size;
                ps.alignx = 0.5f;
                ps.aligny = 0.5f;
                ps.drz = Time.time / 2 * 360;
                ps.dx1 = (float)rnd.NextDouble() * 4.0f;
                ps.dy1 = (float)rnd.NextDouble() * 4.0f;
                ps.dx2 = 1.0f;
                ps.dy2 = 1.0f;
                ps.dz1 = (float)rnd.NextDouble() * 5.0f;
                // src
                ps.SrcMode = AlphaLib.PlaneSettingMode.Area;
                ps.sx1 = 0;
                ps.sy1 = 0;
                ps.sx2 = 256;
                ps.sy2 = 256;

                Batcher.DrawPlane(ref ps, SrcMaterial, color, tintcolor, 0.0f);
            }

            Batcher.Build();

            AL.DrawMesh(ref m, Batcher.Mesh, SrcMaterial);
        }
        // mesh draw test
        void DrawMesh()
        {
            // setup
            if (SrcMaterial == null)
            {
                SrcMaterial = AlphaLib.Misc.CreateMaterial(SelectBlendMode, AlphaLib.ShaderMode.Single, MainTex, null);
                SrcMaterial.SetVirtualSize(256, 256);
            }

            // get mesh & material from "cube" GameObject.
            var g = GameObject.Find("Cube");
            var mesh = g.GetComponent<MeshFilter>().sharedMesh;
            var mats = g.GetComponent<MeshRenderer>().sharedMaterials;
            var id = Shader.PropertyToID("_Color");

            for (var i = 0; i < DrawCount; i++)
            {
                var m = Matrix4x4.identity;
                // size
                AlphaLib.Misc.m4PushScale(ref m, 0.3f, 0.3f, 0.3f);
                // rotate
                AlphaLib.Misc.m4PushRotateZ(ref m, (float)rnd.NextDouble() * 360);
                // move
                AlphaLib.Misc.m4PushMove(ref m, (float)rnd.NextDouble() * 4.0f, (float)rnd.NextDouble() * 4.0f, (float)rnd.NextDouble() * 5.0f);

                var color = new Color((float)rnd.NextDouble(), (float)rnd.NextDouble(), (float)rnd.NextDouble(), 1);
                var tintcolor = new Color(0, 0, 0, 0);

                // draw mesh
                var log = AL.DrawMesh(ref m, mesh, mats);
                // set returned MaterialPropertyBlock
                log.block.SetColor(id, color);
            }
        }
        void BatchMesh()
        {
            var m = Matrix4x4.identity;

            // get mesh & material from "sprite" GameObject.
            var g = GameObject.Find("Cube");
            var cube = g.GetComponent<MeshFilter>().sharedMesh;
            var mat = g.GetComponent<MeshRenderer>().sharedMaterial;
            // make mesh
            if (SrcMesh == null)
            {
                SrcBatchMesh = new AlphaLib.MeshBatchData(cube);
                SrcMaterial = AlphaLib.Misc.CreateMaterial(SelectBlendMode, AlphaLib.ShaderMode.SingleBatch, null, null);
            }

            for (var i = 0; i < DrawCount; i++)
            {
                m = Matrix4x4.identity;
                // size
                AlphaLib.Misc.m4PushScale(ref m, 0.3f, 0.3f, 0.3f);
                // rotate
                AlphaLib.Misc.m4PushRotateZ(ref m, (float)rnd.NextDouble() * 360);
                // move
                AlphaLib.Misc.m4PushMove(ref m, (float)rnd.NextDouble() * 4.0f, (float)rnd.NextDouble() * 4.0f, (float)rnd.NextDouble() * 5.0f);

                var color = new Color((float)rnd.NextDouble(), (float)rnd.NextDouble(), (float)rnd.NextDouble(), 1);
                var tintcolor = new Color(0, 0, 0, 0);

                Batcher.DrawMesh(ref m, SrcBatchMesh, color, tintcolor, 0.0f);
            }

            Batcher.Build();

            m = Matrix4x4.identity;
            AL.DrawMesh(ref m, Batcher.Mesh, SrcMaterial);
        }
        // sprite draw test
        void DrawSprite()
        {
            // get mesh & material from "sprite" GameObject.
            var g = GameObject.Find("Sprite");
            var sprite = g.GetComponent<SpriteRenderer>().sprite;
            var mat = g.GetComponent<SpriteRenderer>().sharedMaterials;
            // make mesh
            if (SrcMesh == null)
            {
                SrcMesh = AlphaLib.Misc.SpriteToMesh(sprite);
                SrcMaterial = AlphaLib.Misc.CreateMaterial(SelectBlendMode, AlphaLib.ShaderMode.Single, sprite.texture, null);
            }

            for (var i = 0; i < DrawCount; i++)
            {
                var m = Matrix4x4.identity;
                // size
                AlphaLib.Misc.m4PushScale(ref m, 0.5f, 0.5f, 0.5f);
                // rotate
                AlphaLib.Misc.m4PushRotateZ(ref m, (float)rnd.NextDouble() * 360);
                // move
                AlphaLib.Misc.m4PushMove(ref m, (float)rnd.NextDouble() * 4.0f, (float)rnd.NextDouble() * 4.0f, (float)rnd.NextDouble() * 5.0f);

                var color = new Color((float)rnd.NextDouble(), (float)rnd.NextDouble(), (float)rnd.NextDouble(), 1);
                var tintcolor = new Color(0, 0, 0, 0);

                AL.DrawMesh(ref m, SrcMesh, SrcMaterial, color, tintcolor);
                /*
                // draw mesh
                var log = AL.DrawMesh(ref m, SampleSpriteMesh, mat);
                // set returned MaterialPropertyBlock
                log.block.SetTexture("_MainTex", sprite.texture);
                log.block.SetColor("_Color", color);
                */
            }
        }
        // sprite batch test
        void BatchSprite()
        {
            var m = Matrix4x4.identity;

            // get mesh & material from "sprite" GameObject.
            var g = GameObject.Find("Sprite");
            var sprite = g.GetComponent<SpriteRenderer>().sprite;
            var mat = g.GetComponent<SpriteRenderer>().material;
            // make mesh
            if (SrcMesh == null)
            {
                SrcBatchMesh= new AlphaLib.MeshBatchData(sprite);
                SrcMaterial = AlphaLib.Misc.CreateMaterial(SelectBlendMode, AlphaLib.ShaderMode.SingleBatch, sprite.texture, null);
            }

            for (var i = 0; i < DrawCount; i++)
            {
                m = Matrix4x4.identity;
                // size
                AlphaLib.Misc.m4PushScale(ref m, 0.5f, 0.5f, 0.5f);
                // rotate
                AlphaLib.Misc.m4PushRotateZ(ref m, (float)rnd.NextDouble() * 360);
                // move
                AlphaLib.Misc.m4PushMove(ref m, (float)rnd.NextDouble() * 4.0f, (float)rnd.NextDouble() * 4.0f, (float)rnd.NextDouble()*5.0f);

                var color = new Color((float)rnd.NextDouble(), (float)rnd.NextDouble(), (float)rnd.NextDouble(), 1);
                var tintcolor = new Color(0, 0, 0, 0);

                Batcher.DrawMesh(ref m, SrcBatchMesh, color, tintcolor, 0.0f);
            }

            Batcher.Build();

            m = Matrix4x4.identity;
            AL.DrawMesh(ref m, Batcher.Mesh, SrcMaterial);
        }
    }
}
