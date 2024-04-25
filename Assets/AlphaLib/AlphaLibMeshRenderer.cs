using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AlphaLib
{
    public enum RenderingMode
    {
        DrawMesh,
        DrawMeshNow
    }
    [AddComponentMenu("AlphaLib/AlphaLib MeshRenderer")]
    public class AlphaLibMeshRenderer : MonoBehaviour
    {
        protected AlphaLib.Renderer fALRenderer = null;
        protected MeshRenderer fMeshRenderer = null;
        protected MeshFilter fMeshFilter = null;

        [Tooltip("Set rendering camera. null is all. When multi cameras, do multi rendering and sorting.")]
        public Camera ActiveCamera = null;
        [Tooltip("Rendering mode. DrawMesh : Cooperate with unity system. DrawMeshNow : unsupport now.")]
        protected RenderingMode Mode = RenderingMode.DrawMesh;
        [Tooltip("Clear draw log per frame. false : you need to call Clear() function.")]
        public bool AutoClearPerFrame = true;
        [Tooltip("Draw object starting cache size. Increase automatically pow 2.")]
        public int StartingCapacity = 128;
        public AlphaLib.Renderer ALRenderer { get { return fALRenderer; } }

        // unity method
        void Start()
        {

        }

        void Awake()
        {
            if (fALRenderer == null)
            {
                fALRenderer = new AlphaLib.Renderer(StartingCapacity);
            }

            fMeshRenderer = gameObject.GetComponent<MeshRenderer>();
            fMeshFilter = gameObject.GetComponent<MeshFilter>();
        }

        private void LateUpdate()
        {
            ALRenderer.Update();          
            if (AutoClearPerFrame) ALRenderer.NextClear();      // safe code

            // draw mesh
            if (Mode == RenderingMode.DrawMesh)
            {
                Update_DrawMesh();
                if (AutoClearPerFrame) ALRenderer.Clear();
            }
        }

        void OnRenderObject()
        {
            if (Mode != RenderingMode.DrawMeshNow) return;

            // camera
            if (ActiveCamera != null)
            if (Camera.current != ActiveCamera) return;
            // layer
            if (gameObject.layer != 0)
                if ((Camera.current.cullingMask & gameObject.layer) == 0) return;

            Update_DrawMeshNow();
        }

        protected void Update_DrawMesh()
        {
            Matrix4x4 pm = gameObject.transform.localToWorldMatrix;

            // Leave to the unity system sort.
            //ALRenderer.DoSort(ref pm);

            var n = ALRenderer.Count;
            for (var i = 0; i < n; i++)
            {
                var log = ALRenderer.Logs.Data[i];
                var m = log.matrix;
                AlphaLib.Misc.m43PushMatrix(ref m, ref pm);

                var meshcount = log.refmesh.subMeshCount;
                for (int j = 0; j < meshcount; j++)
                {
                    Graphics.DrawMesh(
                        log.refmesh,
                        m,
                        log.materials[j],
                        gameObject.layer,
                        ActiveCamera,
                        j, // mesh index
                        log.block //log.block
                    );
                }
            }
        }
        protected void Update_DrawMeshNow()
        {
            Material prevmat;
            MaterialPropertyBlock prevpb;

            Matrix4x4 pm = gameObject.transform.localToWorldMatrix;

            ALRenderer.DoSort(ref pm);

            prevmat = null;
            prevpb = null;

            var n = ALRenderer.Count;
            for (var i = 0; i < n; i++)
            {
                var log = ALRenderer.Logs.Data[i];
                var m = log.matrix;
                AlphaLib.Misc.m43PushMatrix(ref m, ref pm); //m = pm * m; //too heavy...

                var meshcount = log.refmesh.subMeshCount;
                for (int j = 0; j < meshcount; j++)
                {
                    // material
                    // Unsupport material property block on unity.... 
                    // There is no material operation command on GL.
                    // I just want to change the texture.....
                    Material mat = log.materials[j];
                    MaterialPropertyBlock pb = log.block;
                    if (mat != null) {
                        if (prevmat == mat)
                        {
                            if (pb != null)
                            {
                                //if (pb.crc != prevpb.crc) pb.SetPass();
                            }
                        }
                        else
                        {
                            mat.SetPass(0);
                            //if (pb != null) pb.SetPass();
                        }
                    }
                    prevmat = mat;
                    prevpb = pb;

                    Graphics.DrawMeshNow(
                        log.refmesh,
                        m,
                        j      // mesh index
                    );
                }
            }

            ALRenderer.NextClear();
        }


        // method

        public void Clear()
        {
            ALRenderer.Clear();
        } 

    }
}
