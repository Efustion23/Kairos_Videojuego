using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Kairos.EditorTools
{
    /// Convierte los FBX de Mixamo en clips ligeros (.anim) y crea el Animator del protagonista.
    /// Los FBX traen el modelo entero: tras esto ya no hacen falta. Menú: Kairos > Importar animaciones.
    public static class KairosAnimBuilder
    {
        const string Dir = "Assets/Kairos/Models/Protagonista/";
        const string Source = Dir + "Animaciones/", Clips = Dir + "Clips/";
        public const string ControllerPath = Dir + "Protagonista.controller";

        // Clip final, FBX de origen (el primero que exista) y si va en bucle.
        static readonly (string name, string[] files, bool loop)[] Table =
        {
            ("Idle", new[] { "Idle" }, true),
            ("Walk", new[] { "Walking", "Walking Left Turn" }, true),
            ("Run", new[] { "Running" }, true),
            ("Back", new[] { "Walking Backwards" }, true),
            ("StrafeL", new[] { "Left Strafe Walking" }, true),
            ("StrafeR", new[] { "Right Strafe Walking" }, true),
            ("TurnL", new[] { "Left Turn" }, true),
            ("TurnR", new[] { "Right Turn" }, true),
            ("Use", new[] { "Button Pushing" }, true),
            ("Type", new[] { "Typing" }, true),
            ("Talk", new[] { "Talking" }, true),
            ("Stun", new[] { "Standing Melee Punch" }, false),
            ("Kneel", new[] { "Kneeling Down" }, false),
            ("Crouch", new[] { "Crouch Idle 02 Looking Around" }, true),
        };

        static string FbxOf((string name, string[] files, bool loop) row)
        {
            foreach (var f in row.files)
                if (File.Exists(Source + f + ".fbx")) return Source + f + ".fbx";
            return null;
        }

        [MenuItem("Kairos/Importar animaciones")]
        public static void ImportAll()
        {
            ImportRange(0, Table.Length);
            BuildController();
        }

        /// Por tandas: cada FBX tarda en reimportarse porque arrastra la malla entera.
        public static string ImportRange(int from, int count)
        {
            if (!AssetDatabase.IsValidFolder(Dir + "Clips")) AssetDatabase.CreateFolder(Dir.TrimEnd('/'), "Clips");
            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(Dir + "Protagonista_v2.fbx");
            var log = new System.Text.StringBuilder();
            for (int i = from; i < Mathf.Min(from + count, Table.Length); i++)
            {
                var row = Table[i];
                var fbx = FbxOf(row);
                if (fbx == null) { log.AppendLine(row.name + ": sin FBX"); continue; }
                Import(fbx, row.name, row.loop, avatar);
                log.AppendLine(row.name + " <- " + Path.GetFileName(fbx));
            }
            AssetDatabase.SaveAssets();
            return log.ToString();
        }

        static void Import(string fbx, string name, bool loop, Avatar avatar)
        {
            float offset = name == "Walk" || name == "Run" ? LeftFootForward(fbx) : 0; // misma fase: caminar y correr se mezclan sin tropezar
            var imp = (ModelImporter)AssetImporter.GetAtPath(fbx);
            imp.importCameras = false; imp.importLights = false; imp.importAnimation = true;
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            imp.sourceAvatar = avatar;
            var c = imp.defaultClipAnimations[0];
            c.name = name;
            c.loopTime = loop; c.loopPose = loop;
            c.cycleOffset = offset;
            c.lockRootRotation = false;  // sin aplicar: el giro de "Walking Left Turn" y de las vueltas se descarta
            c.lockRootHeightY = true; c.keepOriginalPositionY = true; c.heightFromFeet = false; // la altura sí: arrodillarse baja el cuerpo
            c.lockRootPositionXZ = false;
            imp.clipAnimations = new[] { c };
            imp.SaveAndReimport();

            AnimationClip src = null;
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(fbx))
                if (a is AnimationClip clip && clip.name == name) src = clip;
            var path = Clips + name + ".anim";
            var dst = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            bool isNew = dst == null;
            if (isNew) dst = new AnimationClip();
            EditorUtility.CopySerialized(src, dst);
            dst.name = name;
            dst.hideFlags = HideFlags.None;
            if (isNew) AssetDatabase.CreateAsset(dst, path); else EditorUtility.SetDirty(dst);
        }

        /// Fase normalizada en la que el pie izquierdo está más adelantado.
        static float LeftFootForward(string fbx)
        {
            AnimationClip clip = null;
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(fbx))
                if (a is AnimationClip c && !c.name.StartsWith("__")) clip = c;
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
            var hips = go.transform.Find("mixamorig:Hips");
            var l = hips.Find("mixamorig:LeftUpLeg/mixamorig:LeftLeg/mixamorig:LeftFoot");
            var r = hips.Find("mixamorig:RightUpLeg/mixamorig:RightLeg/mixamorig:RightFoot");
            float best = -9, at = 0;
            for (int i = 0; i < 60; i++)
            {
                clip.SampleAnimation(go, clip.length * i / 60f);
                float d = Vector3.Dot(l.position - r.position, hips.forward);
                if (d > best) { best = d; at = i / 60f; }
            }
            Object.DestroyImmediate(go);
            return at;
        }

        static AnimationClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AnimationClip>(Clips + name + ".anim");

        public static void BuildController()
        {
            // Solo tronco, cabeza y brazos: "Talking" y "Typing" vienen sentados, y así las piernas siguen de pie.
            var maskPath = Dir + "TorsoYBrazos.mask";
            AssetDatabase.DeleteAsset(maskPath);
            var mask = new AvatarMask();
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++) mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            foreach (var part in new[] { AvatarMaskBodyPart.Body, AvatarMaskBodyPart.Head, AvatarMaskBodyPart.LeftArm, AvatarMaskBodyPart.RightArm, AvatarMaskBodyPart.LeftFingers, AvatarMaskBodyPart.RightFingers })
                mask.SetHumanoidBodyPartActive(part, true);
            AssetDatabase.CreateAsset(mask, maskPath);

            AssetDatabase.DeleteAsset(ControllerPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ctrl.AddParameter("VelX", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("VelZ", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("TurnRate", AnimatorControllerParameterType.Float);

            var loco = ctrl.CreateBlendTreeInController("Locomotion", out var tree);
            tree.blendType = BlendTreeType.FreeformDirectional2D;
            tree.blendParameter = "VelX"; tree.blendParameterY = "VelZ";
            // Posiciones: la velocidad real de cada clip a 1,80 m. Así el pie no patina.
            tree.AddChild(Clip("Idle"), Vector2.zero);
            tree.AddChild(Clip("Walk"), new Vector2(0, 1.3f));
            tree.AddChild(Clip("Run"), new Vector2(0, 5.45f));
            tree.AddChild(Clip("Back"), new Vector2(0, -1.05f));
            tree.AddChild(Clip("StrafeL"), new Vector2(-1.6f, 0));
            tree.AddChild(Clip("StrafeR"), new Vector2(1.6f, 0));

            var baseSm = ctrl.layers[0].stateMachine;
            baseSm.defaultState = loco;
            baseSm.AddState("Kneel").motion = Clip("Kneel");
            baseSm.AddState("Crouch").motion = Clip("Crouch");
            foreach (var n in new[] { "TurnL", "TurnR" })
            {
                var s = baseSm.AddState(n);
                s.motion = Clip(n);
                s.speedParameter = "TurnRate"; s.speedParameterActive = true;
            }

            ctrl.AddLayer("Torso");
            var layers = ctrl.layers;
            layers[0].iKPass = true; // pies sobre rampas y manos de la cámara forense
            layers[1].avatarMask = mask;
            layers[1].blendingMode = AnimatorLayerBlendingMode.Override;
            layers[1].defaultWeight = 0;
            ctrl.layers = layers;

            var torso = ctrl.layers[1].stateMachine;
            torso.defaultState = torso.AddState("Empty");
            foreach (var n in new[] { "Use", "Type", "Talk", "Stun" }) torso.AddState(n).motion = Clip(n);

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
        }
    }
}
