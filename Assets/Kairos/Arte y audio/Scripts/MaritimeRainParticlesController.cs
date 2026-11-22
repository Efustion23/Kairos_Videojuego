using System;
using UnityEngine;

namespace Kairos.Pruebas
{
    /// <summary>
    /// Controlador de efectos visuales y acústicos de lluvia torrencial marítima y salpicaduras en cubierta.
    /// Diseñado para la ambientación climática de tormenta nocturna en altamar (Christopher Veliz - Arte 3D y VFX).
    /// </summary>
    public class MaritimeRainParticlesController : MonoBehaviour
    {
        [Header("Parámetros de Lluvia Marina")]
        [Range(0f, 1f)] public float rainIntensity = 0.85f;
        [Range(0f, 60f)] public float windAngleDegrees = 22f;
        public float fallSpeed = 28f;
        public int maxRainDrops = 600;

        [Header("Salpicaduras en Cubierta Metálica")]
        public bool enableDeckSplashes = true;
        public Color rainColor = new Color(0.75f, 0.88f, 1.0f, 0.65f);
        public float splashLifetime = 0.12f;

        [Header("Audio Acústico de Tormenta")]
        public bool enableRainSound = true;
        [Range(0f, 1f)] public float rainAudioVolume = 0.6f;
        public AudioSource rainAudioSource;

        private ParticleSystem rainParticleSystem;
        private ParticleSystem splashParticleSystem;
        private AudioClip generatedRainClip;
        private Material rainMat;
        private Material splashMat;
        private bool isUnderCover;

        public bool IsInitialized { get; private set; }
        public ParticleSystem RainEmitter => rainParticleSystem;
        public ParticleSystem SplashEmitter => splashParticleSystem;

        private void Awake()
        {
            if (Application.isPlaying) RainFx.External = true;
            InitializeRainVfx();
        }


        private void Start()
        {
            if (!Application.isPlaying) return;
            if (rainAudioSource != null && rainAudioSource.outputAudioMixerGroup == null)
                rainAudioSource.outputAudioMixerGroup = SubmarineAudioSuite.AmbienceGroup ?? SubmarineAudioSuite.MasterGroup;
            if (Game.S != null && Game.C != null)
                ApplySectorWeather(Game.C.sector, Ocean.Douglas);
        }

        private void Update()
        {
            if (!Application.isPlaying || !IsInitialized) return;

            var cam = Camera.main;
            if (cam != null && rainParticleSystem != null)
            {
                var cp = cam.transform.position;
                rainParticleSystem.transform.position = new Vector3(cp.x, cp.y + 12f, cp.z);
                if (splashParticleSystem != null)
                {
                    // Las salpicaduras van en la cubierta que pisa el jugador o, fuera del barco, sobre el agua.
                    var inf = InfiltrationController.I;
                    splashParticleSystem.transform.position = inf != null && inf.player != null && inf.player.gameObject.activeInHierarchy
                        ? inf.player.transform.position + Vector3.up * .05f
                        : new Vector3(cp.x, Ocean.Height(cp.x, cp.z) + .05f, cp.z);
                }
            }

            bool underCover = false;
            if (InfiltrationController.I != null && InfiltrationController.I.player != null)
            {
                underCover = !InfiltrationController.I.OnWetDeck(InfiltrationController.I.player.transform.position);
            }

            if (underCover != isUnderCover)
            {
                isUnderCover = underCover;
                if (rainParticleSystem != null)
                {
                    var em = rainParticleSystem.emission;
                    em.enabled = !isUnderCover;
                }
                if (splashParticleSystem != null)
                {
                    var sEm = splashParticleSystem.emission;
                    sEm.enabled = !isUnderCover;
                }
            }

            if (rainAudioSource != null)
            {
                float targetVol = (isUnderCover ? 0.08f : 1f) * rainAudioVolume * rainIntensity;
                rainAudioSource.volume = Mathf.MoveTowards(rainAudioSource.volume, targetVol, Time.deltaTime * 1.8f);
            }
        }

