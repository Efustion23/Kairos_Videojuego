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
        public Light lanternLight;
        public Color halogenColor = new Color(1.0f, 0.88f, 0.65f); // Halógeno cálido
        public float patrolIntensity = 2.8f;
        public float alertIntensity = 4.5f;
        public float beamRange = 18f;
        public float spotAngle = 42f;

        [Header("Balanceo de Patrulla")]
        public bool isPatrolling = true;
        public float walkSpeed = 1.6f;
        public float swingAmplitude = 4.5f;

        float swingTime;
        Quaternion baseLocalRot;

        void Awake()
        {
            if (lanternLight == null)
            {
                var lGo = new GameObject("LinternaHalogena");
                lGo.transform.SetParent(rightHandSlot != null ? rightHandSlot : transform, false);
                lGo.transform.localPosition = new Vector3(0.25f, 1.1f, 0.35f);
                baseLocalRot = lGo.transform.localRotation;

                lanternLight = lGo.AddComponent<Light>();
                lanternLight.type = LightType.Spot;
                lanternLight.color = halogenColor;
                lanternLight.intensity = patrolIntensity;
                lanternLight.range = beamRange;
                lanternLight.spotAngle = spotAngle;
                lanternLight.shadows = LightShadows.Soft;
            }
            else
            {
                baseLocalRot = lanternLight.transform.localRotation;
            }
        }

        void Update()
        {
            if (!isPatrolling || lanternLight == null) return;

            swingTime += Time.deltaTime * walkSpeed * Mathf.PI;
            float yawSwing = Mathf.Sin(swingTime) * swingAmplitude;
            float pitchSwing = Mathf.Abs(Mathf.Cos(swingTime)) * (swingAmplitude * 0.45f);

            lanternLight.transform.localRotation = baseLocalRot * Quaternion.Euler(pitchSwing, yawSwing, 0f);
        }

        public void SetAlert(bool alert)
        {
            if (lanternLight == null) return;
            lanternLight.intensity = alert ? alertIntensity : patrolIntensity;
            lanternLight.color = alert ? new Color(1.0f, 0.45f, 0.25f) : halogenColor;
        }
    }
}
