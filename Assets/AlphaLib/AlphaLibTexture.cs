using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AlphaLib
{
    // texture effect (not relatime, need unsafe)
    public unsafe class ALTexture
    {
        protected bool IsGetPixels = false;
        protected Color32[] PixelData = null;
        protected Color32[] OriginalData = null;
        protected bool GetPixels()
        {
            if (Texture == null) return false;

            if (IsGetPixels)
            {
                return OriginalData != null;
            }

            IsGetPixels = true;
            try
            {
                OriginalData = Texture.GetPixels32(0); // mipmap level 0
            }
            catch
            {
                OriginalData = null;
                AlphaLib.Debug.Warning("AlphaLib.ALtexture : " + Texture.name + " : Can not read pixels. Set write/read check in texture settings.");
                return false;
            }

            PixelData = new Color32[OriginalData.Length];
            return true;
        }
        public Texture2D Texture { get; protected set; } = null;
        public bool MulticoreEnable = false;
        public ALTexture(Texture2D tex, bool getpixelsoncreate = false, bool instantiate = false)
        {
            if (instantiate)
            {
                Texture = UnityEngine.Object.Instantiate<Texture2D>(tex);
            }
            else
            {
                Texture = tex;
            }

            if (getpixelsoncreate)
            {
                GetPixels();
            }
        }
        public void Setup()
        {
            if (!GetPixels()) return;

            Array.Copy(OriginalData, PixelData, OriginalData.Length);
        }
        public void FillColor(Color col)
        {
            if (!GetPixels()) return;

            Color32 col32 = col;
            fixed (Color32* fixedsrc = &PixelData[0])
            {
                var src = fixedsrc;
                var len = PixelData.Length;
                for (var i = 0; i < len; i++)
                {
                    *src = col32;
                    src++;
                }
            }
        }
        public void Apply()
        {
            if (!GetPixels()) return;

            Texture.SetPixels32(PixelData, 0);
            Texture.Apply(true);
        }
    }
}