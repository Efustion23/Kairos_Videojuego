using UnityEngine;
using UnityEngine.InputSystem;

namespace Kairos
{
    /// Inspector en tercera persona: sigilo, cámara forense y herramientas. Sin armas letales.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerAgent : MonoBehaviour
    {
        public Camera cam;
        public Walker walker;
        public float walkSpeed = 2.7f, runSpeed = 5.2f, crouchSpeed = 1.3f;

        CharacterController cc;
        float yaw, pitch = 8f, velY, stepT, slipT, stumble, breathT, focus, roll;
        public bool Crouched { get; private set; }
        public bool Aiming { get; private set; }
        public bool Running { get; private set; }
        public float Stamina { get; private set; } = 1f;
        public float Focus => focus;
        public Interactable Near { get; private set; }
        public Interactable HoldingTarget { get; private set; }
        public EvidenceProp Framed { get; private set; }
        public string FrameBlock { get; private set; }

        InfiltrationController I => InfiltrationController.I;
        public Vector3 ChestPos => transform.position + Vector3.up * (Crouched ? .75f : 1.25f);
        readonly Collider[] overlap = new Collider[16];

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            yaw = transform.eulerAngles.y;
        }

        /// Multiplicador de detección: focos, agacharse y apantallamiento térmico.
        public float Visibility
        {
            get
            {
                float v = I.InDangerLight(transform.position) ? 1.5f : .8f;
                if (Crouched) v *= .6f;
                if (Game.Has(2, 2)) v *= .8f;
                return v;
            }
        }

        void Update()
        {
            if (I == null || !I.PlayerCanAct) { HoldingTarget = null; return; }
            var kb = Keyboard.current; var mouse = Mouse.current;
            if (kb == null || mouse == null) return;

            var md = mouse.delta.ReadValue();
            float sens = Aiming ? .05f : .11f;
            yaw += md.x * sens;
            pitch = Mathf.Clamp(pitch - md.y * sens, -40, 60);

            if (kb.cKey.wasPressedThisFrame) { Crouched = !Crouched; walker.crouched = Crouched; }
            Aiming = mouse.rightButton.isPressed;

            Vector3 input = Vector3.zero;
            if (kb.wKey.isPressed) input.z += 1;
            if (kb.sKey.isPressed) input.z -= 1;
            if (kb.aKey.isPressed) input.x -= 1;
            if (kb.dKey.isPressed) input.x += 1;
            bool moving = input.sqrMagnitude > .01f;

            Running = kb.leftShiftKey.isPressed && moving && !Crouched && !Aiming && Stamina > .02f;
            Stamina = Mathf.Clamp01(Stamina + (Running ? -Time.deltaTime / 9f : Time.deltaTime / 7f));
            float speed = Aiming ? 1.1f : Crouched ? crouchSpeed : Running ? runSpeed : walkSpeed;
            if (stumble > 0) { stumble -= Time.deltaTime; speed = .3f; }

            var dir = Quaternion.Euler(0, yaw, 0) * input.normalized;
            velY = cc.isGrounded ? -2f : velY - 18f * Time.deltaTime;
            cc.Move((dir * speed + Vector3.up * velY) * Time.deltaTime);
            if (Aiming) transform.rotation = Quaternion.Euler(0, yaw, 0);
            else if (moving) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 540f * Time.deltaTime);

            Footsteps(moving);
            Breath();
            Slip(moving);
            Interactions(kb);
            ForensicCamera(mouse);

