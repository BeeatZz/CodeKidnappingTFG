using UnityEngine;

namespace ComputerUI
{
    // The sounds of the computer. The clips are assigned by the ComputerCanvasBuilder.
    // Every method does nothing if its clip is missing, so the computer also works without sounds.
    // How loud things are comes from the Settings app: the volume here is the loudest the sliders can make it.
    //
    // The mouse and the keyboard make their sounds on their own: a click when a mouse button goes down, a softer one when it
    // comes back up, and the same for every key. They do not depend on what is under the mouse or on a text box having the focus.
    public class ComputerSounds : MonoBehaviour
    {
        public AudioSource source;
        public AudioSource keySource;       // typing has its own source, because every key changes the pitch a little
        public float volume = 0.7f;

        public AudioClip clickDown;
        public AudioClip clickUp;
        public AudioClip menuOpen;
        public AudioClip windowOpen;
        public AudioClip windowClose;
        public AudioClip minimize;
        public AudioClip error;
        public AudioClip delete;
        public AudioClip restore;
        public AudioClip notify;
        public AudioClip startup;
        public AudioClip drop;

        // A different clip is picked at random for every key
        public AudioClip[] keys;
        public AudioClip[] bigKeys;         // for Space and Enter
        public AudioClip[] keyUps;

        private static bool latencyChecked;
        private bool keyWasHeld;

        public void Click() { Play(clickDown); }
        public void MenuOpen() { Play(menuOpen); }
        public void WindowOpen() { Play(windowOpen); }
        public void WindowClose() { Play(windowClose); }
        public void Minimize() { Play(minimize); }
        public void Error() { Play(error); }
        public void Delete() { Play(delete); }
        public void Restore() { Play(restore); }
        public void Notify() { Play(notify); }
        public void Startup() { Play(startup); }
        public void Drop() { Play(drop); }

        // Unity waits for a big block of audio before it plays anything, which is heard as a small delay.
        // A smaller block makes the sounds start at the moment they are played.
        private void OnEnable()
        {
            if (latencyChecked)
                return;

            latencyChecked = true;
            int length;
            int buffers;
            AudioSettings.GetDSPBufferSize(out length, out buffers);
            if (length <= 512)
                return;

            AudioConfiguration config = AudioSettings.GetConfiguration();
            config.dspBufferSize = 256;
            AudioSettings.Reset(config);
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            for (int button = 0; button < 3; button++)
            {
                if (Input.GetMouseButtonDown(button))
                    Play(clickDown);
                if (Input.GetMouseButtonUp(button))
                    Play(clickUp);
            }

            // Looking at every key each frame is only needed when a key is pressed, held or just let go
            bool held = Input.anyKey;
            if (held || keyWasHeld || Input.anyKeyDown)
                CheckKeys();
            keyWasHeld = held;
        }

        // The keys of the keyboard are the key codes below the ones of the mouse
        private void CheckKeys()
        {
            for (int code = 8; code < 320; code++)
            {
                KeyCode key = (KeyCode)code;
                if (Input.GetKeyDown(key))
                    Key(key == KeyCode.Space || key == KeyCode.Return || key == KeyCode.KeypadEnter);
                if (Input.GetKeyUp(key))
                    KeyUp();
            }
        }

        private void Play(AudioClip clip)
        {
            if (clip == null || source == null || !Application.isPlaying || !ComputerSettings.UiSounds)
                return;

            source.PlayOneShot(clip, volume * ComputerSettings.MasterVolume * ComputerSettings.UiVolume);
        }

        // One key went down
        public void Key(bool big)
        {
            AudioClip[] choices = big && bigKeys != null && bigKeys.Length > 0 ? bigKeys : keys;
            PlayKey(choices, 1f);
        }

        // One key came back up, softer than when it went down
        public void KeyUp()
        {
            PlayKey(keyUps, 0.6f);
        }

        private void PlayKey(AudioClip[] choices, float strength)
        {
            if (choices == null || choices.Length == 0 || keySource == null || !Application.isPlaying || !ComputerSettings.TypingSounds)
                return;

            // The pitch and the loudness change a little each time, so a fast typist does not hear the same sound over and over
            keySource.pitch = Random.Range(0.93f, 1.07f);
            float loudness = volume * ComputerSettings.MasterVolume * ComputerSettings.TypingVolume * strength * Random.Range(0.8f, 1f);
            keySource.PlayOneShot(choices[Random.Range(0, choices.Length)], loudness);
        }
    }
}
