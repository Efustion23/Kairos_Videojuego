using UnityEngine;

namespace Kairos.Pruebas
{
    /// <summary>
    /// Rig y controlador de patrulla con linterna para los guardias del buque (Semana 13 / S13-3).
    /// Maneja el haz de luz halógena, el balanceo durante la marcha y el cono de detección visual.
    /// </summary>
    public class GuardLanternPatrolRig : MonoBehaviour
    {
        [Header("Configuración de Linterna")]
        public Transform rightHandSlot;
        [SerializeField] private Light _lanternLight;
        public Light lanternLight
        {
            get
            {
                if (_lanternLight == null) EnsureLantern();
                return _lanternLight;
            }
            set => _lanternLight = value;
        }

        public Color halogenColor = new Color(1.0f, 0.88f, 0.65f); // Halógeno cálido
        public float patrolIntensity = 2.8f;
        public float alertIntensity = 4.5f;
        public float beamRange = 18f;
        public float spotAngle = 42f;

        [Header("Balanceo de Patrulla")]
        public bool isPatrolling = true;
        public bool isStunned = false;
        public float walkSpeed = 1.6f;
        public float swingAmplitude = 4.5f;

        float swingTime;
        Quaternion baseLocalRot;
        GuardAI guardAI;

        public void EnsureLantern()
        {
            if (_lanternLight != null)
            {
                _lanternLight.shadows = LightShadows.None;
                return;
            }

            var child = transform.Find("LinternaHalogena");
            GameObject lGo = child != null ? child.gameObject : null;
            if (lGo == null)
            {
                lGo = new GameObject("LinternaHalogena");
                lGo.transform.SetParent(rightHandSlot != null ? rightHandSlot : transform, false);
                lGo.transform.localPosition = new Vector3(0.25f, 1.1f, 0.35f);
            }
            baseLocalRot = lGo.transform.localRotation;

            _lanternLight = lGo.GetComponent<Light>();
            if (_lanternLight == null) _lanternLight = lGo.AddComponent<Light>();

            _lanternLight.type = LightType.Spot;
            _lanternLight.color = halogenColor;
            _lanternLight.intensity = patrolIntensity;
            _lanternLight.range = beamRange;
            _lanternLight.spotAngle = spotAngle;
            // Sin sombras dinámicas en linternas de patrulla para evitar saturar el atlas de sombras URP 2048x2048
            _lanternLight.shadows = LightShadows.None;
        }

        void Awake()
        {
            guardAI = GetComponent<GuardAI>();
            EnsureLantern();
        }

        void Update()
        {
            if (guardAI != null && guardAI.IsStunned != isStunned)
                SetStunned(guardAI.IsStunned);

            if (isStunned || !isPatrolling || lanternLight == null) return;

            swingTime += Time.deltaTime * walkSpeed * Mathf.PI;
            float yawSwing = Mathf.Sin(swingTime) * swingAmplitude;
            float pitchSwing = Mathf.Abs(Mathf.Cos(swingTime)) * (swingAmplitude * 0.45f);

            lanternLight.transform.localRotation = baseLocalRot * Quaternion.Euler(pitchSwing, yawSwing, 0f);
        }

        public void SetStunned(bool stunned)
        {
            isStunned = stunned;
            EnsureLantern();
            if (lanternLight == null) return;
            lanternLight.enabled = !stunned;
            lanternLight.intensity = stunned ? 0f : patrolIntensity;
            if (!stunned) lanternLight.color = halogenColor;
        }

        public void SetAlert(bool alert)
        {
            EnsureLantern();
            if (lanternLight == null || isStunned) return;
            lanternLight.intensity = alert ? alertIntensity : patrolIntensity;
            lanternLight.color = alert ? new Color(1.0f, 0.45f, 0.25f) : halogenColor;
        }
    }
}