            if (kb.qKey.wasPressedThisFrame) I.UseJammer();
            if (kb.vKey.wasPressedThisFrame) I.UseStunner();
            if (kb.pKey.wasPressedThisFrame) I.CallPatrol();
        }

        void Footsteps(bool moving)
        {
            if (!moving) return;
            stepT -= Time.deltaTime;
            if (stepT > 0) return;
            float radius = Crouched ? 1.5f : Running ? 9f : 4f;
            stepT = Crouched ? .7f : Running ? .32f : .5f;
            if (Game.Has(2, 1)) radius *= .7f;
            Sfx.PlayAt(Sfx.Step, transform.position, Crouched ? .15f : Running ? .6f : .3f, 12);
            if (!I.StepsMasked) I.EmitNoise(transform.position, radius);
        }

        void Breath()
        {
            if (Stamina > .25f) return;
            breathT -= Time.deltaTime;
            if (breathT > 0) return;
            breathT = 2.4f;
            Sfx.PlayAt(Sfx.Breath, transform.position, .6f, 6);
            I.EmitNoise(transform.position, 3f); // Si es alta, te oyen a menos de 3 m.
        }

        void Slip(bool moving)
        {
            slipT -= Time.deltaTime;
            if (!Running || !moving || slipT > 0 || !I.OnWetDeck(transform.position)) return;
            slipT = 2f;
            if (Random.value < .15f)
            {
                stumble = 1f;
                Sfx.PlayAt(Sfx.Slip, transform.position, .8f, 15);
                I.EmitNoise(transform.position, 9f);
                I.Toast("Resbalón en la cubierta mojada.", Pal.DeckLight, 1.5f);
            }
        }

        void Interactions(Keyboard kb)
        {
            Near = null;
            int n = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up, 2.3f, overlap, 1 << Layers.Interact, QueryTriggerInteraction.Collide);
            float best = 1e9f;
            for (int i = 0; i < n; i++)
            {
                var it = overlap[i].GetComponentInParent<Interactable>();
                if (it == null || !it.isActiveAndEnabled) continue;
                float dy = it.transform.position.y - transform.position.y;
                if (dy < -.6f || dy > 2.5f) continue; // misma cubierta: nada a través del suelo o el techo
                float d = (overlap[i].ClosestPoint(transform.position + Vector3.up) - (transform.position + Vector3.up)).sqrMagnitude;
                if (it is EvidenceProp) d += 1.5f; // la puerta o la persona antes que el objeto
                if (d < best) { best = d; Near = it; }
            }

            HoldingTarget = null;
            if (Near == null || Aiming) return;
            bool can = Near.CanUse(out string why);
            if (Near.Hold <= 0)
            {
                if (kb.eKey.wasPressedThisFrame)
                {
                    if (can) Near.Use(); else I.Toast(why, Pal.DeckLight);
                }
                return;
            }
            if (kb.eKey.isPressed)
            {
                if (!can) { if (kb.eKey.wasPressedThisFrame) I.Toast(why, Pal.DeckLight); return; }
                HoldingTarget = Near;
                Near.Holding();
                Near.progress += Time.deltaTime;
                if (Near.progress >= Near.Hold) { Near.progress = Near.Hold; var used = Near; HoldingTarget = null; used.Use(); }
            }
        }

        void ForensicCamera(Mouse mouse)
        {
            Framed = null; FrameBlock = null;
            if (!Aiming) { focus = 0; return; }
            float move = mouse.delta.ReadValue().magnitude;
            focus = Mathf.Clamp01(focus + (move > 6f ? -Time.deltaTime * 1.2f : Time.deltaTime * .9f));

            var ray = new Ray(cam.transform.position, cam.transform.forward);
            int mask = (1 << Layers.Interact) | Layers.SolidMask;
            var hits = Physics.RaycastAll(ray, 11f, mask, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (h.collider.gameObject.layer == Layers.ShipSolid) break;
                var e = h.collider.GetComponentInParent<EvidenceProp>();
                if (e != null && !Game.HasEvidence(e.evidenceId)) { Framed = e; break; }
            }

            // Sin rostros sin permiso.
            foreach (var p in I.people)
            {
                if (p == null || !p.gameObject.activeInHierarchy) continue;
                var to = p.transform.position + Vector3.up * 1.5f - cam.transform.position;
                if (to.magnitude < 9f && Vector3.Angle(cam.transform.forward, to) < 12f && p.State.testimony != 1 && !Physics.Linecast(cam.transform.position, p.transform.position + Vector3.up * 1.5f, Layers.SolidMask))
                { FrameBlock = "Rostro en el encuadre sin permiso"; break; }
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Sfx.Play(Sfx.Shutter, .9f);
                I.EmitNoise(transform.position, 2f);
                if (FrameBlock != null) { I.Toast(FrameBlock + ": la cámara no dispara.", Pal.DeckLight); return; }
                if (Framed == null) { I.Toast("Nada que documentar en el encuadre.", Pal.Dim, 1.5f); return; }
                if (focus < .8f) { I.Toast($"Enfoque {focus * 100:0} %: necesitas más de 80 %. Quédate quieto.", Pal.DeckLight, 2f); return; }
                I.Photograph(Framed);
            }
        }

        void LateUpdate()
        {
            if (I == null || cam == null || I.mode != InfiltrationController.Mode.Play && I.mode != InfiltrationController.Mode.Dialog && I.mode != InfiltrationController.Mode.Menu) return;

            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, Aiming ? 32f : 62f, Time.unscaledDeltaTime * 8f);
            roll = Mathf.Lerp(roll, I.HeelAngle * .35f, Time.deltaTime * 2.5f);
            var rot = Quaternion.Euler(pitch, yaw, roll);
            float height = Crouched ? 1.05f : 1.55f;
            var head = transform.position + Vector3.up * height;
            Vector3 offset = Aiming ? new Vector3(.32f, .05f, -.55f) : new Vector3(.42f, .15f, -3.2f);
            var desired = head + rot * offset;
            var to = desired - head;
            if (Physics.SphereCast(head, .18f, to.normalized, out var hit, to.magnitude, Layers.SolidMask))
                desired = head + to.normalized * Mathf.Max(hit.distance - .05f, .25f);
            cam.transform.SetPositionAndRotation(desired, rot);
        }
    }
}
