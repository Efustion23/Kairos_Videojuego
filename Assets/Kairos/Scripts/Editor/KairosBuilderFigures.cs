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
        static GameObject PersonFigure(string id, Material skinTone, out HumanoidMotion motion)
        {
            motion = null;
            string name = "Persona " + id, dir = Root + "/Models/" + CaseData.PersonDefOf(id).name;
            var files = AssetDatabase.IsValidFolder(dir) ? System.IO.Directory.GetFiles(dir, "*.fbx") : new string[0];
            if (files.Length == 0) return Humanoid(name, worker, skinTone);
            string fbx = files[0].Replace('\\', '/');
            PrepareCharacter(fbx);

            var root = new GameObject(name) { layer = Layers.Actor };
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
            model.transform.SetParent(root.transform, false);
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layers.Actor;

            var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
            skin.sharedMaterial = CharacterMaterial(id, dir);
            skin.gameObject.name = "Cuerpo";
            skin.updateWhenOffscreen = true; // igual que el protagonista: el FBX trae límites diminutos
            skin.forceMeshLod = 4;           // unos 780 mil triángulos bajan a una fracción
            model.transform.localScale = Vector3.one * (PersonHeight[id] / skin.bounds.size.y);

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
        static Material CharacterMaterial(string id, string dir)
        {
            string prefix = dir + "/Texturas/" + char.ToUpper(id[0]) + id.Substring(1);
            var color = CharacterTexture(prefix + "_Color.jpg", false);
            var normal = CharacterTexture(prefix + "_Normal.png", true);
            return Lit("M_Persona_" + id, color ? Color.white : new Color(.45f, .47f, .5f), .35f, .05f, color, null, normal);
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
            var a = go.AddComponent<NavMeshAgent>();
            a.radius = .35f; a.height = 1.8f; a.angularSpeed = 300; a.acceleration = 10; a.stoppingDistance = .2f;
            a.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            a.enabled = false;
            return a;
        }
    }
}
