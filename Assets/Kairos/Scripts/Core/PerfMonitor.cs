using UnityEngine;
using UnityEngine.InputSystem;

namespace Kairos
{
    /// Medidor de rendimiento para la tarea S15-2. F3 lo muestra u oculta en cualquier escena.
    public sealed class PerfMonitor : MonoBehaviour
    {
        const float SampleSeconds = 1f;

        bool visible;
        float elapsed, worst;
        int frames;
        string text = "Midiendo…";
        GUIStyle style;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            var go = new GameObject(nameof(PerfMonitor));
            DontDestroyOnLoad(go);
            go.AddComponent<PerfMonitor>();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f3Key.wasPressedThisFrame) visible = !visible;

            float dt = Time.unscaledDeltaTime;
            elapsed += dt;
            frames++;
            if (dt > worst) worst = dt;
            if (elapsed < SampleSeconds) return;

            text = $"{frames / elapsed:0} FPS · {elapsed * 1000f / frames:0.0} ms · peor {worst * 1000f:0.0} ms\n" +
                   $"{Screen.width}x{Screen.height} · Calidad {QualitySettings.names[QualitySettings.GetQualityLevel()]} · VSync {QualitySettings.vSyncCount}";
            elapsed = worst = 0f;
            frames = 0;
        }

        void OnGUI()
        {
            if (!visible) return;
            style ??= new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.UpperLeft };
            GUI.Box(new Rect(10, 10, 380, 48), text, style);
        }
    }
}
