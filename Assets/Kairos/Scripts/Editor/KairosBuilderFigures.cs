using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Kairos.EditorTools
{
    public static partial class KairosBuilder
    {
        static Transform Part(Transform parent, PrimitiveType t, Material m, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(t);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.layer = Layers.Actor;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            return go.transform;
        }

        static Transform Pivot(Transform parent, string name, Vector3 pos)
        {
            var p = new GameObject(name).transform;
            p.SetParent(parent, false); p.localPosition = pos;
            return p;
        }

        static GameObject Humanoid(string name, Material body, Material skin, Material legsMat = null, Material hat = null)
        {
            var root = new GameObject(name) { layer = Layers.Actor };
            legsMat = legsMat ? legsMat : darkMetal;
            var bodyRoot = Pivot(root.transform, "Body", Vector3.zero);
            bool guard = hat != null;
            // Torso con hombros y caderas, cuello y cabeza: una silueta de persona, no una cápsula.
            Part(bodyRoot, PrimitiveType.Capsule, body, new Vector3(0, 1.2f, 0), new Vector3(.42f, .36f, .25f));
            Part(bodyRoot, PrimitiveType.Sphere, body, new Vector3(0, 1.43f, 0), new Vector3(.5f, .18f, .26f));
            Part(bodyRoot, PrimitiveType.Sphere, legsMat, new Vector3(0, .92f, 0), new Vector3(.36f, .2f, .24f));
            Part(bodyRoot, PrimitiveType.Cylinder, skin, new Vector3(0, 1.54f, 0), new Vector3(.09f, .07f, .09f));
            var head = Part(bodyRoot, PrimitiveType.Sphere, skin, new Vector3(0, 1.69f, .01f), new Vector3(.2f, .25f, .22f));
            head.name = "Cabeza";
            if (guard)
            {
                Part(bodyRoot, PrimitiveType.Cube, vest, new Vector3(0, 1.24f, 0), new Vector3(.4f, .05f, .26f));
                Part(bodyRoot, PrimitiveType.Cube, vest, new Vector3(0, 1.08f, 0), new Vector3(.4f, .05f, .24f));
                Part(bodyRoot, PrimitiveType.Sphere, hat, new Vector3(0, 1.77f, 0), new Vector3(.22f, .13f, .24f)); // gorra
                Part(bodyRoot, PrimitiveType.Cube, hat, new Vector3(0, 1.75f, .13f), new Vector3(.16f, .02f, .12f));
            }
            else
            {
                Part(bodyRoot, PrimitiveType.Sphere, rope, new Vector3(0, 1.75f, -.01f), new Vector3(.21f, .17f, .23f)); // gorro de lana
                Part(bodyRoot, PrimitiveType.Cube, jacket, new Vector3(0, 1.2f, .115f), new Vector3(.3f, .42f, .03f));
            }
            Transform Arm(string n, float x)
            {
                var a = Pivot(bodyRoot, n, new Vector3(x, 1.46f, 0));
                Part(a, PrimitiveType.Capsule, body, new Vector3(0, -.15f, 0), new Vector3(.11f, .17f, .11f));
                Part(a, PrimitiveType.Capsule, body, new Vector3(0, -.43f, .02f), new Vector3(.09f, .16f, .09f));
                Part(a, PrimitiveType.Sphere, skin, new Vector3(0, -.62f, .03f), new Vector3(.09f, .1f, .09f));
                return a;
            }
            Transform Leg(string n, float x)
            {
                var l = Pivot(bodyRoot, n, new Vector3(x, .88f, 0));
                Part(l, PrimitiveType.Capsule, legsMat, new Vector3(0, -.2f, 0), new Vector3(.16f, .24f, .16f));
                Part(l, PrimitiveType.Capsule, legsMat, new Vector3(0, -.56f, 0), new Vector3(.12f, .22f, .12f));
                Part(l, PrimitiveType.Cube, rubber, new Vector3(0, -.84f, .05f), new Vector3(.12f, .08f, .26f)); // bota
                return l;
            }
            var armL = Arm("ArmL", -.27f); var armR = Arm("ArmR", .27f);
            var legL = Leg("LegL", -.1f); var legR = Leg("LegR", .1f);
            var w = root.AddComponent<Walker>();
            w.legL = legL; w.legR = legR; w.armL = armL; w.armR = armR; w.body = bodyRoot;
            return root;
        }

        /// Genera texturas PBR (Color.jpg y Normal.png) mapeadas a las islas UV reales de un FBX si aún no existen en disco.
        static void EnsureMeshBakedTextures(string fbxPath, string texDir, string prefix, Color bootsCol, Color pantsCol, Color jacketCol, Color skinCol, Color hairCol, Color trimCol)
        {
            string colorPath = $"{texDir}/{prefix}_Color.jpg";
            string normalPath = $"{texDir}/{prefix}_Normal.png";
            if (System.IO.File.Exists(colorPath) && System.IO.File.Exists(normalPath)) return;
            if (!System.IO.Directory.Exists(texDir)) System.IO.Directory.CreateDirectory(texDir);

            var go = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var smr = go != null ? go.GetComponentInChildren<SkinnedMeshRenderer>() : null;
            var mesh = smr != null ? smr.sharedMesh : null;
            const int size = 512;
            var colBuf = new Color[size * size];
            var norBuf = new Color[size * size];
            var filled = new bool[size * size];

            for (int i = 0; i < colBuf.Length; i++)
            {
                colBuf[i] = jacketCol;
                norBuf[i] = new Color(0.5f, 0.5f, 1f, 1f);
            }

            if (mesh != null)
            {
                var verts = mesh.vertices;
                var uvs = mesh.uv;
                var tris = mesh.triangles;
                if (uvs != null && uvs.Length == verts.Length)
                {
                    float minY = float.MaxValue, maxY = float.MinValue, maxAbsX = 0.01f;
                    for (int i = 0; i < verts.Length; i++)
                    {
                        if (verts[i].y < minY) minY = verts[i].y;
                        if (verts[i].y > maxY) maxY = verts[i].y;
                        float ax = Mathf.Abs(verts[i].x);
                        if (ax > maxAbsX) maxAbsX = ax;
                    }
                    float spanY = Mathf.Max(0.01f, maxY - minY);

                    Color VertexColor(Vector3 v)
                    {
                        float yn = (v.y - minY) / spanY;
                        float xn = Mathf.Abs(v.x) / maxAbsX;
                        if (yn < 0.16f) return bootsCol;
                        if (yn < 0.48f) return pantsCol;
                        if (yn < 0.83f)
                        {
                            if (xn > 0.62f && yn < 0.62f) return skinCol;
                            if (yn > 0.66f && yn < 0.73f && Mathf.Abs(v.x) > maxAbsX * 0.15f && Mathf.Abs(v.x) < maxAbsX * 0.38f) return trimCol;
                            return jacketCol;
                        }
                        if (yn < 0.93f) return v.z >= 0f ? skinCol : hairCol;
                        return hairCol;
                    }

                    for (int t = 0; t < tris.Length; t += 3)
                    {
                        int i0 = tris[t], i1 = tris[t + 1], i2 = tris[t + 2];
                        Vector2 uv0 = uvs[i0], uv1 = uvs[i1], uv2 = uvs[i2];
                        uv0.x = Mathf.Repeat(uv0.x, 1f); uv0.y = Mathf.Repeat(uv0.y, 1f);
                        uv1.x = Mathf.Repeat(uv1.x, 1f); uv1.y = Mathf.Repeat(uv1.y, 1f);
                        uv2.x = Mathf.Repeat(uv2.x, 1f); uv2.y = Mathf.Repeat(uv2.y, 1f);
                        Color c = (VertexColor(verts[i0]) + VertexColor(verts[i1]) + VertexColor(verts[i2])) / 3f;

                        int xMin = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(uv0.x, Mathf.Min(uv1.x, uv2.x)) * size) - 1, 0, size - 1);
                        int xMax = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(uv0.x, Mathf.Max(uv1.x, uv2.x)) * size) + 1, 0, size - 1);
                        int yMin = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(uv0.y, Mathf.Min(uv1.y, uv2.y)) * size) - 1, 0, size - 1);
                        int yMax = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(uv0.y, Mathf.Max(uv1.y, uv2.y)) * size) + 1, 0, size - 1);
                        if (xMax - xMin > size / 3 || yMax - yMin > size / 3) continue;

                        for (int py = yMin; py <= yMax; py++)
                            for (int px = xMin; px <= xMax; px++)
                            {
                                colBuf[py * size + px] = c;
                                filled[py * size + px] = true;
                            }
                    }
                }
            }

            // Añadir relieve textil y grano marino
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size, v = (y + 0.5f) / size;
                    float grain = (Fbm(u, v, 32, 32, 91, 3) - 0.5f) * 0.12f;
                    var baseC = colBuf[y * size + x];
                    colBuf[y * size + x] = new Color(
                        Mathf.Clamp01(baseC.r + grain),
                        Mathf.Clamp01(baseC.g + grain),
                        Mathf.Clamp01(baseC.b + grain), 1f);
                    float nx = (Noise(u, v, 64, 64, 41) - 0.5f) * 0.16f;
                    float ny = (Noise(u, v, 64, 64, 73) - 0.5f) * 0.16f;
                    norBuf[y * size + x] = new Color(0.5f + nx, 0.5f + ny, 0.98f, 1f);
                }

            var texC = new Texture2D(size, size, TextureFormat.RGB24, false);
            texC.SetPixels(colBuf); texC.Apply();
            System.IO.File.WriteAllBytes(colorPath, texC.EncodeToJPG(92));
            Object.DestroyImmediate(texC);

            var texN = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texN.SetPixels(norBuf); texN.Apply();
            System.IO.File.WriteAllBytes(normalPath, texN.EncodeToPNG());
            Object.DestroyImmediate(texN);

            AssetDatabase.ImportAsset(colorPath);
            AssetDatabase.ImportAsset(normalPath);
        }

        /// Guardia o Capitán: modelo humanoid 3D propio en Models/<Guardia>/ con sus texturas en Texturas/, accesorios tácticos y linterna.
        static GameObject GuardFigure(string name, Material uniformMat, Material skinTone, Material hatMat, out HumanoidMotion motion)
        {
            motion = null;
            bool isCaptain = name.ToLower().Contains("capitán") || name.ToLower().Contains("capitan");
            bool isBosun = name.ToLower().Contains("contramaestre");
            string folder = isCaptain ? "Capitan" : "Guardia";
            string dir = Root + "/Models/" + folder;
            string fbx = dir + "/" + folder + ".fbx";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(fbx) == null)
                fbx = Root + "/Models/Protagonista/Protagonista_v2.fbx";

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            if (modelAsset == null) return Humanoid(name, uniformMat, skinTone, darkMetal, hatMat);

            PrepareCharacter(fbx);
            if (isCaptain)
                EnsureMeshBakedTextures(fbx, dir + "/Texturas", "Capitan",
                    new Color(0.08f, 0.08f, 0.09f), new Color(0.10f, 0.13f, 0.18f), new Color(0.12f, 0.16f, 0.24f),
                    new Color(0.56f, 0.40f, 0.28f), new Color(0.14f, 0.13f, 0.12f), new Color(0.85f, 0.68f, 0.22f));
            else
                EnsureMeshBakedTextures(fbx, dir + "/Texturas", "Guardia",
                    new Color(0.11f, 0.11f, 0.10f), new Color(0.16f, 0.19f, 0.17f), new Color(0.18f, 0.25f, 0.21f),
                    new Color(0.52f, 0.36f, 0.25f), new Color(0.12f, 0.11f, 0.10f), new Color(0.88f, 0.55f, 0.12f));

            var root = new GameObject(name) { layer = Layers.Actor };
            var model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
            model.transform.SetParent(root.transform, false);
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layers.Actor;

            var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
            skin.sharedMaterial = CharacterMaterial(folder.ToLower(), dir);
            skin.gameObject.name = "Cuerpo";
            skin.updateWhenOffscreen = true;
            skin.forceMeshLod = 4;

            float targetHeight = isCaptain ? 1.86f : isBosun ? 1.84f : 1.79f;
            model.transform.localScale = Vector3.one * (targetHeight / skin.bounds.size.y);

            var animator = model.GetComponent<Animator>();
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(KairosAnimBuilder.ControllerPath);
            motion = model.AddComponent<HumanoidMotion>();
            motion.animator = animator;

            // Accesorios 3D distintivos sobre los huesos del rig (gorra de plato, casco de tormenta, chaleco reflectante).
            // Medidas en metros: se compensa la escala del modelo (~1,7) que heredan los huesos, si no salen
            // gigantes y separados del cuerpo.
            Transform Acc(Transform bone, PrimitiveType t, Material m, Vector3 pos, Vector3 size)
            {
                float k = 1f / bone.lossyScale.x;
                return Part(bone, t, m, pos * k, size * k);
            }
            var headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            if (headBone != null)
            {
                if (isCaptain)
                {
                    Acc(headBone, PrimitiveType.Cylinder, coat, new Vector3(0, 0.14f, 0.01f), new Vector3(0.22f, 0.04f, 0.24f));
                    Acc(headBone, PrimitiveType.Cube, yellow, new Vector3(0, 0.11f, 0.11f), new Vector3(0.16f, 0.018f, 0.08f));
                }
                else
                {
                    Acc(headBone, PrimitiveType.Sphere, isBosun ? yellow : hatMat, new Vector3(0, 0.12f, 0f), new Vector3(0.23f, 0.14f, 0.25f));
                }
            }
            var chestBone = animator.GetBoneTransform(HumanBodyBones.UpperChest) ?? animator.GetBoneTransform(HumanBodyBones.Chest);
            if (chestBone != null)
            {
                if (isCaptain)
                {
                    Acc(chestBone, PrimitiveType.Cube, yellow, new Vector3(-0.18f, 0.12f, 0f), new Vector3(0.08f, 0.025f, 0.12f));
                    Acc(chestBone, PrimitiveType.Cube, yellow, new Vector3(0.18f, 0.12f, 0f), new Vector3(0.08f, 0.025f, 0.12f));
                }
                else
                {
                    Acc(chestBone, PrimitiveType.Cube, vest, new Vector3(0f, 0.02f, 0.11f), new Vector3(0.32f, 0.045f, 0.04f));
                }
            }

            // Linterna halógena con rig de patrulla
            var rig = root.AddComponent<Kairos.Pruebas.GuardLanternPatrolRig>();
            rig.halogenColor = new Color(1.0f, 0.88f, 0.65f);
            rig.patrolIntensity = 3.0f;
            rig.alertIntensity = 4.8f;
            rig.beamRange = isCaptain ? 14f : 16f;
            rig.spotAngle = 44f;
            rig.EnsureLantern();

            return root;
        }

        /// El protagonista: modelo de Models/Protagonista, a 1,80 m y con los pies en el origen.
        static GameObject Inspector(out HumanoidMotion motion)
        {
            const string dir = Root + "/Models/Protagonista/";
            var normalImporter = (TextureImporter)AssetImporter.GetAtPath(dir + "Textures_v2/Protagonista_v2_Normal.png");
            if (normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }
            var mat = Lit("M_Protagonista", Color.white, .35f, .1f, AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "Textures_v2/Protagonista_v2_Color.jpg"));
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "Textures_v2/Protagonista_v2_Normal.png"));
            mat.SetKeyword(new LocalKeyword(mat.shader, "_NORMALMAP"), true);

            var root = new GameObject("Inspector") { layer = Layers.Actor };
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(dir + "Protagonista_v2.fbx"));
            model.transform.SetParent(root.transform, false);
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layers.Actor;

            var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
            skin.sharedMaterial = mat;
            skin.gameObject.name = "Cuerpo";
            skin.updateWhenOffscreen = true; // el modelo trae unos límites diminutos y desaparecería al acercar la cámara
            skin.forceMeshLod = 4; // 56 mil triángulos en vez de 787 mil (Mesh LOD activado en el importador del FBX)
            model.transform.localScale = Vector3.one * (1.8f / skin.bounds.size.y); // los clips ya apoyan los pies en el origen

            var animator = model.GetComponent<Animator>();
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(KairosAnimBuilder.ControllerPath);
            motion = model.AddComponent<HumanoidMotion>();
            motion.animator = animator;
            return root;
        }

        // Estatura de cada pescador, en metros.
        static readonly Dictionary<string, float> PersonHeight = new Dictionary<string, float>
        {
            { "rodel", 1.68f }, { "ari", 1.70f }, { "moussa", 1.85f }, { "thant", 1.65f },
        };

        /// Pescador con su modelo de Models/<Nombre>/, animado con los clips del protagonista.
        /// Si la carpeta no tiene modelo, vuelve a la figura simple.
        /// Modelo de reserva para la tripulación de los Sectores 2 y 3 mientras no tenga su propia carpeta en Models/:
        /// el del Sector 1 que más se acerca a su origen. Con una carpeta Models/<Nombre>/ se usa la suya.
        static readonly Dictionary<string, string>[] FallbackModel =
        {
            null,
            new Dictionary<string, string> { { "rodel", "moussa" }, { "ari", "rodel" }, { "moussa", "ari" }, { "thant", "thant" } }, // Golfo de Guinea
            new Dictionary<string, string> { { "rodel", "ari" }, { "ari", "thant" }, { "moussa", "rodel" }, { "thant", "moussa" } },      // Índico
        };

        static string[] Fbx(string dir) => AssetDatabase.IsValidFolder(dir) ? System.IO.Directory.GetFiles(dir, "*.fbx") : new string[0];

        static GameObject PersonFigure(string id, Material skinTone, out HumanoidMotion motion)
        {
            motion = null;
            var def = System.Array.Find(CaseData.PeopleFor(buildSector), p => p.id == id);
            string name = "Persona " + id, dir = Root + "/Models/" + def.name;
            var files = Fbx(dir);
            string personId = id; // la estatura es de la persona, no del modelo de reserva (si no, los clones miden igual)
            if (files.Length == 0 && buildSector > 0)
            {
                id = FallbackModel[buildSector][id];
                dir = Root + "/Models/" + System.Array.Find(CaseData.People, p => p.id == id).name;
                files = Fbx(dir);
            }
            if (files.Length == 0) return Humanoid(name, worker, skinTone);
            string fbx = files[0].Replace('\\', '/');
            PrepareCharacter(fbx);
            if (id == "ari")
                EnsureMeshBakedTextures(fbx, dir + "/Texturas", "Ari",
                    new Color(0.14f, 0.13f, 0.12f), new Color(0.18f, 0.24f, 0.31f), new Color(0.24f, 0.38f, 0.35f),
                    new Color(0.68f, 0.49f, 0.35f), new Color(0.11f, 0.10f, 0.09f), new Color(0.55f, 0.38f, 0.18f));

            var root = new GameObject(name) { layer = Layers.Actor };
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
            model.transform.SetParent(root.transform, false);
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layers.Actor;

            var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
            string matName = buildSector > 0 ? $"M_Persona_S{buildSector + 1}_{personId}" : "M_Persona_" + id;
            Color tint = CaseData.CrewOutfitTintFor(buildSector, personId);
            skin.sharedMaterial = CharacterMaterial(id, dir, matName, tint);
            skin.gameObject.name = "Cuerpo";
            skin.updateWhenOffscreen = true; // igual que el protagonista: el FBX trae límites diminutos
            skin.forceMeshLod = 4;           // unos 780 mil triángulos bajan a una fracción
            model.transform.localScale = Vector3.one * (PersonHeight[personId] / skin.bounds.size.y);

            var animator = model.GetComponent<Animator>();
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(KairosAnimBuilder.ControllerPath);
            motion = model.AddComponent<HumanoidMotion>();
            motion.animator = animator;
            return root;
        }

        /// Ajustes de importación de un personaje de Mixamo: avatar humano, sin cámara ni luz del archivo y con LOD de malla.
        static void PrepareCharacter(string fbx)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(fbx);
            if (imp.animationType == ModelImporterAnimationType.Human && imp.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel
                && !imp.importCameras && !imp.importLights && !imp.importAnimation && imp.generateMeshLods) return;
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importCameras = false; imp.importLights = false; imp.importAnimation = false;
            imp.generateMeshLods = true;
            imp.SaveAndReimport();
        }

        /// Texturas/<Id>_Color.jpg y <Id>_Normal.png a 1024 px. Sin texturas, un tono de ropa neutro.
        static Material CharacterMaterial(string id, string dir, string matName = null, Color? tint = null)
        {
            string prefix = dir + "/Texturas/" + char.ToUpper(id[0]) + id.Substring(1);
            var color = CharacterTexture(prefix + "_Color.jpg", false);
            var normal = CharacterTexture(prefix + "_Normal.png", true);
            Color baseCol = color ? (tint ?? Color.white) : (tint.HasValue ? Color.Lerp(new Color(.45f, .47f, .5f), tint.Value, .6f) : new Color(.45f, .47f, .5f));
            return Lit(matName ?? "M_Persona_" + id, baseCol, .35f, .05f, color, null, normal);
        }

        static Texture2D CharacterTexture(string path, bool normal)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) return null;
            var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            // Las texturas que se extraen del FBX llegan como cubemap: aquí se fuerza una imagen 2D.
            if (ti.textureType != type || ti.textureShape != TextureImporterShape.Texture2D || ti.maxTextureSize != 1024)
            {
                ti.textureType = type; ti.textureShape = TextureImporterShape.Texture2D; ti.maxTextureSize = 1024;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static NavMeshAgent Agent(GameObject go)
        {
            var col = go.AddComponent<CapsuleCollider>();
            col.radius = .3f; col.height = 1.8f; col.center = new Vector3(0, .9f, 0);
            var a = go.AddComponent<NavMeshAgent>();
            a.radius = .35f; a.height = 1.8f; a.angularSpeed = 300; a.acceleration = 10; a.stoppingDistance = .2f;
            a.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            a.enabled = false;
            return a;
        }
    }
}
