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

        public void FireSonarPulse(Vector3 originWorld)
        {
            var sonar = GetComponent<SonarController>();
            float targetDist = 180f;
            bool isCrest = true;
            if (sonar != null && sonar.ship != null)
            {
                targetDist = Vector3.Distance(
                    new Vector3(originWorld.x, 0f, originWorld.z),
                    new Vector3(sonar.ship.position.x, 0f, sonar.ship.position.z));
                isCrest = Ocean.SecondsFromCrest(originWorld.x, originWorld.z) <= (Game.Has(0, 1) ? 0.7f : 0.5f);
            }
            // SonarController ya dispara PingAcoustic y EcoMetallic; evitar doble disparo de audio
            FireSonarPulseInternal(originWorld, isCrest, targetDist, sonar == null);
        }

        public void FireSonarPulse(Vector3 originWorld, bool isCrest = true, float targetDistance = 180f)
        {
            FireSonarPulseInternal(originWorld, isCrest, targetDistance, true);
        }

        private void FireSonarPulseInternal(Vector3 originWorld, bool isCrest, float targetDistance, bool playPrimaryPingAndEco)
        {
            if (mainCamera == null) mainCamera = Camera.main;

            // 1. Efecto en espacio de pantalla (Barrido radial HUD con borde cian brillante)
            StartCoroutine(ScreenSweepRoutine(originWorld, isCrest));

            // 2. Efecto en espacio 3D (Onda en la superficie marina)
            StartCoroutine(WorldSweepRoutine(originWorld, isCrest));

            // 3. Audio diegético sincronizado con la onda
            if (playPrimaryPingAndEco)
            {
                SubmarineAudioSuite.Play(SubmarineAudioSuite.PingAcoustic, isCrest ? 0.95f : 0.65f);
                float echoDelay = Mathf.Clamp(targetDistance / 450f, 0.15f, 1.25f);
                StartCoroutine(DelayedEcho(echoDelay, isCrest));
            }
            SubmarineAudioSuite.Play(SubmarineAudioSuite.SonarReverb, isCrest ? 0.75f : 0.45f);

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
            GameObject tempCanvasGo = null;
            Canvas canvas = targetCanvas != null ? targetCanvas : FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                tempCanvasGo = new GameObject("TempSonarCanvas");
                canvas = tempCanvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                tempCanvasGo.AddComponent<CanvasScaler>();
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
            if (tempCanvasGo != null) Destroy(tempCanvasGo);
            if (mat != null) Destroy(mat);
        }

        IEnumerator WorldSweepRoutine(Vector3 center, bool isCrest)
        {
            var ringGo = new GameObject("Prueba_PulsoRing3D");
            ringGo.transform.position = center;
            var lr = ringGo.AddComponent<LineRenderer>();
            Material ringMat = MatsSonarPrueba.CreateRadialSweep(cyanColor, 0f, 16f);
            lr.sharedMaterial = ringMat;
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
                if (ringMat != null)
                {
                    ringMat.SetColor("_SweepColor", new Color(cyanColor.r, cyanColor.g, cyanColor.b, alpha));
                    ringMat.SetVector("_Center", new Vector4(center.x, center.y, center.z, 0));
                    ringMat.SetFloat("_Radius", r);
                }
                yield return null;
            }

            Destroy(ringGo);
            if (ringMat != null) Destroy(ringMat);
        }
    }
}
