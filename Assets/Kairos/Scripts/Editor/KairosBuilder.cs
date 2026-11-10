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
    /// Genera materiales, el buque Hai Feng 77 y las 5 escenas. Menú: Kairos > Generar juego.
    /// Coordenadas del buque: proa +Z, popa -Z, babor -X. Cubierta principal y=6, cubierta inferior y=3.
    public static partial class KairosBuilder
    {
        const string Root = "Assets/Kairos", ScenesDir = Root + "/Scenes", GenDir = Root + "/Generated";

        // ---------- Materiales ----------
        static Material steel, rust, deck, paint, floor, crate, frozen, skinA, skinB, skinC, skinD, coat, suit, worker, bulb, cyanGlow, darkMetal, windowMat;

        /// Con normal, el canal alfa de la textura lleva la lisura: lo oxidado es mate y lo mojado brilla.
        static Material Lit(string name, Color c, float smooth = .3f, float metal = .5f, Texture2D tex = null, Color? emission = null, Texture2D normal = null)
        {
            string path = $"{GenDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            if (tex != null) m.SetTexture("_BaseMap", tex);
            if (emission != null)
            {
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.SetKeyword(new LocalKeyword(m.shader, "_EMISSION"), true);
                m.SetColor("_EmissionColor", emission.Value);
            }
            if (normal != null)
            {
                m.SetTexture("_BumpMap", normal);
                m.SetKeyword(new LocalKeyword(m.shader, "_NORMALMAP"), true);
                m.SetFloat("_SmoothnessTextureChannel", 1);
                m.SetKeyword(new LocalKeyword(m.shader, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A"), true);
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        // ---------- Escenas ----------
        static Scene NewScene(string name)
        {
            var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            s.name = name;
            return s;
        }

        static Camera MainCamera(float far = 4000)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var c = go.AddComponent<Camera>();
            c.farClipPlane = far; c.nearClipPlane = .15f; c.fieldOfView = 58;
            c.allowHDR = true;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            go.AddComponent<AudioListener>();
            return c;
        }

        static VolumeProfile nightProfile;
        static void PostVolume()
        {
            var go = new GameObject("Global Volume");
            var v = go.AddComponent<Volume>();
            v.isGlobal = true; v.sharedProfile = nightProfile;
        }

        static void BuildProfile()
        {
            string path = $"{GenDir}/NightProfile.asset";
            AssetDatabase.DeleteAsset(path);
            nightProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(nightProfile, path);
            var bloom = nightProfile.Add<Bloom>(true);
            bloom.intensity.Override(.9f); bloom.threshold.Override(.95f); bloom.scatter.Override(.72f);
            var tone = nightProfile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var vig = nightProfile.Add<Vignette>(true);
            vig.intensity.Override(.3f); vig.smoothness.Override(.5f);
            var ca = nightProfile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(.6f); ca.contrast.Override(10f); ca.saturation.Override(-4f);
            GradePost(nightProfile);
            foreach (var c in nightProfile.components) AssetDatabase.AddObjectToAsset(c, nightProfile);
            EditorUtility.SetDirty(nightProfile);
        }

        static Material skyMat, oceanMat;

        static void NightWorld(float clouds, float douglas, Color? ambient = null)
        {
            var moon = new GameObject("Luna");
            var l = moon.AddComponent<Light>();
            l.type = LightType.Directional; l.color = new Color(.66f, .78f, .95f); l.intensity = 1.25f;
            l.shadows = LightShadows.Soft; l.shadowStrength = .75f;
            moon.AddComponent<UniversalAdditionalLightData>();
            var moonDir = new Vector3(.45f, .55f, .7f).normalized;
            moon.transform.rotation = Quaternion.LookRotation(-moonDir);

            if (skyMat == null)
            {
                string sp = $"{GenDir}/M_NightSky.mat";
                skyMat = AssetDatabase.LoadAssetAtPath<Material>(sp);
                if (skyMat == null) { skyMat = new Material(Shader.Find("Kairos/NightSky")); AssetDatabase.CreateAsset(skyMat, sp); }
                skyMat.SetVector("_MoonDir", moonDir);
                skyMat.SetColor("_Horizon", Pal.Storm);
                skyMat.SetFloat("_Clouds", clouds);
                EditorUtility.SetDirty(skyMat);
                string op = $"{GenDir}/M_Ocean.mat";
                oceanMat = AssetDatabase.LoadAssetAtPath<Material>(op);
                if (oceanMat == null) { oceanMat = new Material(Shader.Find("Kairos/Ocean")); AssetDatabase.CreateAsset(oceanMat, op); }
                oceanMat.SetColor("_Deep", Pal.Ocean);
                oceanMat.SetColor("_SkyTint", Pal.Storm);
                oceanMat.SetTexture("_WaterNormal", waterNormal);
                EditorUtility.SetDirty(oceanMat);
            }
            RenderSettings.skybox = skyMat;
            RenderSettings.sun = l;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient ?? new Color(.22f, .28f, .34f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.Lerp(Pal.Storm, Pal.Ocean, .35f);
            RenderSettings.fogDensity = .0022f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;

            var o = new GameObject("Ocean", typeof(MeshFilter), typeof(MeshRenderer));
            o.GetComponent<MeshRenderer>().sharedMaterial = oceanMat;
            o.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var ocean = o.AddComponent<Ocean>();
            ocean.douglasAtStart = douglas;
            ocean.ambient = RenderSettings.ambientLight;
            Fleet();
            PostVolume();
        }

        static void SaveScene(Scene s, string name, List<EditorBuildSettingsScene> list)
        {
            string path = $"{ScenesDir}/{name}.unity";
            EditorSceneManager.SaveScene(s, path);
            list.Add(new EditorBuildSettingsScene(path, true));
        }

        static GameObject Boat() => (GameObject)PrefabUtility.InstantiatePrefab(BoatPrefab());

        [MenuItem("Kairos/Generar juego")]
        public static void BuildAll()
        {
            if (Application.isPlaying) { Debug.LogError("KAIROS: sal de Play Mode antes de generar."); return; }
            EnsureLayers();
            foreach (var dir in new[] { Root + "/Generated", Root + "/Prefabs", Root + "/Scenes", Root + "/Resources" })
                if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(Root, System.IO.Path.GetFileName(dir));

            BuildMaterials();
            BuildProfile();
            skyMat = null;
            boatPrefab = null;
            var shipPrefab = BuildShip();

            var scenes = new List<EditorBuildSettingsScene>();
            BuildMenuScene(shipPrefab, scenes);
            BuildRastreoScene(scenes);
            BuildSonarScene(shipPrefab, scenes);
            BuildInfiltrationScene(shipPrefab, scenes);
            BuildJuicioScene(scenes);
            EditorBuildSettings.scenes = scenes.ToArray();

            PlayerSettings.productName = "KAIROS Sombras en Altamar";
            PlayerSettings.companyName = "Unidad KAIROS";
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(scenes[0].path);
            Debug.Log("KAIROS: juego generado (" + scenes.Count + " escenas).");
        }

        static void BuildMenuScene(GameObject ship, List<EditorBuildSettingsScene> list)
        {
            var s = NewScene("00_Menu");
            var cam = MainCamera();
            NightWorld(.55f, 3.5f);
            PrefabUtility.InstantiatePrefab(ship);
            var go = new GameObject("Menu");
            go.AddComponent<MenuController>().cam = cam;
            SaveScene(s, "00_Menu", list);
        }

        static void BuildRastreoScene(List<EditorBuildSettingsScene> list)
        {
            var s = NewScene("01_Rastreo");
            var cam = MainCamera(50);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.01f, .02f, .035f);
            cam.nearClipPlane = .005f; cam.fieldOfView = 40;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;
            PostVolume();
            var go = new GameObject("Rastreo");
            go.AddComponent<RastreoController>().cam = cam;
            SaveScene(s, "01_Rastreo", list);
        }

        static void BuildSonarScene(GameObject ship, List<EditorBuildSettingsScene> list)
        {
            var s = NewScene("02_Sonar");
            var cam = MainCamera();
            NightWorld(.55f, 4f);
            var sh = (GameObject)PrefabUtility.InstantiatePrefab(ship);
            var boat = Boat();
            var go = new GameObject("Sonar");
            var c = go.AddComponent<SonarController>();
            c.cam = cam; c.ship = sh.transform; c.boat = boat.transform;
            SaveScene(s, "02_Sonar", list);
        }

        static Transform Waypoint(Transform parent, string name, Vector3 p)
        {
            var w = new GameObject(name).transform;
            w.SetParent(parent, false); w.position = p;
            return w;
        }

        static GuardAI Guard(Transform parent, string name, Vector3 start, Material hat, Vector3[] route)
        {
            var go = Humanoid(name, coat, skinD, darkMetal, hat);
            go.transform.SetParent(parent, false);
            go.transform.position = start;
            Agent(go);
            var g = go.AddComponent<GuardAI>();
            g.displayName = name;
            var wps = new List<Transform>();
            for (int i = 0; i < route.Length; i++) wps.Add(Waypoint(parent, $"{name}_wp{i}", route[i]));
            g.route = wps.ToArray();
            return g;
        }

        static PersonNpc Person(Transform parent, string id, Vector3 pos, float yaw, Material skin)
        {
            var go = PersonFigure(id, skin, out var motion);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            Agent(go);
            var n = go.AddComponent<PersonNpc>();
            n.personId = id;
            n.motion = motion;
            Trigger(go.transform, Vector3.up, new Vector3(1.2f, 2f, 1.2f));
            return n;
        }

        static void Trigger(Transform parent, Vector3 local, Vector3 size)
        {
            var t = new GameObject("Trigger") { layer = Layers.Interact };
            t.transform.SetParent(parent, false);
            t.transform.localPosition = local;
            var c = t.AddComponent<BoxCollider>(); c.size = size; c.isTrigger = true;
        }

        static EvidenceProp Evidence(Transform parent, string id, Vector3 pos, Vector3 scale, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = "Prueba " + id;
            go.transform.SetParent(parent, false);
            go.transform.position = pos; go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            go.layer = Layers.Interact;
            var e = go.AddComponent<EvidenceProp>();
            e.evidenceId = id;
            var trig = new GameObject("Trigger") { layer = Layers.Interact };
            trig.transform.SetParent(go.transform, false);
            trig.transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            var c = trig.AddComponent<BoxCollider>(); c.size = Vector3.one * 1.3f; c.isTrigger = true;

            // Marca verde discreta: se documenta con la cámara.
            var mark = new GameObject("Luz prueba");
            mark.transform.SetParent(go.transform, false);
            mark.transform.position = pos + Vector3.up * .4f;
            var l = mark.AddComponent<Light>();
            l.type = LightType.Point; l.color = Pal.Sealed; l.range = 2.2f; l.intensity = .9f; l.shadows = LightShadows.None;
            DressEvidence(e, scale);
            return e;
        }

        static LockedDoor Door(Transform parent, string id, string doorName, Vector3 pos, Vector3 size, float seconds, bool cuttable, bool compressors, params string[] frees)
        {
            var solid = new GameObject("Hoja " + id) { layer = Layers.ShipSolid };
            solid.transform.SetParent(parent, false);
            solid.transform.position = pos;
            var bc = solid.AddComponent<BoxCollider>(); bc.size = size;
            var vis = Part(solid.transform, PrimitiveType.Cube, rust, Vector3.zero, size);
            vis.gameObject.layer = Layers.ShipSolid;
            var lockVis = Part(solid.transform, PrimitiveType.Cube, bulb, new Vector3(size.x > size.z ? 0 : Mathf.Sign(-pos.x) * .15f, .1f, size.x > size.z ? -.15f : 0), new Vector3(.18f, .24f, .18f));
            lockVis.gameObject.layer = Layers.ShipSolid;

            var go = new GameObject("Puerta " + id) { layer = Layers.Interact };
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var c = go.AddComponent<BoxCollider>(); c.size = size + Vector3.one * 1.2f; c.isTrigger = true;
            var d = go.AddComponent<LockedDoor>();
            d.doorId = id; d.doorName = doorName; d.cutSeconds = seconds; d.cuttable = cuttable;
            d.nearCompressors = compressors; d.solid = solid; d.freesPersons = frees;
            return d;
        }

        static void BuildInfiltrationScene(GameObject shipPrefab, List<EditorBuildSettingsScene> list)
        {
            var s = NewScene("03_Infiltracion");
            var cam = MainCamera();
            NightWorld(.75f, 4.5f, new Color(.24f, .29f, .34f));
            var ship = (GameObject)PrefabUtility.InstantiatePrefab(shipPrefab);
            var gameplay = new GameObject("Gameplay").transform;

            var surfaceGo = new GameObject("NavMesh");
            var surface = surfaceGo.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = 1 << Layers.ShipSolid;
            surface.overrideVoxelSize = true; surface.voxelSize = .1f;

            // Jugador
            var player = Inspector(out var motion);
            player.transform.position = new Vector3(2.5f, 6.1f, -38.5f); // lejos de la salida: no se ofrece nada más subir
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = .3f; cc.center = new Vector3(0, .95f, 0); cc.stepOffset = .35f; cc.slopeLimit = 50;
            var agent = player.AddComponent<PlayerAgent>();
            agent.cam = cam; agent.motion = motion;

            // Guardias: rondas en bucle con huecos para colarse.
            var guards = new List<GuardAI>();
            guards.Add(Guard(gameplay, "Contramaestre", new Vector3(-6, 6.05f, -24), bulb, new[] {
                new Vector3(-6, 6, -23), new Vector3(-6, 6, 1), new Vector3(0, 6, 8), new Vector3(0, 6, 34), new Vector3(5.8f, 6, 12), new Vector3(0, 6, 4), new Vector3(-6, 6, -6), new Vector3(0, 6, -24), new Vector3(0, 6, -37), new Vector3(3, 6, -29) }));
            guards.Add(Guard(gameplay, "Guardia de bodega", new Vector3(0, 3.05f, -26), darkMetal, new[] {
                new Vector3(0, 3, -26), new Vector3(3.6f, 3, -6), new Vector3(0, 3, 0), new Vector3(0, 3, 19), new Vector3(3.2f, 3, 12), new Vector3(0, 3, 8), new Vector3(-3.5f, 3, -20), new Vector3(0, 3, -12) }));
            guards.Add(Guard(gameplay, "Guardia de habilitación", new Vector3(0, 6.05f, -18), darkMetal, new[] {
                new Vector3(0, 6, -18), new Vector3(0, 6, 0), new Vector3(3.2f, 6, -8), new Vector3(0, 6, 4), new Vector3(6, 6, 3), new Vector3(0, 6, -22), new Vector3(0, 6, -9) }));
            var captain = Guard(gameplay, "Capitán", new Vector3(0, 9.15f, -7), coat, new[] {
                new Vector3(0, 9.1f, -7), new Vector3(-2, 9.1f, -11), new Vector3(2.5f, 9.1f, -9) });
            guards.Add(captain);
            captain.viewRange = 9;

            // Cámara del barco: vigila la cubierta de popa; el pasillo inferior queda libre.
            var cam1 = new GameObject("Camara popa").AddComponent<SecurityCam>();
            cam1.transform.SetParent(gameplay, false);
            cam1.transform.SetPositionAndRotation(new Vector3(0, 8.6f, -20.4f), Quaternion.Euler(0, 180, 0));
            cam1.floorY = 6f; cam1.range = 12f; cam1.alarmName = "La cámara de popa";
            Part(cam1.transform, PrimitiveType.Cube, darkMetal, Vector3.zero, new Vector3(.18f, .18f, .4f));

            // Puertas cerradas (encajan en los huecos de los mamparos)
            var doorSize = new Vector3(.3f, 2.2f, 1.4f);
            var doors = new List<LockedDoor>
            {
                Door(gameplay, "babor", "escotilla de babor", new Vector3(-1.25f, 4.1f, 2), doorSize, 6f, true, false, "ari", "moussa", "thant"),
                Door(gameplay, "servicio", "puerta de servicio", new Vector3(-4f, 4.1f, -6), new Vector3(1.4f, 2.2f, .3f), 6f, false, false),
                Door(gameplay, "capitan", "puerta del capitán", new Vector3(-1.25f, 7.1f, -16), doorSize, 6f, true, false),
                Door(gameplay, "oficial", "puerta del oficial", new Vector3(1.25f, 7.1f, -16), doorSize, 4f, true, false),
                Door(gameplay, "maquinas", "reja de máquinas", new Vector3(1.25f, 4.1f, -21), doorSize, 5f, true, true),
            };

            // Personas
            var people = new List<PersonNpc>
            {
                Person(gameplay, "rodel", new Vector3(-3f, 6.05f, -9.5f), 40, skinA),
                Person(gameplay, "ari", new Vector3(-3.3f, 3.05f, 5f), 110, skinC),
                Person(gameplay, "moussa", new Vector3(-3.8f, 3.05f, -2.5f), 70, skinB),
                Person(gameplay, "thant", new Vector3(-3.2f, 3.05f, 1f), 100, skinD),
            };

            // Pruebas
            Surface("Doc", 256, 4, Document, out var docC, out var docN);
            var paper = Lit("M_Paper", Color.white, 1f, 0, docC, null, docN);
            var ev = new List<EvidenceProp>
            {
                Evidence(gameplay, "e_celda", new Vector3(-6.3f, 4.2f, 3.5f), new Vector3(.08f, .8f, 1.2f), rust),
                Evidence(gameplay, "e_pasaportes", new Vector3(-3.95f, 7.35f, -14f), new Vector3(.3f, .25f, .5f), paper),
                Evidence(gameplay, "e_deudas", new Vector3(2.9f, 6.95f, -18.8f), new Vector3(.5f, .08f, .7f), paper),
                Evidence(gameplay, "e_contratos", new Vector3(-3.2f, 6.85f, -5f), new Vector3(.6f, .06f, .9f), paper),
                // En el camarote del oficial: el camino principal no obliga a pasar por el capitán.
                Evidence(gameplay, "e_manifiesto", new Vector3(4.1f, 6.95f, -18.8f), new Vector3(.7f, .05f, .6f), paper),
                Evidence(gameplay, "e_registro", new Vector3(-3.5f, 6.95f, -18.8f), new Vector3(.7f, .1f, .5f), paper),
                Evidence(gameplay, "e_turnos", new Vector3(4.86f, 7.5f, -7f), new Vector3(.06f, .9f, .7f), paper),
                Evidence(gameplay, "e_candados", new Vector3(-1.05f, 4.3f, 1.05f), new Vector3(.15f, .35f, .3f), darkMetal),
                Evidence(gameplay, "e_facturas", new Vector3(6.66f, 4.3f, -21f), new Vector3(.06f, .7f, .5f), paper),
            };

            // Consola del puente y salida
            var console = new GameObject("Consola") { layer = Layers.Interact };
            console.transform.SetParent(gameplay, false);
            console.transform.position = new Vector3(0, 10f, -5.8f);
            var cc2 = console.AddComponent<BoxCollider>(); cc2.size = new Vector3(5, 2, 1.6f); cc2.isTrigger = true;
            console.AddComponent<BridgeConsole>();

            var exit = new GameObject("Salida popa") { layer = Layers.Interact };
            exit.transform.SetParent(gameplay, false);
            exit.transform.position = new Vector3(0, 7f, -44f);
            var ec = exit.AddComponent<BoxCollider>(); ec.size = new Vector3(6, 2.5f, 2.2f); ec.isTrigger = true;
            exit.AddComponent<ExitZone>();

            var boat = Boat();
            boat.transform.position = new Vector3(0, 0, -52);

            var compressor = new GameObject("Compresores").transform;
            compressor.position = new Vector3(3.9f, 4.2f, -21f);

            // Controlador
            var ctrl = new GameObject("Infiltracion").AddComponent<InfiltrationController>();
            ctrl.cam = cam; ctrl.player = agent; ctrl.surface = surface; ctrl.guards = guards.ToArray(); ctrl.captain = captain;
            ctrl.people = people.ToArray(); ctrl.evidenceProps = ev.ToArray(); ctrl.doors = doors.ToArray();
            ctrl.boat = boat.transform; ctrl.compressorRoom = compressor;
            var lights = new List<Light>();
            foreach (var l in ship.GetComponentsInChildren<Light>())
                if (l.name.StartsWith("Foco") || l.name == "Luz Tejado") lights.Add(l);
            ctrl.dangerLights = lights.ToArray();

            SaveScene(s, "03_Infiltracion", list);
        }

        static void BuildJuicioScene(List<EditorBuildSettingsScene> list)
        {
            var s = NewScene("04_Juicio");
            var cam = MainCamera(50);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.01f, .03f, .05f);
            RenderSettings.fog = false;
            new GameObject("Juicio").AddComponent<JuicioController>();
            SaveScene(s, "04_Juicio", list);
        }

        static void EnsureLayers()
        {
            var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tm.FindProperty("layers");
            void Set(int i, string n) { layers.GetArrayElementAtIndex(i).stringValue = n; }
            Set(Layers.ShipSolid, "ShipSolid"); Set(Layers.Actor, "Actor"); Set(Layers.Thermal, "Thermal");
            Set(Layers.Interact, "Interact"); Set(Layers.HullShell, "HullShell");
            tm.ApplyModifiedProperties();
        }
    }
}
