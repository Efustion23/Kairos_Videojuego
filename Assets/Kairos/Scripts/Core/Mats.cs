using UnityEngine;

namespace Kairos
{
    /// Materiales en tiempo de ejecución a partir de Resources/KGlow (shader Kairos/Glow).
    public static class Mats
    {
        static Material baseMat;
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
