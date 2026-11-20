using UnityEditor;
using UnityEngine;

namespace Kairos.EditorTools
{
    /// Reemplaza la geometría primitiva de Lancha.prefab por Assets/Kairos/Models/Vehicles/motorboat.glb
    /// sin regenerar escenas (actualiza automáticamente 02_Sonar y 03_Infiltracion).
    public static class MotorboatIntegrator
    {
        const string GlbPath = "Assets/Kairos/Models/Vehicles/motorboat.glb";
        const string PrefabPath = "Assets/Kairos/Prefabs/Lancha.prefab";

        [MenuItem("Kairos/Aplicar modelo 3D a Lancha (motorboat.glb)")]
        public static void ApplyMotorboatToPrefab()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(GlbPath);
            if (model == null)
            {
                Debug.LogError($"No se encontró '{GlbPath}' importado como GameObject. Espera a que Unity instale com.unity.cloud.gltfast y reimporte el .glb.");
                return;
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"No se encontró '{PrefabPath}'.");
                return;
            }

            try
            {
                // Ocultar/quitar la geometría primitiva anterior pero conservar luces o puntos auxiliares
                for (int i = prefabRoot.transform.childCount - 1; i >= 0; i--)
                {
                    var child = prefabRoot.transform.GetChild(i);
                    if (child.GetComponent<Light>() != null) continue;
                    Object.DestroyImmediate(child.gameObject);
                }

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(model, prefabRoot.transform);
                inst.name = "Motorboat_3D";
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                inst.transform.localScale = Vector3.one;

                // Calcular límites y normalizar eslora a ~7.2 m con la quilla apoyada en y = -0.15
                var bounds = new Bounds(Vector3.zero, Vector3.zero);
                bool hasBounds = false;
                foreach (var mr in inst.GetComponentsInChildren<Renderer>(true))
                {
                    if (!hasBounds) { bounds = mr.bounds; hasBounds = true; }
                    else bounds.Encapsulate(mr.bounds);
                }

                if (hasBounds)
                {
                    float maxHoriz = Mathf.Max(bounds.size.x, bounds.size.z);
                    if (maxHoriz > 0.01f)
                    {
                        float scale = 7.2f / maxHoriz;
                        inst.transform.localScale = Vector3.one * scale;
                        // Si el modelo viene alineado en X en vez de Z, rotarlo 90° para que proa apunte a +Z
                        if (bounds.size.x > bounds.size.z * 1.15f)
                            inst.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    }

                    // Recalcular tras escalar/rotar para centrar en XZ y apoyar quilla en y = -0.15
                    hasBounds = false;
                    foreach (var mr in inst.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!hasBounds) { bounds = mr.bounds; hasBounds = true; }
                        else bounds.Encapsulate(mr.bounds);
                    }
                    if (hasBounds)
                    {
                        var offset = new Vector3(-bounds.center.x, -bounds.min.y - 0.15f, -bounds.center.z);
                        inst.transform.localPosition += offset;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                Debug.Log("KAIROS: Lancha.prefab actualizada con motorboat.glb (visible en 02_Sonar y 03_Infiltracion).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
    }
}
