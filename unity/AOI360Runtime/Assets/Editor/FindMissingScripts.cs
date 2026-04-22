using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FindMissingScripts
{
    [MenuItem("Tools/Debug/Find Missing Scripts In Open Scene")]
    public static void FindInOpenScene()
    {
        int total = 0;
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();

        foreach (var root in roots)
        {
            total += CheckGameObjectRecursive(root);
        }

        Debug.Log($"[FindMissingScripts] Total missing scripts in open scene: {total}");
    }

    private static int CheckGameObjectRecursive(GameObject go)
    {
        int count = 0;

        Component[] components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                Debug.LogWarning(
                    $"[FindMissingScripts] Missing script found on: {GetGameObjectPath(go)}",
                    go
                );
                count++;
            }
        }

        foreach (Transform child in go.transform)
        {
            count += CheckGameObjectRecursive(child.gameObject);
        }

        return count;
    }

    private static string GetGameObjectPath(GameObject go)
    {
        string path = go.name;
        Transform current = go.transform;

        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }
}