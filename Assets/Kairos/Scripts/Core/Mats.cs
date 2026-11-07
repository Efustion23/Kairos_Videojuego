using UnityEngine;

namespace Kairos
{
    /// Materiales en tiempo de ejecución a partir de Resources/KGlow (shader Kairos/Glow) y shaders de efectos.
    public static class Mats
    {
        static Material baseMat;
        static Material sonarSweepMat;

        public static Material SonarSweep(Color c, float radius = 0f, float width = 14f)
        {
            if (sonarSweepMat == null)
            {
                var s = Shader.Find("Kairos/SonarSweep");
                if (s != null) sonarSweepMat = new Material(s);
            }
            if (sonarSweepMat != null)
            {
                var m = new Material(sonarSweepMat);
                m.SetColor("_SweepColor", c);
                m.SetVector("_Center", Vector4.zero);
                m.SetFloat("_Radius", radius);
                m.SetFloat("_Width", width);
                return m;
            }
            return Glow(c, true, true);
        }

        public static Material Glow(Color c, bool additive = true, bool overlay = false, bool round = false, Texture tex = null, bool opaque = false)
        {
            if (baseMat == null) baseMat = Resources.Load<Material>("KGlow");
            var m = new Material(baseMat);
            m.SetColor("_Color", c);
            if (tex != null) m.SetTexture("_MainTex", tex);
            m.SetFloat("_Src", opaque ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_Dst", opaque ? (float)UnityEngine.Rendering.BlendMode.Zero : additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZT", overlay ? (float)UnityEngine.Rendering.CompareFunction.Always : (float)UnityEngine.Rendering.CompareFunction.LessEqual);
            m.SetFloat("_ZW", opaque ? 1 : 0);
            m.SetFloat("_Round", round ? 1 : 0);
            m.renderQueue = opaque ? 2000 : 3000;
            return m;
        }
    }
}
