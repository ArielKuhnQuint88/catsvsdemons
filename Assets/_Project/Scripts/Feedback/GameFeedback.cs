using UnityEngine;

namespace CatsVsDemons.Feedback
{
    public sealed class GameFeedback : MonoBehaviour
    {
        private static GameFeedback instance;
        private AudioSource music;
        private AudioSource effects;
        private AudioClip hit;
        private AudioClip build;
        private AudioClip heal;
        private AudioClip portal;
        private AudioClip special;
        private float nextVibration;
        private float nextHit;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() => Ensure();

        public static void PlayHit()
        {
            GameFeedback feedback = Ensure();
            if (Time.unscaledTime < feedback.nextHit) return;
            feedback.nextHit = Time.unscaledTime + 0.055f;
            feedback.Play(feedback.hit, 0.28f, false);
        }
        public static void PlayBuild() => Ensure().Play(Ensure().build, 0.55f, true);
        public static void PlayHeal() => Ensure().Play(Ensure().heal, 0.18f, false);
        public static void PlayPortal() => Ensure().Play(Ensure().portal, 0.5f, true);
        public static void PlaySpecial() => Ensure().Play(Ensure().special, 0.8f, true);

        private static GameFeedback Ensure()
        {
            if (instance != null) return instance;
            GameObject root = new("Game Feedback");
            instance = root.AddComponent<GameFeedback>();
            DontDestroyOnLoad(root);
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            music = gameObject.AddComponent<AudioSource>();
            effects = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.volume = 0.14f;
            effects.volume = 0.82f;
            hit = Tone("Sword Hit", 150f, 75f, 0.1f, true);
            build = Tone("Defense Built", 330f, 660f, 0.25f, false);
            heal = Tone("Bonsai Heal", 520f, 780f, 0.22f, false);
            portal = Tone("Portal", 210f, 920f, 0.38f, false);
            special = Tone("Samurai Wave", 110f, 880f, 0.48f, true);
            music.clip = AmbientMusic();
            music.Play();
        }

        private void Play(AudioClip clip, float volume, bool vibrate)
        {
            effects.PlayOneShot(clip, volume);
            if (vibrate && Application.isMobilePlatform &&
                PlayerPrefs.GetInt("Vibration", 1) == 1 &&
                Time.unscaledTime >= nextVibration)
            {
                nextVibration = Time.unscaledTime + 0.25f;
                Handheld.Vibrate();
            }
        }

        private static AudioClip Tone(string name, float start, float end,
            float duration, bool noise)
        {
            const int rate = 22050;
            int count = Mathf.CeilToInt(rate * duration);
            float[] data = new float[count];
            float phase = 0f;
            for (int index = 0; index < count; index++)
            {
                float t = index / (float)count;
                phase += Mathf.PI * 2f * Mathf.Lerp(start, end, t) / rate;
                float random = noise ? Random.Range(-0.18f, 0.18f) : 0f;
                data[index] = (Mathf.Sin(phase) + random) *
                    Mathf.Pow(1f - t, 2f) * 0.45f;
            }
            AudioClip clip = AudioClip.Create(name, count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip AmbientMusic()
        {
            const int rate = 22050;
            const int seconds = 12;
            float[] notes = { 110f, 164.81f, 220f, 246.94f };
            float[] data = new float[rate * seconds];
            for (int index = 0; index < data.Length; index++)
            {
                float time = index / (float)rate;
                foreach (float note in notes)
                    data[index] += Mathf.Sin(time * Mathf.PI * 2f * note) * 0.018f;
            }
            AudioClip clip = AudioClip.Create(
                "Garden Night Theme", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
