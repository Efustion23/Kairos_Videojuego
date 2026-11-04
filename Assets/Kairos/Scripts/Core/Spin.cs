using UnityEngine;

namespace Kairos
{
    /// Gira sobre su eje vertical: el radar del puente.
    public class Spin : MonoBehaviour
    {
        public float degreesPerSecond = 90;
        void Update() => transform.Rotate(0, degreesPerSecond * Time.deltaTime, 0, Space.Self);
    }
}
