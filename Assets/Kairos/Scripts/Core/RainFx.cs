using UnityEngine;

namespace Kairos
{
    /// Lluvia ligera alrededor de la cámara. El efecto en la IA está en InfiltrationController.
    public static class RainFx
    {
        /// Lo activa la lluvia de Arte y audio (MaritimeRainParticlesController): con ella en escena
        /// esta queda sin gotas para no duplicar partículas ni sonido.
        public static bool External;

        public static ParticleSystem Create(Transform follow, float rate = 900)
        {
            Shader.SetGlobalFloat("_RainDim", 1); // chubasco: las nubes tapan casi todas las estrellas
            var go = new GameObject("Rain");
            go.transform.SetParent(follow, false);
            go.transform.localPosition = new Vector3(0, 10, 4);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop();
            var main = ps.main;
            main.startLifetime = .8f; main.startSpeed = 24f; main.startSize = .018f; main.maxParticles = 2500;
            main.startColor = new Color(.6f, .75f, .85f, .35f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = External ? 0 : rate;
            // Caja plana que dispara hacia abajo: el lado fino va en Z, que es la dirección de emisión.
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(30, 30, .5f);
            sh.rotation = new Vector3(90, 0, 0);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 6f; r.velocityScale = .02f;
            r.material = Mats.Glow(new Color(.65f, .8f, .9f, .25f), additive: false);
            ps.Play();
            return ps;
        }
    }
}
