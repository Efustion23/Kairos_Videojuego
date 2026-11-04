using UnityEngine;

namespace Kairos
{
    public abstract class Interactable : MonoBehaviour
    {
        public abstract string Prompt { get; }
        /// 0 = pulsar; más de 0 = mantener esos segundos.
        public virtual float Hold => 0;
        public virtual bool CanUse(out string why) { why = null; return true; }
        public virtual void Holding() { }
        public abstract void Use();
        [System.NonSerialized] public float progress;
    }
}
