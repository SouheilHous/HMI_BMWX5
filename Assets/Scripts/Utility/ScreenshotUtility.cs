// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace KHI.Utility
{
    /// <summary>
    /// Utility class to aide in taking screenshots via menu items and public APIs. Screenshots can 
    /// be capture at various resolutions and with the current camera's clear color or a transparent 
    /// clear color for use in easy post compositing of images.
    /// </summary>
    public class ScreenshotUtility
    {
        private class DisposableCamera : IDisposable
        {
            private UnityEngine.Camera _disposableCamera;

            private RenderTexture _renderTexture;
            
            public DisposableCamera(UnityEngine.Camera sourceCamera, bool transparentClearColor)
            {
                _disposableCamera = new GameObject("[Disposable Camera]").AddComponent<UnityEngine.Camera>();
                _disposableCamera.orthographic = sourceCamera.orthographic;
                _disposableCamera.transform.position = sourceCamera.transform.position;
                _disposableCamera.transform.rotation = sourceCamera.transform.rotation;
                _disposableCamera.clearFlags = transparentClearColor ? CameraClearFlags.Color : sourceCamera.clearFlags;
                _disposableCamera.backgroundColor = transparentClearColor ? new Color(0.0f, 0.0f, 0.0f, 0.0f) : sourceCamera.backgroundColor;
                _disposableCamera.nearClipPlane = sourceCamera.nearClipPlane;
                _disposableCamera.farClipPlane = sourceCamera.farClipPlane;
                
                if (_disposableCamera.orthographic)
                {
                    _disposableCamera.orthographicSize = sourceCamera.orthographicSize;
                }
                else
                {
                    _disposableCamera.fieldOfView = sourceCamera.fieldOfView;
                }

            }

            public Texture2D TakeScreenshot(float scalar = 1)
            {
                var width = Mathf.RoundToInt(Screen.width * scalar);
                var height = Mathf.RoundToInt(Screen.height * scalar);
                _renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                _renderTexture.antiAliasing = 8;
                _disposableCamera.targetTexture = _renderTexture;
                
                _disposableCamera.Render();
                
                var _outputTexture = new Texture2D(_renderTexture.width, _renderTexture.height, TextureFormat.ARGB32, false);
                RenderTexture previousRenderTexture = RenderTexture.active;
                RenderTexture.active = _renderTexture;
                _outputTexture.ReadPixels(new Rect(0.0f, 0.0f, width, height), 0, 0);
                _outputTexture.Apply();
                RenderTexture.active = previousRenderTexture;

                return _outputTexture;
            }

            public void Dispose()
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(_disposableCamera.gameObject);
                    UnityEngine.Object.Destroy(_renderTexture);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(_disposableCamera.gameObject);
                    UnityEngine.Object.DestroyImmediate(_renderTexture);
                }
            }
        }
#if UNITY_EDITOR
        [MenuItem("Tools/Take Screenshot/Native Resolution")]
        private static void QuickSaveScreenshot1x()
        {
            if (QuickSaveScreenshot(GetScreenshotPath(), 1))
                EditorUtility.RevealInFinder(GetScreenshotDirectory());
        }

        [MenuItem("Tools/Take Screenshot/Native Resolution (Transparent Background)")]
        private static void QuickSaveScreenshot1xAlphaComposite()
        {
            if (QuickSaveScreenshot(GetScreenshotPath(), 1, true))
                EditorUtility.RevealInFinder(GetScreenshotDirectory());
        }

        [MenuItem("Tools/Take Screenshot/2x Resolution")]
        private static void QuickSaveScreenshot2x()
        {
            if (QuickSaveScreenshot(GetScreenshotPath(), 2))
                EditorUtility.RevealInFinder(GetScreenshotDirectory());
        }

        [MenuItem("Tools/Take Screenshot/2x Resolution (Transparent Background)")]
        private static void QuickSaveScreenshot2xAlphaComposite()
        {
            if (QuickSaveScreenshot(GetScreenshotPath(), 2, true))
                EditorUtility.RevealInFinder(GetScreenshotDirectory());
        }

        [MenuItem("Tools/Take Screenshot/4x Resolution")]
        private static void QuickSaveScreenshot4x()
        {
            if (QuickSaveScreenshot(GetScreenshotPath(), 4))
                EditorUtility.RevealInFinder(GetScreenshotDirectory());
        }

        [MenuItem("Tools/Take Screenshot/4x Resolution (Transparent Background)")]
        private static void QuickSaveScreenshot4xAlphaComposite()
        {
            if (QuickSaveScreenshot(GetScreenshotPath(), 4, true))
                EditorUtility.RevealInFinder(GetScreenshotDirectory());
        }
#endif
        public static Texture2D TakeScreenshot(UnityEngine.Camera sourceCamera)
        {
            using var disposableCamera = new DisposableCamera(sourceCamera, false);
            return disposableCamera.TakeScreenshot();
        }

        /// <summary>
        /// Captures a screenshot with the current main camera's clear color.
        /// </summary>
        /// <param name="path">The path to save the screenshot to.</param>
        /// <param name="superSize">The multiplication factor to apply to the native resolution.</param>
        /// <param name="transparentClearColor">True if the captured screenshot should have a transparent clear color. Which can be used for screenshot overlays.</param>
        /// <param name="camera">The optional camera to take the screenshot from.</param>
        /// <returns>True on successful screenshot capture, false otherwise.</returns>
        public static bool QuickSaveScreenshot(string path, int superSize = 1, bool transparentClearColor = false, UnityEngine.Camera camera = null)
        {
            if (string.IsNullOrEmpty(path) || superSize <= 0)
            {
                return false;
            }

            // If a transparent clear color isn't needed and we are capturing from the default camera, use Unity's screenshot API.
            if (!transparentClearColor && (camera == null || camera == UnityEngine.Camera.main))
            {
                ScreenCapture.CaptureScreenshot(path, superSize);
                
                Debug.LogFormat("Screenshot captured to: {0}", path);

                return true;
            }

            // Make sure we have a valid camera to render from.
            if (camera == null)
            {
                camera = UnityEngine.Camera.main;

                if (camera == null)
                {
                    Debug.Log("Failed to acquire a valid camera to capture a screenshot from.");

                    return false;
                }
            }

            bool success = false;
            using (var disposableCamera = new DisposableCamera(camera, transparentClearColor))
            {
                Texture2D screenshot = disposableCamera.TakeScreenshot(superSize);

                try
                {
                    File.WriteAllBytes(path, screenshot.EncodeToPNG());
                    success = true;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    UnityEngine.Object.Destroy(screenshot);
                }
            }

            return success;
        }

        /// <summary>
        /// Gets a directory which is safe for saving screenshots.
        /// </summary>
        /// <returns>A directory safe for saving screenshots.</returns>
        public static string GetScreenshotDirectory()
        {
            return Application.temporaryCachePath;
        }

        /// <summary>
        /// Gets a unique screenshot path with a file name based on date and time.
        /// </summary>
        /// <returns>A unique screenshot path.</returns>
        public static string GetScreenshotPath()
        {
            return Path.Combine(GetScreenshotDirectory(), $"Screenshot_{DateTime.Now:yyyy-MM-dd_hh-mm-ss-tt}.png");
        }
    }
}