        /// <summary>
        /// Adapta densidad de lluvia y ángulo de ráfaga al sector oceánico activo y la escala Douglas.
        /// </summary>
        public void ApplySectorWeather(int sector, float douglas)
        {
            float d = Mathf.Clamp(douglas, 0f, 9f);
            float intensity;
            float wind;
            if (sector == 1)
            {
                // Golfo de Guinea: chubasco tropical cálido moderado
                intensity = Mathf.Clamp01(0.45f + (d - 3f) * 0.07f);
                wind = Mathf.Clamp(14f + d * 2.0f, 6f, 40f);
            }
            else if (sector >= 2)
            {
                // Océano Índico: tormenta monzónica intensa
                intensity = Mathf.Clamp01(0.82f + (d - 3f) * 0.06f);
                wind = Mathf.Clamp(28f + d * 3.0f, 15f, 55f);
            }
            else
            {
                // Atlántico Sur (Milla 201): llovizna austral y viento constante
                intensity = Mathf.Clamp01(0.60f + (d - 3f) * 0.08f);
                wind = Mathf.Clamp(18f + d * 2.4f, 8f, 46f);
            }

            SetRainIntensity(intensity);
            SetWindAngle(wind);
        }

        public void InitializeRainVfx()
        {
            if (IsInitialized) return;

            SetupRainEmitters();
            SetupAudioLoop();

            IsInitialized = true;
        }

