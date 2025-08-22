using System;
using System.Media;

namespace MicControlX
{
    /// <summary>
    /// Manages sound feedback for microphone state changes
    /// </summary>
    public static class SoundFeedback
    {
        /// <summary>
        /// Play embedded sound resource
        /// </summary>
        public static void PlayEmbeddedSound(string resourceName)
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        // Use SoundPlayer for simple WAV playback from embedded resource
                        using (var player = new SoundPlayer(stream))
                        {
                            player.Play(); // Non-blocking playback
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Sound resource not found: {resourceName}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sound playback error: {ex.Message}");
            }
        }

        /// <summary>
        /// Play mute sound from embedded resource
        /// </summary>
        public static void PlayMuteSound()
        {
            PlayEmbeddedSound("mic_mute.wav");
        }

        /// <summary>
        /// Play unmute sound from embedded resource
        /// </summary>
        public static void PlayUnmuteSound()
        {
            PlayEmbeddedSound("mic_unmute.wav");
        }
    }
}
