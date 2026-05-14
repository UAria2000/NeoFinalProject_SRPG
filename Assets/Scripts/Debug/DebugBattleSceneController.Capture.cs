using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

public partial class DebugBattleSceneController
{
    private IEnumerator CaptureGameViewAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        CaptureGameViewScreenshot();
    }

    private void CaptureGameViewScreenshot()
    {
        string directory = Path.Combine(Directory.GetCurrentDirectory(), ".codex_tmp", "captures");
        Directory.CreateDirectory(directory);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        string latestPath = Path.Combine(directory, "debugbattle_gameview.png");
        string timestampedPath = Path.Combine(directory, $"debugbattle_gameview_{timestamp}.png");

        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        byte[] pngBytes = screenshot.EncodeToPNG();
        File.WriteAllBytes(latestPath, pngBytes);
        File.WriteAllBytes(timestampedPath, pngBytes);
        Destroy(screenshot);

        CleanupOldGameViewCaptures(directory, 3);

        Debug.Log($"Debug battle Game View screenshot saved: {latestPath}");
        Debug.Log($"Debug battle timestamped Game View screenshot saved: {timestampedPath}");
    }

#if UNITY_EDITOR
    public void EditorQueueGameViewCapture()
    {
        StartCoroutine(CaptureGameViewAfterFrame());
    }
#endif

    private static void CleanupOldGameViewCaptures(string directory, int keepCount)
    {
        try
        {
            FileInfo[] captures = new DirectoryInfo(directory)
                .GetFiles("debugbattle_gameview_*.png")
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ToArray();

            for (int i = keepCount; i < captures.Length; i++)
                captures[i].Delete();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to clean old debug battle Game View screenshots: {exception.Message}");
        }
    }
}
