using UnityEngine;

namespace Kairos
{
    /// Traduce lo que hace el personaje a su Animator: velocidad real para mezclar los clips de Mixamo,
    /// acciones en la capa del torso, pies sobre el suelo y manos a la cara con la cámara forense.
    public class HumanoidMotion : MonoBehaviour
    {
        public Animator animator;
        [System.NonSerialized] public bool busy, aiming, typing, talking;
        /// Cuenta las pisadas: PlayerAgent hace sonar y oír cada paso cuando el pie toca el suelo.
        public int Steps { get; private set; }

        const int TorsoLayer = 1;
        const float MaxFootDelta = .35f, StepGap = .2f, TurnClipRate = 132f; // grados/s de las vueltas de Mixamo
        static readonly int VelX = Animator.StringToHash("VelX"), VelZ = Animator.StringToHash("VelZ"), TurnRate = Animator.StringToHash("TurnRate");

        Transform body, footL, footR;
        Vector3 last;
        float lastYaw, speed, torsoWeight, aimWeight, stunT, kneelT, pelvis;
        bool leftAhead, kneeling, crouched;
        string torsoState, turnState;

        void Awake()
        {
            body = transform.parent;
            footL = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            footR = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            animator.applyRootMotion = false; // manda el CharacterController
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            last = body.position;
            lastYaw = body.eulerAngles.y;
        }

        public void PlayStun() => stunT = 1f;

        /// Ofrecer botiquín o raciones: se agacha y sigue así hasta cerrar la conversación.
        public void PlayKneel()
        {
            if (kneeling) return;
            kneeling = true; crouched = false; kneelT = 2.5f;
            animator.CrossFadeInFixedTime("Kneel", .25f, 0);
        }

        void Update()
        {
            // El diálogo pausa el juego, pero el personaje sigue hablando.
            animator.updateMode = talking ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal;
            float dt = talking ? Time.unscaledDeltaTime : Time.deltaTime;
            if (dt <= 0) return;

            var move = body.position - last; move.y = 0;
            last = body.position;
            float raw = move.magnitude / dt;
            if (raw > 12f) { move = Vector3.zero; raw = 0; } // un repliegue es un salto, no una carrera
            speed = raw;
            var local = Quaternion.Inverse(body.rotation) * (move / dt);
            animator.SetFloat(VelX, local.x, .12f, dt);
            animator.SetFloat(VelZ, local.z, .12f, dt);

            float yawRate = Mathf.DeltaAngle(lastYaw, body.eulerAngles.y) / dt;
            lastYaw = body.eulerAngles.y;

            UpdateKneel(dt);
            if (!kneeling) UpdateTurn(yawRate);
            UpdateTorso(dt);
            aimWeight = Mathf.MoveTowards(aimWeight, aiming ? 1 : 0, dt * 6f);
        }

        void UpdateKneel(float dt)
        {
            if (!kneeling) return;
            if (!talking)
            {
                kneeling = false;
                animator.CrossFadeInFixedTime("Locomotion", .45f, 0);
                return;
            }
            kneelT -= dt;
            if (kneelT <= 0 && !crouched) { crouched = true; animator.CrossFadeInFixedTime("Crouch", .3f, 0); }
        }

        /// Girar sin avanzar: da pasos en el sitio en vez de rotar deslizando los pies.
        void UpdateTurn(float yawRate)
        {
            float rate = Mathf.Abs(yawRate);
            string want = turnState;
            if (speed > .2f || talking || rate < 15f) want = null;
            else if (rate > 35f) want = yawRate > 0 ? "TurnR" : "TurnL";
            if (want != turnState)
            {
                turnState = want;
                animator.CrossFadeInFixedTime(want ?? "Locomotion", .18f, 0);
            }
            animator.SetFloat(TurnRate, Mathf.Clamp(rate / TurnClipRate, .6f, 1.8f));
        }

        void UpdateTorso(float dt)
        {
            stunT = Mathf.Max(0, stunT - dt);
            // El aturdidor es un dispositivo: se acciona con la mano, sin golpear a nadie.
            string want = kneeling ? null : stunT > 0 ? "Use" : talking ? "Talk" : busy ? (typing ? "Type" : "Use") : null;
            if (want != torsoState)
            {
                torsoState = want;
                if (want != null) animator.CrossFadeInFixedTime(want, .2f, TorsoLayer);
            }
            torsoWeight = Mathf.MoveTowards(torsoWeight, want != null ? 1 : 0, dt * 6f);
            animator.SetLayerWeight(TorsoLayer, torsoWeight);
        }

        void LateUpdate()
        {
            // La pisada llega cuando el pie que avanza queda claramente delante del otro. Vale también en rampas,
            // porque se mide en el espacio del cuerpo.
            float gap = Vector3.Dot(footL.position - footR.position, body.forward);
            if (Mathf.Abs(gap) > StepGap && gap > 0 != leftAhead)
            {
                leftAhead = gap > 0;
                if (speed > .5f) Steps++;
            }
        }

        void OnAnimatorIK(int layer)
        {
            if (layer != 0) return;
            // Los clips asumen suelo plano a la altura del personaje: se corrige en rampas y bordes.
            float dl = FootOffset(AvatarIKGoal.LeftFoot), dr = FootOffset(AvatarIKGoal.RightFoot);
            pelvis = Mathf.Lerp(pelvis, Mathf.Min(0, Mathf.Min(dl, dr)), .2f);
            animator.bodyPosition += Vector3.up * pelvis;
            Plant(AvatarIKGoal.LeftFoot, dl);
            Plant(AvatarIKGoal.RightFoot, dr);

            HandToCamera(AvatarIKGoal.LeftHand, -.09f);
            HandToCamera(AvatarIKGoal.RightHand, .09f);
        }

        float FootOffset(AvatarIKGoal foot)
        {
            var p = animator.GetIKPosition(foot);
            if (!Physics.Raycast(p + Vector3.up * MaxFootDelta, Vector3.down, out var hit, MaxFootDelta * 2.2f, Layers.SolidMask)) return 0;
            float delta = hit.point.y - body.position.y;
            return Mathf.Abs(delta) > MaxFootDelta ? 0 : delta;
        }

        void Plant(AvatarIKGoal foot, float delta)
        {
            animator.SetIKPositionWeight(foot, 1);
            animator.SetIKPosition(foot, animator.GetIKPosition(foot) + Vector3.up * delta);
        }

        /// No hay clip de sostener la cámara: las manos suben hasta ella, a la altura de los ojos.
        void HandToCamera(AvatarIKGoal hand, float side)
        {
            animator.SetIKPositionWeight(hand, aimWeight);
            animator.SetIKRotationWeight(hand, aimWeight);
            if (aimWeight <= 0) return;
            animator.SetIKPosition(hand, animator.bodyPosition + animator.bodyRotation * new Vector3(side, .55f, .34f));
            animator.SetIKRotation(hand, animator.bodyRotation * Quaternion.Euler(0, side > 0 ? -70 : 70, side > 0 ? 90 : -90));
        }
    }
}
