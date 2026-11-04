using UnityEngine;

namespace Kairos
{
    /// Cuerpo que flota sobre la ola real.
    public class Buoy : MonoBehaviour
    {
        public float draft = .2f, tilt = 1f;
        public float yaw;
        void LateUpdate()
        {
            var p = transform.position;
            p.y = Ocean.Height(p.x, p.z) - draft;
            transform.position = p;
            var n = Vector3.Slerp(Vector3.up, Ocean.Normal(p.x, p.z), tilt);
            var f = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(f, n), n);
        }
    }
}
