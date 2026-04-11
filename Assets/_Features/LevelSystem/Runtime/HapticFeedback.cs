using UnityEngine;

namespace BusAway.Gameplay
{
    /// <summary>
    /// Lightweight haptic feedback utility for mobile (Android/iOS).
    /// Falls back silently on platforms that don't support it (Editor, WebGL, etc.).
    /// 
    /// Usage:
    ///   HapticFeedback.Light();   // subtle tap — land selection
    ///   HapticFeedback.Medium();  // stronger tap — passenger boards bus
    /// </summary>
    public static class HapticFeedback
    {
        /// <summary>
        /// Light vibration — use when a valid land tap is registered.
        /// ~10ms pulse on Android, UIImpactFeedbackStyle.Light equivalent on iOS.
        /// </summary>
        public static void Light()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            TriggerAndroid(10);
#elif UNITY_IOS && !UNITY_EDITOR
            TriggerIOS(0); // UIImpactFeedbackGenerator.Light = 0
#endif
        }

        /// <summary>
        /// Medium vibration — use once per passenger that boards the bus.
        /// ~20ms pulse on Android, UIImpactFeedbackStyle.Medium equivalent on iOS.
        /// </summary>
        public static void Medium()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            TriggerAndroid(20);
#elif UNITY_IOS && !UNITY_EDITOR
            TriggerIOS(1); // UIImpactFeedbackGenerator.Medium = 1
#endif
        }

        // ─────────────────────────────────────────────────────────────────────
        // Internal Implementations
        // ─────────────────────────────────────────────────────────────────────

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _vibrator;

        private static void TriggerAndroid(long ms)
        {
            try
            {
                if (_vibrator == null)
                {
                    using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }

                // API 26+ supports amplitude-aware vibration; fallback to legacy for older devices
                int sdkInt = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
                if (sdkInt >= 26)
                {
                    using var effect = new AndroidJavaClass("android.os.VibrationEffect")
                        .CallStatic<AndroidJavaObject>("createOneShot", ms, 128); // 128 = medium amplitude
                    _vibrator.Call("vibrate", effect);
                }
                else
                {
                    _vibrator.Call("vibrate", ms);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[HapticFeedback] Android vibration failed: {e.Message}");
            }
        }
#endif

#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void _TriggerImpactFeedback(int style);

        private static void TriggerIOS(int style)
        {
            try
            {
                _TriggerImpactFeedback(style);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[HapticFeedback] iOS haptic failed: {e.Message}");
            }
        }
#endif

        // [UNITY HACK] - Force Unity to add Android VIBRATE permission to AndroidManifest.xml
        // Since we use AndroidJavaObject reflection above, Unity's IL parser doesn't detect the 
        // need for the permission and omits it from the final APK, making vibration silently fail.
        // Putting this here ensures the permission is always included without us needing a custom manifest!
        private static void ForcePermissionGeneration()
        {
            if (Application.isEditor && false)
            {
                Handheld.Vibrate();
            }
        }
    }
}
