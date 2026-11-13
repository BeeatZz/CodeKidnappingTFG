using System;
using UnityEngine;

namespace ComputerUI
{
    // The music of the computer. It keeps playing while the player does other things on the desktop,
    // stops when the player leaves the computer, and carries on from the same place when they come back.
    // The tracks are assigned by the ComputerCanvasBuilder.
    public class ComputerMusic : MonoBehaviour
    {
        public AudioSource source;
        public AudioClip[] tracks;
        public string[] titles;

        // The player pressed Play and has not stopped it
        public bool IsPlaying { get; private set; }
        public int Index { get; private set; }
        public bool Repeat = true;

        // A different track started, or it was paused or resumed
        public event Action Changed;

        private bool paused;
        private float resumeAt;

        public bool HasTracks
        {
            get { return tracks != null && tracks.Length > 0; }
        }

        public float Length
        {
            get { return HasTracks ? tracks[Index].length : 0f; }
        }

        public float Position
        {
            get { return IsPlaying && source.clip != null ? source.time : resumeAt; }
        }

        public bool IsPaused
        {
            get { return paused; }
        }

        public string Title(int index)
        {
            return titles != null && index < titles.Length ? titles[index] : "Track " + (index + 1);
        }

        // Plays a track from the start
        public void Play(int index)
        {
            if (!HasTracks)
                return;

            Index = (index + tracks.Length) % tracks.Length;
            source.clip = tracks[Index];
            source.time = 0f;
            resumeAt = 0f;
            paused = false;
            IsPlaying = true;
            source.Play();
            RaiseChanged();
        }

        // The Play button: starts the first track if nothing played yet, pauses, or goes on
        public void TogglePause()
        {
            if (!HasTracks)
                return;

            if (!IsPlaying)
            {
                Play(Index);
                return;
            }

            if (paused)
            {
                paused = false;
                source.time = resumeAt;
                source.Play();
            }
            else
            {
                resumeAt = source.time;
                paused = true;
                source.Pause();
            }
            RaiseChanged();
        }

        public void Next()
        {
            Play(Index + 1);
        }

        // A second press near the start goes to the track before, like most players
        public void Previous()
        {
            if (IsPlaying && Position > 3f)
                Play(Index);
            else
                Play(Index - 1);
        }

        // Jumps to a place in the track, from 0 to 1
        public void Seek(float fraction)
        {
            if (!HasTracks || source.clip == null)
                return;

            resumeAt = Mathf.Clamp01(fraction) * Length;
            source.time = Mathf.Min(resumeAt, Length - 0.05f);
        }

        private void Update()
        {
            source.volume = ComputerSettings.MasterVolume * ComputerSettings.MusicVolume;

            // The track ended: the next one starts, or the music stops at the last one if it does not repeat
            if (IsPlaying && !paused && !source.isPlaying)
            {
                if (Repeat || Index < tracks.Length - 1)
                    Next();
                else
                    Stop();
            }
        }

        public void Stop()
        {
            if (source != null)
                source.Stop();
            IsPlaying = false;
            paused = false;
            resumeAt = 0f;
            RaiseChanged();
        }

        // Leaving the computer switches this object off, which stops the sound. The place is kept for when the player comes back.
        private void OnDisable()
        {
            if (IsPlaying && !paused && source != null && source.clip != null)
                resumeAt = source.time;
        }

        private void OnEnable()
        {
            if (IsPlaying && !paused && source.clip != null)
            {
                source.time = Mathf.Min(resumeAt, source.clip.length - 0.05f);
                source.Play();
            }
        }

        private void RaiseChanged()
        {
            if (Changed != null)
                Changed();
        }
    }
}
