using BeltPop.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeltPop.Editor
{
    public static class BeltPopPrototypeMenu
    {
        private const string ScenePath = "Assets/Scenes/BeltPopPrototype.unity";

        [MenuItem("BeltPop/Create Playable Prototype Scene")]
        public static void CreatePlayableScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                Debug.LogWarning("BeltPop prototype scene exists. Not overwriting: " + ScenePath);
                EditorSceneManager.OpenScene(ScenePath);
                return;
            }
            Scene previous = SceneManager.GetActiveScene();
            if (previous.IsValid() && previous.isDirty)
            {
                Debug.LogError("Current Unity scene has unsaved edits. Save it before building BeltPop prototype.");
                return;
            }
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject camera = new GameObject("Main Camera", typeof(Camera));
            camera.tag = "MainCamera";
            Camera component = camera.GetComponent<Camera>();
            component.orthographic = true;
            component.orthographicSize = 7;
            component.clearFlags = CameraClearFlags.SolidColor;
            component.backgroundColor = new Color(.96f, .97f, .99f);
            camera.transform.position = new Vector3(0, 0, -10);

            GameObject controller = new GameObject("BeltPop Game");
            controller.AddComponent<BeltPopGame>();
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("Unable to save " + ScenePath);
                return;
            }
            // Preserve existing scene entries; ensure the playable prototype is first for local testing.
            var previousBuild = EditorBuildSettings.scenes;
            var revised = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            revised.Add(new EditorBuildSettingsScene(ScenePath, true));
            foreach (var entry in previousBuild)
            {
                if (entry.path != ScenePath) revised.Add(entry);
            }
            EditorBuildSettings.scenes = revised.ToArray();
            Debug.Log("BeltPop playable prototype created at " + ScenePath);
        }
    }
}
