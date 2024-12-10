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
            if (urpAssetScene1 == null || urpAssetScene2 == null)
            {
                Debug.LogError("One or both URP Assets are not assigned. Please assign valid URP assets.");
            }
        }
        else
        {
            Debug.LogWarning("URP Asset mode is disabled. Using Renderer Index mode.");
        }
    }

    public void LoadScene1()
    {
        if (useDifferentURPAssets)
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
        if (useDifferentURPAssets)
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
}
