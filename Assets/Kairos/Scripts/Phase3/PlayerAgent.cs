using UnityEngine;
using UnityEngine.InputSystem;

namespace Kairos
{
    /// Inspector en tercera persona: pocas teclas, y E hace lo correcto según lo que tengas delante.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerAgent : MonoBehaviour
    {
        public Camera cam;
        public HumanoidMotion motion;
        public float walkSpeed = 2f, runSpeed = 5.2f;
        public float acceleration = 11f, braking = 15f, turnSharpness = 11f;

        CharacterController cc;
        float yaw, pitch = 8f, velY, slipT, stumble, breathT, focus, roll, camVelY;
        int lastStep;
        Vector3 moveVel, camAnchor, camVelXZ;
        bool camReady;
        public bool Aiming { get; private set; }
        public bool Running { get; private set; }
        public bool Moved { get; private set; }
        public float Stamina { get; private set; } = 1f;
        public float Focus => focus;
        public Interactable Near { get; private set; }
        public Interactable HoldingTarget { get; private set; }
        public EvidenceProp Framed { get; private set; }
        public string FrameBlock { get; private set; }

        InfiltrationController I => InfiltrationController.I;
        public Vector3 ChestPos => transform.position + Vector3.up * 1.25f;
        readonly Collider[] overlap = new Collider[16];

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            yaw = transform.eulerAngles.y;
        }

        public void Teleport(Vector3 pos)
        {
            cc.enabled = false;
            transform.position = pos;
            cc.enabled = true;
            stumble = 0;
            moveVel = Vector3.zero;
            camReady = false; // la cámara salta con el personaje, sin arrastrarse por el barco
        }

        /// Mira a un punto, con la cámara detrás: en una conversación se ve a las dos personas.
        public void FaceTo(Vector3 point)
        {
            var dir = point - transform.position; dir.y = 0;
            if (dir.sqrMagnitude < .01f) return;
            transform.rotation = Quaternion.LookRotation(dir);
            yaw = transform.eulerAngles.y;
            pitch = 8f;
            moveVel = Vector3.zero;
        }

        /// Multiplicador de detección: los focos delatan, la sombra protege.
        public float Visibility
        {
            get
            {
                float v = I.InDangerLight(transform.position) ? 1.3f : .75f;
                if (Game.Has(2, 2)) v *= .8f;
                return v;
            }
        }

        void Update()
        {
            if (I != null) motion.talking = I.mode == InfiltrationController.Mode.Dialog;
            if (I == null || !I.PlayerCanAct) { HoldingTarget = null; motion.busy = motion.aiming = false; return; }
            var kb = Keyboard.current; var mouse = Mouse.current;
            if (kb == null || mouse == null) return;

            var md = mouse.delta.ReadValue();
            float sens = (Aiming ? .05f : .11f) * Game.S.mouseSens;
            yaw += md.x * sens;
            pitch = Mathf.Clamp(pitch - md.y * sens, -40, 60);

            Aiming = mouse.rightButton.isPressed;

            Vector3 input = Vector3.zero;
            if (kb.wKey.isPressed) input.z += 1;
            if (kb.sKey.isPressed) input.z -= 1;
            if (kb.aKey.isPressed) input.x -= 1;
            if (kb.dKey.isPressed) input.x += 1;
            bool moving = input.sqrMagnitude > .01f;
            if (moving) Moved = true;

            Running = kb.leftShiftKey.isPressed && moving && !Aiming && Stamina > .02f;
            Stamina = Mathf.Clamp01(Stamina + (Running ? -Time.deltaTime / 9f : Time.deltaTime / 7f));
            float speed = Aiming ? 1.1f : Running ? runSpeed : walkSpeed;
            if (stumble > 0) { stumble -= Time.deltaTime; speed = .3f; }

            // Arranca, frena y cambia de dirección con inercia: el cuerpo tiene peso.
            var dir = Quaternion.Euler(0, yaw, 0) * input.normalized;
            moveVel = Vector3.MoveTowards(moveVel, dir * speed, (moving ? acceleration : braking) * Time.deltaTime);
            velY = cc.isGrounded ? -2f : velY - 18f * Time.deltaTime;
            cc.Move((moveVel + Vector3.up * velY) * Time.deltaTime);

            // Gira hacia donde se mueve, con una curva suave en vez de a velocidad fija.
            var facing = Aiming ? Quaternion.Euler(0, yaw, 0)
                : moveVel.sqrMagnitude > .04f ? Quaternion.LookRotation(moveVel) : transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, facing, 1f - Mathf.Exp(-turnSharpness * Time.deltaTime));

