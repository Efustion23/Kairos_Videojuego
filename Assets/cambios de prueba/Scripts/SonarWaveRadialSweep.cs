using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Kairos.Pruebas
{
    /// <summary>
    /// Componente que genera el efecto visual de onda expansiva radial cian (#3FE0D0)
    /// en el espacio del mundo y en el espacio de pantalla, con sincronía acústica.
    /// </summary>
    public class SonarWaveRadialSweep : MonoBehaviour
    {
        public Color cyanColor = new Color(0.247f, 0.878f, 0.816f, 1f); // #3FE0D0
        public Camera mainCamera;
        public Canvas targetCanvas;

        public void FireSonarPulse(Vector3 originWorld, bool isCrest = true, float targetDistance = 180f)
        {
            if (mainCamera == null) mainCamera = Camera.main;

            // 1. Efecto en espacio de pantalla (Barrido radial HUD con borde cian brillante)
            StartCoroutine(ScreenSweepRoutine(originWorld, isCrest));

            // 2. Efecto en espacio 3D (Onda en la superficie marina)
            StartCoroutine(WorldSweepRoutine(originWorld, isCrest));

            // 3. Audio diegético sincronizado con la onda
            SubmarineAudioSuite.Play(SubmarineAudioSuite.PingAcoustic, isCrest ? 0.95f : 0.65f);
            SubmarineAudioSuite.Play(SubmarineAudioSuite.SonarReverb, isCrest ? 0.75f : 0.45f);

            float echoDelay = Mathf.Clamp(targetDistance / 450f, 0.15f, 1.25f);
            StartCoroutine(DelayedEcho(echoDelay, isCrest));

            if (targetDistance < 220f)
                SubmarineAudioSuite.Play(SubmarineAudioSuite.UnderwaterCreak, isCrest ? 0.7f : 0.45f);
        }

        IEnumerator DelayedEcho(float delay, bool isCrest)
        {
            yield return new WaitForSeconds(delay);
            SubmarineAudioSuite.Play(SubmarineAudioSuite.EcoMetallic, isCrest ? 0.85f : 0.55f);
        }

        IEnumerator ScreenSweepRoutine(Vector3 worldOrigin, bool isCrest)
        {
            var sweepGo = new GameObject("Prueba_ScreenSweepWave");
            Canvas canvas = targetCanvas != null ? targetCanvas : FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                var cGo = new GameObject("TempSonarCanvas");
                canvas = cGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                cGo.AddComponent<CanvasScaler>();
            }
            sweepGo.transform.SetParent(canvas.transform, false);

            var rt = sweepGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var rawImg = sweepGo.AddComponent<RawImage>();
            rawImg.raycastTarget = false;

            Vector2 screenOrigin = new Vector2(0.5f, 0.5f);
            if (mainCamera != null)
            {
                var vp = mainCamera.WorldToViewportPoint(worldOrigin);
                screenOrigin = new Vector2(Mathf.Clamp01(vp.x), Mathf.Clamp01(vp.y));
            }

            Material mat = MatsSonarPrueba.CreateScreenSweep(cyanColor, 0f, screenOrigin, isCrest ? 0.09f : 0.06f, isCrest ? 3.0f : 2.2f);
            rawImg.material = mat;

            float duration = 1.4f;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float progress = t / duration;
                if (mat != null) mat.SetFloat("_Progress", progress);
                yield return null;
            }

            Destroy(sweepGo);
            if (mat != null) Destroy(mat);
        }

        IEnumerator WorldSweepRoutine(Vector3 center, bool isCrest)
        {
            var ringGo = new GameObject("Prueba_PulsoRing3D");
            ringGo.transform.position = center;
            var lr = ringGo.AddComponent<LineRenderer>();
            lr.material = MatsSonarPrueba.CreateRadialSweep(cyanColor, 0f, 16f);
            lr.loop = true;
            lr.positionCount = 80;
            lr.widthMultiplier = isCrest ? 2.5f : 1.6f;

            float maxRadius = 300f;
            float duration = 1.6f;

            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float r = (t / duration) * maxRadius;
                for (int i = 0; i < 80; i++)
                {
                    float angle = i * Mathf.PI * 2f / 80f;
                    float x = center.x + Mathf.Cos(angle) * r;
                    float z = center.z + Mathf.Sin(angle) * r;
                    lr.SetPosition(i, new Vector3(x, center.y + 0.35f, z));
                }

                float alpha = 1f - (t / duration);
                if (lr.material != null)
                {
                    lr.material.SetColor("_SweepColor", new Color(cyanColor.r, cyanColor.g, cyanColor.b, alpha));
                    lr.material.SetVector("_Center", new Vector4(center.x, center.y, center.z, 0));
                    lr.material.SetFloat("_Radius", r);
                }
                yield return null;
            }

            Destroy(ringGo);
        }
    }
}
