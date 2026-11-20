using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kairos.EditorTools
{
    /// Barcos .glb (Models/Vehicles/Fuentes): cada uno llega en cientos de piezas. Se unen por material en
    /// unas pocas mallas y se guardan en Resources/Ships, que es lo que carga SectorTheme y lo único que entra en la build.
    public static partial class KairosBuilder
    {
        const string VehicleSrcDir = Root + "/Models/Vehicles/Fuentes", ShipsOut = Root + "/Resources/Ships", ShipMeshDir = GenDir + "/Ships";

        static void BakeShipVisuals()
        {
            if (!AssetDatabase.IsValidFolder(ShipMeshDir)) AssetDatabase.CreateFolder(GenDir, "Ships");
            foreach (var name in new[] { "KestrelExplorer_3D", "ReeferShip_3D", "CargoShip_3D" }) BakeShip(name);
        }

        /// Potero de flota: el Hai Feng sin luces, colisiones, textos ni objetos sueltos. Solo su geometría unida
        /// (Geo_*) y los conos de luz de los focos (Haz), que es lo que se distingue de un potero a 300 m.
        static void BakePoteroFleetShip(GameObject shipPrefab)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(shipPrefab);
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.name = "PoteroFlota_3D";
            // Primero los scripts: Unity no quita un componente del que dependa otro.
            foreach (var mb in inst.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(mb);
            foreach (var c in inst.GetComponentsInChildren<Component>(true))
            {
                if (c == null || c is Transform || c is MeshFilter) continue;
                bool keep = c is MeshRenderer && (c.name.StartsWith("Geo_") || c.name == "Haz");
                if (!keep) Object.DestroyImmediate(c);
            }
            PrefabUtility.SaveAsPrefabAsset(inst, $"{ShipsOut}/PoteroFlota_3D.prefab");
            Object.DestroyImmediate(inst);
        }

        static void BakeShip(string name)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{VehicleSrcDir}/{name}.prefab");
            if (src == null) { Debug.LogWarning($"KAIROS: falta {VehicleSrcDir}/{name}.prefab"); return; }

            // Ya horneado y sin cambios en su prefab ni en su .glb: no se repite (los modelos grandes tardan).
            string outPath = $"{ShipsOut}/{name}.prefab";
            var deps = AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(src), true);
            if (File.Exists(outPath))
            {
                var baked = File.GetLastWriteTimeUtc(outPath); bool stale = false;
                foreach (var d in deps) if (!d.EndsWith(".cs") && File.GetLastWriteTimeUtc(d) > baked) { stale = true; break; }
                if (!stale) return;
            }
            var inst = (GameObject)Object.Instantiate(src);
            var toLocal = inst.transform.worldToLocalMatrix;

            // El casco es la pieza de mayor planta; lo que cae lejos de él son restos del modelo original (planos, cámaras, piezas sueltas).
            var parts = inst.GetComponentsInChildren<MeshRenderer>(true);
            Bounds hull = default; float best = -1;
            foreach (var r in parts) { float a = r.bounds.size.x * r.bounds.size.z; if (a > best) { best = a; hull = r.bounds; } }
            var keep = new Bounds(hull.center, Vector3.Scale(hull.size, new Vector3(1.3f, 1f, 1.15f)) + Vector3.up * 60f);

            var byMat = new Dictionary<Material, List<CombineInstance>>();
            int dropped = 0;
            foreach (var r in parts)
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                if (!keep.Contains(r.bounds.center)) { dropped++; continue; }
                var mats = r.sharedMaterials;
                for (int s = 0; s < mf.sharedMesh.subMeshCount && s < mats.Length; s++)
                {
                    if (mats[s] == null || mf.sharedMesh.GetTopology(s) != MeshTopology.Triangles) continue;
                    if (!byMat.TryGetValue(mats[s], out var list)) byMat[mats[s]] = list = new List<CombineInstance>();
                    list.Add(new CombineInstance { mesh = mf.sharedMesh, subMeshIndex = s, transform = toLocal * mf.transform.localToWorldMatrix });
                }
            }
            Object.DestroyImmediate(inst);

            var root = new GameObject(name);
            int i = 0;
            foreach (var kv in byMat)
            {
                var mesh = new Mesh { name = $"{name}_{i}", indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(kv.Value.ToArray(), true, true);
                mesh.RecalculateBounds();
                MeshLodUtility.GenerateMeshLods(mesh, 6); // de lejos (la flota) se dibujan versiones con menos triángulos
                string path = $"{ShipMeshDir}/{name}_{i}.asset";
                var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (old != null) { EditorUtility.CopySerialized(mesh, old); Object.DestroyImmediate(mesh); mesh = old; } else AssetDatabase.CreateAsset(mesh, path);
                var part = new GameObject(kv.Key.name);
                part.transform.SetParent(root.transform, false);
                part.AddComponent<MeshFilter>().sharedMesh = mesh;
                part.AddComponent<MeshRenderer>().sharedMaterial = kv.Key;
                i++;
            }
            for (; AssetDatabase.LoadAssetAtPath<Mesh>($"{ShipMeshDir}/{name}_{i}.asset") != null; i++) AssetDatabase.DeleteAsset($"{ShipMeshDir}/{name}_{i}.asset");
            PrefabUtility.SaveAsPrefabAsset(root, outPath);
            Object.DestroyImmediate(root);
            Debug.Log($"KAIROS: {name}: {parts.Length} piezas unidas en {byMat.Count} mallas ({dropped} piezas sueltas descartadas).");
        }
    }
}