        private static Material CreateUrpRainMaterial(Color tint, bool roundSplash)
        {
            var glowTemplate = Resources.Load<Material>("KGlow");
            if (glowTemplate != null)
            {
                var mat = new Material(glowTemplate) { hideFlags = HideFlags.DontSave };
                mat.SetColor("_Color", tint);
                mat.SetFloat("_Src", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_Dst", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZT", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                mat.SetFloat("_ZW", 0f);
                mat.SetFloat("_Round", roundSplash ? 1f : 0f);
                mat.renderQueue = 3000;
                return mat;
            }

            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default");
            if (sh == null) return null;

            var fallback = new Material(sh) { hideFlags = HideFlags.DontSave };
            if (fallback.HasProperty("_BaseColor")) fallback.SetColor("_BaseColor", tint);
            if (fallback.HasProperty("_Color")) fallback.SetColor("_Color", tint);
            return fallback;
        }

        private void SetupRainEmitters()
        {
            // Emisor principal de gotas
            GameObject rainObj = new GameObject("Maritime_Rain_Emitter");
            rainObj.transform.SetParent(transform, false);
            rainObj.transform.localPosition = new Vector3(0, 15f, 0);

            rainParticleSystem = rainObj.AddComponent<ParticleSystem>();
            var main = rainParticleSystem.main;
            main.maxParticles = maxRainDrops;
            main.startLifetime = 1.0f;
            main.startSpeed = fallSpeed;
            main.startSize = 0.025f; // gotas finas: a 0.04 las cercanas parecían palos
            main.startColor = rainColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = rainParticleSystem.emission;
            emission.rateOverTime = maxRainDrops * rainIntensity;

            var shape = rainParticleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            // La caja emite por su eje Z: girada 90° en X dispara hacia abajo (sin esto la lluvia volaba en horizontal).
            shape.rotation = new Vector3(90f, 0f, 0f);
            shape.scale = new Vector3(30f, 30f, 0.5f);

            var rainRenderer = rainObj.GetComponent<ParticleSystemRenderer>();
            if (rainRenderer != null)
            {
                rainMat = CreateUrpRainMaterial(rainColor, false);
                if (rainMat != null) rainRenderer.sharedMaterial = rainMat;
                rainRenderer.renderMode = ParticleSystemRenderMode.Stretch;
                rainRenderer.velocityScale = 0.05f; // cayendo en vertical el trazo se ve corto: algo más largo para que se lea como lluvia
                rainRenderer.lengthScale = 1.6f;
                rainRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rainRenderer.receiveShadows = false;
            }

            // Inclinación por ráfagas de viento oceánico
            rainObj.transform.localRotation = Quaternion.Euler(windAngleDegrees, 0, 0);

            // Emisor secundario de salpicaduras
            if (enableDeckSplashes)
            {
                GameObject splashObj = new GameObject("Maritime_Splash_Emitter");
                splashObj.transform.SetParent(transform, false);
                splashObj.transform.localPosition = Vector3.zero;

                splashParticleSystem = splashObj.AddComponent<ParticleSystem>();
                var sMain = splashParticleSystem.main;
                sMain.maxParticles = 200;
                sMain.startLifetime = splashLifetime;
                sMain.startSpeed = 1.8f;
                sMain.startSize = 0.08f;
                Color splashCol = new Color(0.85f, 0.95f, 1f, 0.45f);
                sMain.startColor = splashCol;
                sMain.simulationSpace = ParticleSystemSimulationSpace.World;

                // Salpicaduras que saltan hacia arriba desde una capa plana en el suelo, no un cono horizontal.
                var sShape = splashParticleSystem.shape;
                sShape.shapeType = ParticleSystemShapeType.Box;
                sShape.rotation = new Vector3(-90f, 0f, 0f);
                sShape.scale = new Vector3(14f, 14f, 0.05f);

                var sEmission = splashParticleSystem.emission;
                sEmission.rateOverTime = 80f * rainIntensity;

                var splashRenderer = splashObj.GetComponent<ParticleSystemRenderer>();
                if (splashRenderer != null)
                {
                    splashMat = CreateUrpRainMaterial(splashCol, true);
                    if (splashMat != null) splashRenderer.sharedMaterial = splashMat;
                    splashRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    splashRenderer.receiveShadows = false;
                }
            }
        }

        private void SetupAudioLoop()
        {
            if (!enableRainSound) return;

            generatedRainClip = GenerateProceduralRainNoise(2.0f);

            if (rainAudioSource == null)
            {
                var audioChild = new GameObject("Rain_Ambience_Audio");
                audioChild.SetActive(false);
                audioChild.transform.SetParent(transform, false);
                rainAudioSource = audioChild.AddComponent<AudioSource>();
                rainAudioSource.playOnAwake = false;
                rainAudioSource.clip = generatedRainClip;
                audioChild.SetActive(true);
            }
            else
            {
                rainAudioSource.playOnAwake = false;
                rainAudioSource.clip = generatedRainClip;
            }

            rainAudioSource.loop = true;
            rainAudioSource.volume = rainAudioVolume * rainIntensity;
            rainAudioSource.spatialBlend = 0.2f; // Audio envolvente semi-ambiental

            if (Application.isPlaying)
            {
                rainAudioSource.Play();
            }
        }

        public void SetRainIntensity(float intensity)
        {
            rainIntensity = Mathf.Clamp01(intensity);

            if (rainParticleSystem != null)
            {
                var em = rainParticleSystem.emission;
                em.rateOverTime = maxRainDrops * rainIntensity;
            }

            if (splashParticleSystem != null)
            {
                var sEm = splashParticleSystem.emission;
                sEm.rateOverTime = 80f * rainIntensity;
            }

            if (rainAudioSource != null)
            {
                rainAudioSource.volume = rainAudioVolume * rainIntensity;
            }
        }

        public void SetWindAngle(float angle)
        {
            windAngleDegrees = Mathf.Clamp(angle, 0f, 60f);
            if (rainParticleSystem != null)
            {
                rainParticleSystem.transform.localRotation = Quaternion.Euler(windAngleDegrees, 0, 0);
            }
        }

        /// <summary>
        /// Síntesis de ruido blanco filtrado simulando la caída densa de lluvia sobre metal y agua marina.
        /// </summary>
        public static AudioClip GenerateProceduralRainNoise(float durationSeconds)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.CeilToInt(sampleRate * durationSeconds);
            float[] data = new float[totalSamples];

            System.Random rand = new System.Random(7721);
            float lastSample = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                // Ruido blanco atenuado con filtro paso bajo simple (brownian/pink noise blend)
                float white = (float)(rand.NextDouble() * 2.0 - 1.0);
                lastSample = Mathf.Lerp(lastSample, white, 0.15f);

                // Modulación de ráfagas suaves de viento (0.8 Hz)
                float windMod = 0.7f + 0.3f * Mathf.Sin((float)i / sampleRate * Mathf.PI * 1.6f);
                data[i] = lastSample * windMod * 0.45f;
            }

            AudioClip clip = AudioClip.Create("Procedural_Maritime_Rain", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                RainFx.External = false;
                if (generatedRainClip != null) Destroy(generatedRainClip);
                if (rainMat != null) Destroy(rainMat);
                if (splashMat != null) Destroy(splashMat);
            }
            else
            {
                if (generatedRainClip != null) DestroyImmediate(generatedRainClip);
                if (rainMat != null) DestroyImmediate(rainMat);
                if (splashMat != null) DestroyImmediate(splashMat);
            }
        }
    }
}