            Footsteps();
            Breath();
            Slip(moving);
            Interactions(kb);
            ForensicCamera(mouse);
            motion.busy = HoldingTarget != null;
            motion.typing = HoldingTarget is BridgeConsole;
            motion.aiming = Aiming;

            if (kb.qKey.wasPressedThisFrame) I.UseTool();
            if (kb.fKey.wasPressedThisFrame) I.UseVisor();
        }

        /// Suena y se oye justo cuando el pie toca el suelo.
        void Footsteps()
        {
            if (motion.Steps == lastStep) return;
            lastStep = motion.Steps;
            float radius = Running ? 8f : 2.5f; // caminar es sigiloso; correr, no
            if (Game.Has(2, 1)) radius *= .7f;
            // Metal seco o cubierta mojada, y ningún paso suena igual al anterior.
            Sfx.PlayAt(I.OnWetDeck(transform.position) ? Sfx.StepWet : Sfx.Step, transform.position, Running ? .6f : .25f, 12, Random.Range(.88f, 1.12f));
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
                stumble = .6f;
                Sfx.PlayAt(Sfx.Slip, transform.position, .8f, 15);
                I.EmitNoise(transform.position, 6f);
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

        /// Opcional: fotografiar de lejos. De cerca basta con mantener E.
        void ForensicCamera(Mouse mouse)
        {
            Framed = null; FrameBlock = null;
            if (!Aiming) { focus = 0; return; }
            float move = mouse.delta.ReadValue().magnitude;
            focus = Mathf.Clamp01(focus + (move > 12f ? -Time.deltaTime : Time.deltaTime * 1.6f));

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
                if (focus < .8f) { I.Toast($"Enfoque {focus * 100:0} %: quédate quieto un momento.", Pal.DeckLight, 2f); return; }
                I.Photograph(Framed);
            }
        }

        void LateUpdate()
        {
            if (I == null || cam == null || I.mode != InfiltrationController.Mode.Play && I.mode != InfiltrationController.Mode.Dialog && I.mode != InfiltrationController.Mode.Menu) return;

            float runFov = Running ? 66f : 62f;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, Aiming ? 32f : runFov, Time.unscaledDeltaTime * 6f);
            roll = Mathf.Lerp(roll, I.HeelAngle * .35f, Time.deltaTime * 2.5f);
            var rot = Quaternion.Euler(pitch, yaw, roll);

            // La cámara sigue con un ligero retardo, más lento en altura: rampas y escalones no la sacuden.
            var target = transform.position + Vector3.up * 1.55f;
            if (!camReady) { camAnchor = target; camVelXZ = Vector3.zero; camVelY = 0; camReady = true; }
            var flatAnchor = new Vector3(camAnchor.x, 0, camAnchor.z);
            flatAnchor = Vector3.SmoothDamp(flatAnchor, new Vector3(target.x, 0, target.z), ref camVelXZ, Aiming ? .02f : .05f);
            camAnchor = new Vector3(flatAnchor.x, Mathf.SmoothDamp(camAnchor.y, target.y, ref camVelY, .12f), flatAnchor.z);
            var head = camAnchor;
            Vector3 offset = Aiming ? new Vector3(.32f, .05f, -.55f) : new Vector3(.42f, .15f, -3.2f);
            var desired = head + rot * offset;
            var to = desired - head;
            if (Physics.SphereCast(head, .18f, to.normalized, out var hit, to.magnitude, Layers.SolidMask))
                desired = head + to.normalized * Mathf.Max(hit.distance - .05f, .25f);
            cam.transform.SetPositionAndRotation(desired, rot);
        }
    }
}
