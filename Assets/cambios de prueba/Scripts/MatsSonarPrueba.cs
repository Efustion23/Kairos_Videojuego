using UnityEngine;

namespace Kairos.Pruebas
{
    public static class MatsSonarPrueba
    {
        static Material radialSweepMat;
        static Material screenSweepMat;

        public static Material CreateRadialSweep(Color color, float radius = 0f, float width = 14f)
        {
            if (radialSweepMat == null)
            {
                var s = Shader.Find("Kairos/Pruebas/SonarRadialSweep") ?? Shader.Find("Kairos/SonarSweep");
                if (s != null) radialSweepMat = new Material(s);
            }
            if (radialSweepMat != null)
            {
                var m = new Material(radialSweepMat);
                m.SetColor("_SweepColor", color);
                m.SetVector("_Center", Vector4.zero);
                m.SetFloat("_Radius", radius);
                m.SetFloat("_Width", width);
                return m;
            }
            var fallback = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            fallback.color = color;
            return fallback;
        }

        public static Material CreateScreenSweep(Color color, float progress = 0f, Vector2? origin = null, float width = 0.08f, float intensity = 2.5f)
        {
            if (screenSweepMat == null)
            {
                var s = Shader.Find("Kairos/Pruebas/SonarScreenSweep") ?? Shader.Find("Kairos/SonarScreenSweep");
                if (s != null) screenSweepMat = new Material(s);
            }
            if (screenSweepMat != null)
            {
                var m = new Material(screenSweepMat);
                m.SetColor("_SweepColor", color);
                var orig = origin ?? new Vector2(0.5f, 0.5f);
                m.SetVector("_Origin", new Vector4(orig.x, orig.y, 0, 0));
                m.SetFloat("_Progress", progress);
                m.SetFloat("_Width", width);
                m.SetFloat("_Intensity", intensity);
                return m;
            }
            return CreateRadialSweep(color, progress * 100f, width * 100f);
        }
    }
}
