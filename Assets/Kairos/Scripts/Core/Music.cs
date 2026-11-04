using UnityEngine;

namespace Kairos
{
    /// Música entre escenas: cada pista entra con un fundido sobre la anterior. Las pistas viven en Resources/Music.
    public class Music : MonoBehaviour
    {
        const float FadePerSecond = .5f;
        static Music instance;

        readonly AudioSource[] sources = new AudioSource[2];
        int active;
        string current;

        static Music Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameObject("Musica").AddComponent<Music>();
                    DontDestroyOnLoad(instance.gameObject);
                    for (int i = 0; i < 2; i++)
                    {
                        var a = instance.gameObject.AddComponent<AudioSource>();
                        a.loop = true; a.playOnAwake = false; a.ignoreListenerPause = true;
                        instance.sources[i] = a;
                    }
                }
                return instance;
            }
        }

        public static void Play(string track)
        {
            var m = Instance;
            if (m.current == track) return;
            m.current = track;
            m.active = 1 - m.active;
            var a = m.sources[m.active];
            a.clip = Resources.Load<AudioClip>("Music/" + track);
            a.volume = 0;
            a.Play();
        }

        /// Pieza corta por encima de la música, sin cortarla.
        public static void Stinger(string track) =>
            Instance.sources[Instance.active].PlayOneShot(Resources.Load<AudioClip>("Music/" + track), Game.S.musicVolume);

        void Update()
        {
            float step = FadePerSecond * Time.unscaledDeltaTime;
            for (int i = 0; i < 2; i++)
            {
                float target = i == active ? Game.S.musicVolume : 0;
                sources[i].volume = Mathf.MoveTowards(sources[i].volume, target, step);
                if (i != active && sources[i].isPlaying && sources[i].volume <= 0) sources[i].Stop();
            }
        }
    }
}
