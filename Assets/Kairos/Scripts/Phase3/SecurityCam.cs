using UnityEngine;
using UnityEngine.AI;

namespace Kairos
{
    /// Cámara del barco. El bloqueador la ciega 45 s.
    public class SecurityCam : MonoBehaviour
    {
        public float yawA = -40, yawB = 40, range = 10f, angle = 55f, floorY = 0f;
        public string alarmName = "Cámara";
        float charge, t;
        VisionFan fan;
        float baseYaw;
        InfiltrationController I => InfiltrationController.I;

        void Start()
        {
            baseYaw = transform.eulerAngles.y;
            fan = VisionFan.Create(transform, 0, range, angle);
            fan.transform.position = new Vector3(transform.position.x, floorY + .08f, transform.position.z);
        }

        void Update()
        {
            if (I == null || !I.Running) return;
            t += Time.deltaTime;
            transform.rotation = Quaternion.Euler(0, baseYaw + Mathf.Lerp(yawA, yawB, Mathf.PingPong(t * .18f, 1f)), 0);
            if (I.JamActive) { fan.SetColor(new Color(.2f, .2f, .2f)); charge = 0; return; }
            fan.SetColor(charge > 0 ? Pal.DeckLight : new Color(.5f, .8f, 1f));
            var p = I.player;
            var to = p.ChestPos - transform.position;
            var flat = new Vector3(to.x, 0, to.z);
            bool seen = to.magnitude < range + 2f && flat.magnitude < range && Vector3.Angle(transform.forward, flat) < angle * .5f && !Physics.Linecast(transform.position, p.ChestPos, Layers.SolidMask);
            charge = Mathf.Clamp01(charge + (seen ? Time.deltaTime * .8f * p.Visibility : -Time.deltaTime * .3f));
            if (charge >= 1f)
            {
                charge = 0;
                I.Toast(alarmName + " te ha grabado: un guardia viene a mirar.", Pal.DeckLight);
                GuardAI nearest = null; float best = 1e9f;
                foreach (var g in I.guards)
                {
                    if (g.IsStunned) continue;
                    float d = Vector3.Distance(g.transform.position, p.transform.position);
                    if (d < best) { best = d; nearest = g; }
                }
                if (nearest != null) nearest.Lure(p.transform.position);
            }
        }
    }
}
