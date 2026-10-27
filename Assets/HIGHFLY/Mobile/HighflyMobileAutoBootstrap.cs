using UnityEngine;

namespace HIGHFLY.Mobile
{
    /// <summary>
    /// Auto-install the HUD in Android builds and mobile WebGL without editing
    /// original AnyRPG scenes (some addon scenes are gitignored).
    /// Set PlayerPrefs HIGHFLY_FORCE_TOUCH=1 to opt into the HUD on a PC test build.
    /// </summary>
    public static class HighflyMobileAutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallForMobile()
        {
            bool mobileBrowser = Application.platform == RuntimePlatform.WebGLPlayer
                                 && SystemInfo.deviceType == DeviceType.Handheld;
            bool mobileDevice = Application.isMobilePlatform;
            bool forcedTest = PlayerPrefs.GetInt("HIGHFLY_FORCE_TOUCH", 0) == 1;

            if (!mobileDevice && !mobileBrowser && !forcedTest)
                return;

            if (Object.FindFirstObjectByType<HighflyAnyRpgMobileHud>(FindObjectsInactive.Include) != null)
                return;

            var go = new GameObject("HIGHFLY Mobile A3 Runtime");
            go.AddComponent<HighflyAnyRpgMobileHud>();
            // This implementation waits for a valid AnyRPG player before
            // displaying buttons; it stays hidden on the game main menu.
        }
    }
}
