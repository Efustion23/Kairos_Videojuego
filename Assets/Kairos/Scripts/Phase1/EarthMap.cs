using System.Collections.Generic;
using UnityEngine;

namespace Kairos
{
    /// Mapa estilizado generado a partir de polígonos gruesos (lat, lon) de los continentes.
    public static class EarthMap
    {
        static readonly float[][] Land =
        {
            // Sudamérica
            new float[] { 12,-72, 11,-62, 6,-52, 0,-50, -5,-35, -13,-38, -23,-42, -25,-48, -33,-52, -38,-57, -42,-63, -47,-66, -52,-68, -55,-68, -53,-74, -45,-74, -37,-73, -28,-71, -18,-70, -14,-76, -5,-81, 1,-80, 7,-78 },
            // Norte y Centroamérica
            new float[] { 8,-78, 9,-83, 15,-92, 19,-105, 28,-112, 33,-118, 40,-124, 48,-125, 57,-135, 60,-147, 56,-160, 66,-165, 71,-157, 70,-130, 68,-110, 73,-90, 66,-85, 60,-94, 54,-82, 58,-77, 62,-72, 52,-56, 47,-60, 44,-66, 40,-74, 32,-80, 25,-80, 30,-90, 26,-97, 19,-96, 21,-87, 16,-88, 11,-84 },
            // Groenlandia
            new float[] { 60,-44, 70,-22, 81,-15, 83,-40, 78,-70, 70,-55 },
            // África
            new float[] { 36,-6, 37,10, 32,20, 31,32, 22,37, 12,44, 11,51, 2,46, -8,40, -16,40, -25,35, -34,26, -34,18, -22,14, -12,13, -5,12, 4,9, 5,-4, 5,-10, 10,-15, 15,-17, 21,-17, 28,-13 },
            // Madagascar
            new float[] { -13,49, -18,50, -25,47, -24,44, -17,44 },
            // Europa y Asia
            new float[] { 36,-9, 43,-9, 44,-1, 48,-4, 51,2, 54,8, 58,6, 62,5, 70,20, 71,30, 68,42, 73,70, 76,105, 72,130, 70,160, 65,178, 60,165, 56,162, 52,157, 58,140, 50,140, 42,132, 38,127, 34,127, 38,118, 30,122, 22,114, 18,106, 10,106, 9,100, 2,104, 8,98, 16,95, 22,90, 16,81, 8,77, 20,72, 25,62, 25,56, 17,55, 13,45, 28,34, 36,36, 37,27, 40,26, 36,23, 40,19, 44,13, 38,16, 42,10, 43,4, 38,0 },
            // Indonesia y Filipinas (masas simplificadas)
            new float[] { 5,95, 2,102, -5,105, -8,115, -8,125, -3,120, 1,119, 7,117, 2,109, -3,106, -5,100 },
            new float[] { -2,131, -3,141, -9,148, -8,138, -5,133 },
            new float[] { 18,121, 14,124, 8,126, 7,122, 13,120 },
            // Japón
            new float[] { 45,142, 40,142, 35,140, 33,131, 35,133, 39,139 },
            // Australia y Nueva Zelanda
            new float[] { -11,132, -11,142, -19,147, -28,154, -38,149, -38,140, -32,133, -34,120, -32,115, -22,113, -15,124 },
            new float[] { -35,173, -41,176, -46,168, -42,171 },
            // Gran Bretaña e Islandia
            new float[] { 58,-5, 53,0, 50,-5, 54,-5 },
            new float[] { 66,-22, 66,-14, 63,-18, 64,-22 },
            // Antártida
            new float[] { -68,-180, -66,-120, -70,-60, -64,-58, -72,-20, -68,30, -66,90, -67,140, -70,180, -90,180, -90,-180 },
        };

        public static Texture2D Build()
        {
            const int W = 2048, H = 1024;
            var px = new Color32[W * H];
            var ocean = (Color32)Color.Lerp(Pal.Ocean, Color.black, .25f);
            for (int i = 0; i < px.Length; i++) px[i] = ocean;
            var land = (Color32)new Color(.13f, .24f, .27f);
            var dot = (Color32)new Color(.22f, .55f, .55f);
            var mask = new bool[W * H];
            var crossings = new List<float>();
            foreach (var poly in Land)
                for (int y = 0; y < H; y++)
                {
                    float lat = -90 + 180f * (y + .5f) / H;
                    crossings.Clear();
                    int n = poly.Length / 2;
                    for (int i = 0; i < n; i++)
                    {
                        float la0 = poly[i * 2], lo0 = poly[i * 2 + 1], la1 = poly[(i + 1) % n * 2], lo1 = poly[(i + 1) % n * 2 + 1];
                        if ((la0 <= lat) != (la1 <= lat)) crossings.Add(lo0 + (lat - la0) / (la1 - la0) * (lo1 - lo0));
                    }
                    crossings.Sort();
                    for (int c = 0; c + 1 < crossings.Count; c += 2)
                    {
                        int x0 = Mathf.Clamp((int)((crossings[c] + 180) / 360 * W), 0, W - 1);
                        int x1 = Mathf.Clamp((int)((crossings[c + 1] + 180) / 360 * W), 0, W - 1);
                        for (int x = x0; x <= x1; x++) mask[y * W + x] = true;
                    }
                }
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int idx = y * W + x;
                    if (mask[idx])
                    {
                        px[idx] = land;
                        // Sin puntos cerca de los polos: la proyección los convierte en rayas.
                        if (x % 6 < 2 && y % 6 < 2 && y > H / 6 && y < H - H / 10) px[idx] = dot;
                    }
                    else if (x > 0 && y > 0 && (mask[idx - 1] || mask[idx - W])) px[idx] = dot;
                    if (x % (W / 36) == 0 || y % (H / 18) == 0) px[idx] = Color32.Lerp(px[idx], new Color32(63, 224, 208, 255), .12f);
                }
            var t = new Texture2D(W, H, TextureFormat.RGBA32, true) { wrapModeU = TextureWrapMode.Repeat, wrapModeV = TextureWrapMode.Clamp, anisoLevel = 4 };
            t.SetPixels32(px); t.Apply();
            return t;
        }

        public static Texture2D Clouds()
        {
            const int W = 512, H = 256;
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float n = Mathf.PerlinNoise(x * .022f, y * .04f) * .6f + Mathf.PerlinNoise(x * .07f + 9, y * .12f + 3) * .4f;
                    byte a = (byte)(Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.48f, .75f, n)) * 170);
                    px[y * W + x] = new Color32(190, 205, 215, a);
                }
            var t = new Texture2D(W, H, TextureFormat.RGBA32, true) { wrapModeU = TextureWrapMode.Repeat, wrapModeV = TextureWrapMode.Clamp };
            t.SetPixels32(px); t.Apply();
            return t;
        }
    }
}
