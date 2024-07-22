using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PrefabPlacerTool : EditorWindow
{
    private List<GameObject> prefabs = new List<GameObject>();
    private int selectedPrefabIndex = 0;
    private GameObject previewObject;
    private bool isSnapping = true;
    private float snapValue = 1.0f;

    private Collider[] overlapResults = new Collider[10];

    [MenuItem("Tools/Prefab Placer Tool")]
    public static void ShowWindow()
    {
        GetWindow<PrefabPlacerTool>("Prefab Placer Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Prefab Placer Tool", EditorStyles.boldLabel);

        if (GUILayout.Button("Add Prefab"))
        {
            prefabs.Add(null);
        }

        for (int i = 0; i < prefabs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            prefabs[i] = (GameObject)EditorGUILayout.ObjectField(prefabs[i], typeof(GameObject), false);
            if (GUILayout.Button("Remove"))
            {
                prefabs.RemoveAt(i);
            }
            EditorGUILayout.EndHorizontal();
        }

        selectedPrefabIndex = EditorGUILayout.Popup("Selected Prefab", selectedPrefabIndex, GetPrefabNames());

        isSnapping = EditorGUILayout.Toggle("Enable Snapping", isSnapping);
        snapValue = EditorGUILayout.FloatField("Snap Value", snapValue);
    }

    private string[] GetPrefabNames()
    {
        List<string> names = new List<string>();
        foreach (var prefab in prefabs)
        {
            names.Add(prefab != null ? prefab.name : "None");
        }
        return names.ToArray();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (selectedPrefabIndex < 0 || selectedPrefabIndex >= prefabs.Count || prefabs[selectedPrefabIndex] == null)
            return;

        Event e = Event.current;

        if (e.type == EventType.MouseMove)
        {
            UpdatePreview(sceneView);
        }

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            PlacePrefab();
        }

        if (previewObject != null)
        {
            Handles.color = Color.green;
            Handles.DrawWireCube(previewObject.transform.position, prefabs[selectedPrefabIndex].transform.localScale);
            sceneView.Repaint();
        }
    }

    private void UpdatePreview(SceneView sceneView)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 hitPosition = hit.point;
            if (isSnapping)
            {
                hitPosition = SnapPosition(hitPosition);
            }

            if (previewObject == null)
            {
                previewObject = Instantiate(prefabs[selectedPrefabIndex], hitPosition, Quaternion.identity);
                previewObject.name = "PrefabPreview";
                previewObject.hideFlags = HideFlags.HideAndDontSave;
            }
            else
            {
                previewObject.transform.position = hitPosition;
            }
        }
    }

    private Vector3 SnapPosition(Vector3 position)
    {
        position.x = Mathf.Round(position.x / snapValue) * snapValue;
        position.y = Mathf.Round(position.y / snapValue) * snapValue;
        position.z = Mathf.Round(position.z / snapValue) * snapValue;
        return position;
    }

    private void PlacePrefab()
    {
        if (previewObject != null)
        {
            GameObject placedPrefab = Instantiate(prefabs[selectedPrefabIndex], previewObject.transform.position, previewObject.transform.rotation);
            AdjustPosition(placedPrefab);
        }
    }

    private void AdjustPosition(GameObject placedPrefab)
    {
        Collider placedCollider = placedPrefab.GetComponent<Collider>();

        if (placedCollider == null)
        {
            Debug.LogWarning("Placed prefab does not have a collider.");
            return;
        }

        int numColliders = Physics.OverlapBoxNonAlloc(
            placedCollider.bounds.center,
            placedCollider.bounds.extents,
            overlapResults,
            placedPrefab.transform.rotation);

        for (int i = 0; i < numColliders; i++)
        {
            Collider collider = overlapResults[i];
            if (collider.gameObject != placedPrefab)
            {
                Vector3 offset = Vector3.zero;

                float xOverlap = Mathf.Abs(placedCollider.bounds.min.x - collider.bounds.max.x);
                float xReverseOverlap = Mathf.Abs(placedCollider.bounds.max.x - collider.bounds.min.x);
                float zOverlap = Mathf.Abs(placedCollider.bounds.min.z - collider.bounds.max.z);
                float zReverseOverlap = Mathf.Abs(placedCollider.bounds.max.z - collider.bounds.min.z);

                if (xOverlap < snapValue || xReverseOverlap < snapValue)
                {
                    offset.x = xOverlap < xReverseOverlap ? collider.bounds.extents.x + placedCollider.bounds.extents.x : -(collider.bounds.extents.x + placedCollider.bounds.extents.x);
                }
                else if (zOverlap < snapValue || zReverseOverlap < snapValue)
                {
                    offset.z = zOverlap < zReverseOverlap ? collider.bounds.extents.z + placedCollider.bounds.extents.z : -(collider.bounds.extents.z + placedCollider.bounds.extents.z);
                }

                placedPrefab.transform.position += offset;
                break;
            }
        }
    }
}
