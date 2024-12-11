using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;

public class SceneAndQualityManager : MonoBehaviour
{
    [Header("Scene Names")]
    public string scene1Name = "Scene1";
    public string scene2Name = "Scene2";

    [Header("URP Assets")]
    [Tooltip("URP Asset for Scene 1")]
    public UniversalRenderPipelineAsset urpAssetScene1; // Assign the URP asset for Scene 1 in the Inspector
    [Tooltip("URP Asset for Scene 2")]
    public UniversalRenderPipelineAsset urpAssetScene2; // Assign the URP asset for Scene 2 in the Inspector

    [Header("Mode")]
    public bool useDifferentURPAssets = true; // Toggle between setting URP Asset or Renderer Index

    [Header("Renderer Index (Optional)")]
    [Tooltip("The index in the Renderer List for Scene 1")]
    public int scene1RendererIndex = 0; // Renderer index for Scene 1
    [Tooltip("The index in the Renderer List for Scene 2")]
    public int scene2RendererIndex = 1; // Renderer index for Scene 2

    private void Start()
    {
        if (useDifferentURPAssets)
        {
            if (urpAssetScene1 == null && urpAssetScene2 == null)
            {
                Debug.LogError("Both URP Assets are not assigned. Please assign valid URP assets.");
                return;
            }

            // Apply render scale to available URP assets
            SetRenderScaleBasedOnDevice(urpAssetScene1);
            SetRenderScaleBasedOnDevice(urpAssetScene2);
        }
        else
        {
            Debug.LogWarning("URP Asset mode is disabled. Using Renderer Index mode.");
        }
    }

    public void LoadScene1()
    {
        if (useDifferentURPAssets && urpAssetScene1 != null)
        {
            SetURPAsset(urpAssetScene1);
        }
        else
        {
            SetPipelineRenderer(scene1RendererIndex);
        }

        SceneManager.LoadScene(scene1Name);
    }

    public void LoadScene2()
    {
        if (useDifferentURPAssets && urpAssetScene2 != null)
        {
            SetURPAsset(urpAssetScene2);
        }
        else
        {
            SetPipelineRenderer(scene2RendererIndex);
        }

        SceneManager.LoadScene(scene2Name);
    }

    private void SetURPAsset(UniversalRenderPipelineAsset urpAsset)
    {
        if (urpAsset != null)
        {
            GraphicsSettings.renderPipelineAsset = urpAsset;
            QualitySettings.renderPipeline = urpAsset;
            Debug.Log($"Switched to URP Asset: {urpAsset.name}");
        }
        else
        {
            Debug.LogError("URP Asset is null. Cannot switch pipeline.");
        }
    }

    private void SetPipelineRenderer(int rendererIndex)
    {
        var urpAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
        if (urpAsset == null)
        {
            Debug.LogError("Current pipeline is not a Universal Render Pipeline Asset.");
            return;
        }

        Type urpAssetType = typeof(UniversalRenderPipelineAsset);
        var defaultRendererIndexField = urpAssetType.GetField("m_DefaultRendererIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (defaultRendererIndexField == null)
        {
            Debug.LogError("Unable to find m_DefaultRendererIndex in the URP Asset.");
            return;
        }

        defaultRendererIndexField.SetValue(urpAsset, rendererIndex);
        Debug.Log($"Default Renderer Index set to {rendererIndex}.");
    }

    private void SetRenderScaleBasedOnDevice(UniversalRenderPipelineAsset urpAsset)
    {
        if (urpAsset == null) return;

        // Determine device type and memory
        string deviceType = SystemInfo.deviceType.ToString();
        float memoryInGB = SystemInfo.systemMemorySize / 1024f;

        float renderScale = 1.0f; // Default render scale

        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            // IOS logic
            if (memoryInGB >= 6)
            {
                renderScale = 1.5f;
            }
            else if (memoryInGB >= 4)
            {
                renderScale = 1.0f;
            }
            else
            {
                renderScale = 0.75f;
            }
        }
        else if (Application.platform == RuntimePlatform.Android)
        {
            // Android logic
            if (memoryInGB > 8)
            {
                renderScale = 1.5f;
            }
            else if (memoryInGB >= 5)
            {
                renderScale = 1.0f;
            }
            else
            {
                renderScale = 0.75f;
            }
        }
        else
        {
            // Default for unsupported platforms
            Debug.LogWarning("Render scale adjustment not supported for this platform.");
            renderScale = 1.0f;
        }

        urpAsset.renderScale = renderScale;
        Debug.Log($"Device Type: {deviceType}, Memory: {memoryInGB}GB, Render Scale Set To: {renderScale} for URP Asset: {urpAsset.name}");
    }
}
