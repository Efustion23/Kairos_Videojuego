using UnityEngine;
using UnityEngine.AI;

namespace Kairos
{
    /// Balanceo de brazos y piernas según la velocidad real: basta hasta que haya animaciones.
    public class Walker : MonoBehaviour
    {
        public Transform legL, legR, armL, armR, body;
        Vector3 last; float phase, speed;
        public bool crouched;
        void Start() => last = transform.position;
        void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, .0001f);
            speed = Mathf.Lerp(speed, (transform.position - last).magnitude / dt, dt * 10f);
            last = transform.position;
            phase += speed * dt * 3.2f;
            float swing = Mathf.Sin(phase) * Mathf.Clamp(speed * 14f, 0, 42f);
            if (legL) legL.localRotation = Quaternion.Euler(swing, 0, 0);
            if (legR) legR.localRotation = Quaternion.Euler(-swing, 0, 0);
            if (armL) armL.localRotation = Quaternion.Euler(-swing * .8f, 0, 0);
            if (armR) armR.localRotation = Quaternion.Euler(swing * .8f, 0, 0);
            if (body) body.localScale = new Vector3(1, Mathf.MoveTowards(body.localScale.y, crouched ? .68f : 1f, dt * 5f), 1);
        }
    }
}
