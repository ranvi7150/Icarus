using System.Collections.Generic;
using Icarus.Core.Audio;
using Icarus.Core.SceneManagement;
using Icarus.Gameplay.Player;
using Icarus.Gameplay.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Icarus.Editor.Validation
{
    public class StageValidator : EditorWindow
    {
        private const string WindowTitle = "Stage Validator";
        private const string StageScenePrefix = "Stage_";

        private readonly List<ValidationMessage> _messages = new List<ValidationMessage>();
        private Vector2 _scrollPosition;

        [MenuItem("Tools/Icarus/Stage Validator")]
        private static void OpenWindow()
        {
            StageValidator window = GetWindow<StageValidator>(WindowTitle);
            window.minSize = new Vector2(520f, 340f);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(WindowTitle, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Active Scene", SceneManager.GetActiveScene().name);

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Validate Current Stage", GUILayout.Height(28f)))
            {
                ValidateCurrentStage();
            }

            EditorGUILayout.Space(8f);
            DrawSummary();
            DrawMessages();
        }

        private void DrawSummary()
        {
            int errorCount = 0;
            int warningCount = 0;

            foreach (ValidationMessage message in _messages)
            {
                if (message.Type == ValidationMessageType.Error)
                {
                    errorCount++;
                }
                else if (message.Type == ValidationMessageType.Warning)
                {
                    warningCount++;
                }
            }

            EditorGUILayout.LabelField($"Errors: {errorCount}    Warnings: {warningCount}    Total: {_messages.Count}");
        }

        private void DrawMessages()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (ValidationMessage message in _messages)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(message.Type.ToString(), GUILayout.Width(70f));
                EditorGUILayout.LabelField(message.Text, EditorStyles.wordWrappedLabel);

                if (message.Context != null && GUILayout.Button("Ping", GUILayout.Width(44f)))
                {
                    EditorGUIUtility.PingObject(message.Context);
                    Selection.activeObject = message.Context;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(3f);
            }

            EditorGUILayout.EndScrollView();
        }

        private void ValidateCurrentStage()
        {
            _messages.Clear();

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                AddError("No loaded active scene was found.", null);
                Repaint();
                return;
            }

            if (!scene.name.StartsWith(StageScenePrefix))
            {
                AddWarning($"'{scene.name}' is not a stage scene. Stage validation is skipped.", null);
                LogMessagesToConsole();
                Repaint();
                return;
            }

            ValidateSingleComponent<PlayerController>(scene, "PlayerController");
            ValidateSingleComponent<StageSpawnController>(scene, "StageSpawnController");
            ValidateSingleComponent<StageTransitionController>(scene, "StageTransitionController");
            ValidateSingleComponent<SoundManager>(scene, "SoundManager");
            ValidateSingleRootGameObject(scene, "HUD");
            ValidateAtLeastOneComponent<Portal>(scene, "Portal");
            ValidateSceneInBuildSettings(scene);

            if (!HasErrors())
            {
                AddInfo($"Stage validation passed: {scene.name}", null);
            }

            LogMessagesToConsole();
            Repaint();
        }

        private void ValidateSingleComponent<T>(Scene scene, string label) where T : Component
        {
            int count = CountSceneObjects<T>(scene);
            if (count == 1)
            {
                return;
            }

            AddError($"{label} must exist exactly once in the stage scene. Found: {count}.", null);
        }

        private void ValidateAtLeastOneComponent<T>(Scene scene, string label) where T : Component
        {
            int count = CountSceneObjects<T>(scene);
            if (count == 0)
            {
                AddError($"Stage requires at least one {label}.", null);
            }
        }

        private void ValidateSingleRootGameObject(Scene scene, string objectName)
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();
            int count = 0;

            foreach (GameObject rootObject in rootObjects)
            {
                if (rootObject.name == objectName)
                {
                    count++;
                }
            }

            if (count == 1)
            {
                return;
            }

            AddError($"Root GameObject '{objectName}' must exist exactly once in the stage scene. Found: {count}.", null);
        }

        private void ValidateSceneInBuildSettings(Scene scene)
        {
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                if (buildScene.enabled && buildScene.path == scene.path)
                {
                    return;
                }
            }

            AddError($"Stage scene '{scene.name}' must be enabled in Build Settings.", null);
        }

        private void LogMessagesToConsole()
        {
            foreach (ValidationMessage message in _messages)
            {
                if (message.Type == ValidationMessageType.Error)
                {
                    Debug.LogError(message.Text, message.Context);
                }
                else if (message.Type == ValidationMessageType.Warning)
                {
                    Debug.LogWarning(message.Text, message.Context);
                }
                else
                {
                    Debug.Log(message.Text, message.Context);
                }
            }
        }

        private bool HasErrors()
        {
            foreach (ValidationMessage message in _messages)
            {
                if (message.Type == ValidationMessageType.Error)
                {
                    return true;
                }
            }

            return false;
        }

        private void AddInfo(string text, Object context)
        {
            _messages.Add(new ValidationMessage(ValidationMessageType.Info, text, context));
        }

        private void AddWarning(string text, Object context)
        {
            _messages.Add(new ValidationMessage(ValidationMessageType.Warning, text, context));
        }

        private void AddError(string text, Object context)
        {
            _messages.Add(new ValidationMessage(ValidationMessageType.Error, text, context));
        }

        private static int CountSceneObjects<T>(Scene scene) where T : Component
        {
            T[] objects = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int count = 0;

            foreach (T current in objects)
            {
                if (current.gameObject.scene == scene)
                {
                    count++;
                }
            }

            return count;
        }

        private enum ValidationMessageType
        {
            Info,
            Warning,
            Error
        }

        private readonly struct ValidationMessage
        {
            public ValidationMessage(ValidationMessageType type, string text, Object context)
            {
                Type = type;
                Text = text;
                Context = context;
            }

            public ValidationMessageType Type { get; }
            public string Text { get; }
            public Object Context { get; }
        }
    }
}
