#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class AdvancedReferenceFinder : EditorWindow
{
    GameObject targetObject;
    Vector2 scrollPos;
    List<GameObject> foundObjects = new List<GameObject>();
    bool includeInactive = true;

    [MenuItem("Tools/Advanced Reference Finder")]
    public static void ShowWindow()
    {
        GetWindow<AdvancedReferenceFinder>("Ref Finder");
    }

    void OnGUI()
    {
        GUILayout.Label("Reference Finder", EditorStyles.boldLabel);
        targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", targetObject, typeof(GameObject), true);
        includeInactive = EditorGUILayout.Toggle("Include Inactive Objects", includeInactive);

        if (GUILayout.Button("Find References"))
        {
            foundObjects.Clear();
            if (targetObject != null)
            {
                FindReferences(targetObject);
            }
        }

        if (foundObjects.Count > 0)
        {
            EditorGUILayout.Space();
            GUILayout.Label("References Found:", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
            foreach (var obj in foundObjects)
            {
                if (GUILayout.Button(GetHierarchyPath(obj), GUILayout.ExpandWidth(true)))
                {
                    EditorGUIUtility.PingObject(obj);
                    Selection.activeGameObject = obj;
                }
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Highlight References in Scene"))
            {
                HighlightFoundObjects();
            }

            if (GUILayout.Button("Export References to File"))
            {
                ExportResults();
            }
        }
    }

    void FindReferences(GameObject target)
    {
        GameObject[] allRoots = SceneManager.GetActiveScene().GetRootGameObjects();
        int count = 0;
        int total = allRoots.Length;

        foreach (GameObject root in allRoots)
        {
            if (!includeInactive && !root.activeInHierarchy) continue;

            Component[] components = root.GetComponentsInChildren<Component>(true);

            foreach (Component comp in components)
            {
                if (comp == null) continue;

                SerializedObject so = new SerializedObject(comp);
                SerializedProperty prop = so.GetIterator();

                while (prop.NextVisible(true))
                {
                    if (prop.propertyType == SerializedPropertyType.ObjectReference &&
                        prop.objectReferenceValue == target)
                    {
                        if (!foundObjects.Contains(comp.gameObject))
                            foundObjects.Add(comp.gameObject);
                        break;
                    }
                }
            }

            count++;
            EditorUtility.DisplayProgressBar("Searching References", $"Scanning {root.name}...", (float)count / total);
        }

        EditorUtility.ClearProgressBar();
        if (foundObjects.Count == 0)
        {
            EditorUtility.DisplayDialog("Reference Finder", "No references found.", "OK");
        }
    }

    string GetHierarchyPath(GameObject obj)
    {
        string path = obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = obj.name + "/" + path;
        }
        return path;
    }

    void HighlightFoundObjects()
    {
        foreach (var obj in foundObjects)
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                EditorApplication.delayCall += () =>
                {
                    var originalColor = renderer.sharedMaterial.color;
                    renderer.sharedMaterial.color = Color.yellow;
                    EditorApplication.delayCall += () => renderer.sharedMaterial.color = originalColor;
                };
            }
        }
    }

    void ExportResults()
    {
        string path = EditorUtility.SaveFilePanel("Save References", "", "ReferenceResults.txt", "txt");
        if (string.IsNullOrEmpty(path)) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var obj in foundObjects)
        {
            sb.AppendLine(GetHierarchyPath(obj));
        }
        System.IO.File.WriteAllText(path, sb.ToString());
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Reference Finder", "References exported successfully.", "OK");
    }
}
#endif
