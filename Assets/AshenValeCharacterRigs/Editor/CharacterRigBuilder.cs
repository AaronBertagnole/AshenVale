using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AshenVale.CharacterRigs.Editor
{
    public static class CharacterRigBuilder
    {
        const string Root = "Assets/AshenValeCharacterRigs";
        static readonly string[] Names = { "Head", "Torso", "Pelvis", "NearUpperArm", "NearForearm", "FarUpperArm", "FarForearm", "NearThigh", "NearShin", "FarThigh", "FarShin", "Weapon" };
        [MenuItem("Tools/Ashen Vale/Characters/Create Rig Preview")]
        public static void CreatePreview()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Root + "/Generated"); AssetDatabase.Refresh();
            string folder = AssetDatabase.GenerateUniqueAssetPath(Root + "/Generated/Preview_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 2.8f;
            camera.transform.position = new Vector3(0, 1.8f, -10); camera.backgroundColor = new Color(.035f, .075f, .085f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            var rigs = new CutoutCharacterRig[2];
            rigs[0] = Make("Ranger", true, folder, -2);
            rigs[1] = Make("Warrior", false, folder, 2);
            var controls = new GameObject("Preview Controls").AddComponent<RigPreviewControls>(); controls.rigs = rigs;
            controls.rangerMaster = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Ranger_Master.png");
            controls.warriorMaster = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Warrior_Master.png");
            EditorSceneManager.SaveScene(scene, folder + "/CharacterRigPreview.unity");
            AssetDatabase.SaveAssets(); Selection.activeGameObject = rigs[0].gameObject;
            Debug.Log("Created character rig preview in " + folder + ". Press Play and toggle Idle/Walk. Inspect joints before gameplay integration.");
        }
        static CutoutCharacterRig Make(string name, bool ranger, string folder, float x)
        {
            string path = Root + "/Art/" + name + "_Parts.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (!importer) throw new InvalidOperationException("Missing parts texture: " + path);
            if (!importer.isReadable) { importer.isReadable = true; importer.SaveAndReimport(); }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var pixels = texture.GetPixels32();
            var definition = ScriptableObject.CreateInstance<CharacterRigDefinition>(); definition.ranger = ranger;
            definition.headHeight = ranger ? 1.2f : 1.05f;
            definition.pelvisHeight = ranger ? .65f : .70f;
            definition.weaponHeight = ranger ? 2.5f : 3.95f;
            AssetDatabase.CreateAsset(definition, folder + "/" + name + ".asset");
            for (int i = 0; i < 12; i++)
            {
                Rect bounds = FindBounds(texture, pixels, i);
                var sprite = Sprite.Create(texture, bounds, Pivot(i, ranger), 100, 0, SpriteMeshType.FullRect);
                sprite.name = Names[i]; AssetDatabase.AddObjectToAsset(sprite, definition); definition.parts[i] = sprite;
            }
            definition.nearSole = SolePoints(texture, pixels, definition.parts[8]);
            definition.farSole = SolePoints(texture, pixels, definition.parts[10]);
            MakeFootSlices(texture, pixels, definition, true);
            MakeFootSlices(texture, pixels, definition, false);
            if (!ranger)
            {
                string capePath = Root + "/Art/Warrior_Cape.png";
                var capeImporter = (TextureImporter)AssetImporter.GetAtPath(capePath);
                if (!capeImporter.isReadable) { capeImporter.isReadable = true; capeImporter.SaveAndReimport(); }
                var capeTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(capePath);
                var capeBounds = FindBounds(capeTexture, capeTexture.GetPixels32(), 0, 1, 1);
                var capeSprite = Sprite.Create(capeTexture, capeBounds, new Vector2(.82f, .95f), 100, 0, SpriteMeshType.FullRect);
                capeSprite.name = "Cape"; AssetDatabase.AddObjectToAsset(capeSprite, definition); definition.cape = capeSprite;
            }
            EditorUtility.SetDirty(definition); AssetDatabase.SaveAssets();
            var rig = new GameObject(name + " Rig").AddComponent<CutoutCharacterRig>(); rig.Build(definition);
            var idle = Bake(rig, false, folder + "/" + name + "_Idle.anim");
            var walk = Bake(rig, true, folder + "/" + name + "_Walk.anim");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(folder + "/" + name + ".controller");
            controller.AddParameter("Walking", AnimatorControllerParameterType.Bool);
            var machine = controller.layers[0].stateMachine;
            var idleState = machine.AddState("Idle"); idleState.motion = idle; machine.defaultState = idleState;
            var walkState = machine.AddState("Walk"); walkState.motion = walk;
            var forward = idleState.AddTransition(walkState); forward.hasExitTime = false; forward.duration = .12f; forward.AddCondition(AnimatorConditionMode.If, 0, "Walking");
            var back = walkState.AddTransition(idleState); back.hasExitTime = false; back.duration = .12f; back.AddCondition(AnimatorConditionMode.IfNot, 0, "Walking");
            rig.ApplyPose(0, false); rig.gameObject.AddComponent<Animator>().runtimeAnimatorController = controller;
            PrefabUtility.SaveAsPrefabAsset(rig.gameObject, folder + "/" + name + "Rig.prefab");
            rig.transform.position = new Vector3(x, 0, 0); return rig;
        }
        static Rect FindBounds(Texture2D texture, Color32[] pixels, int cell, int columns = 4, int rows = 3)
        {
            int col = cell % columns, row = cell / columns;
            int x0 = Mathf.RoundToInt(col * texture.width / (float)columns), x1 = Mathf.RoundToInt((col + 1) * texture.width / (float)columns);
            int y0 = texture.height - Mathf.RoundToInt((row + 1) * texture.height / (float)rows), y1 = texture.height - Mathf.RoundToInt(row * texture.height / (float)rows);
            int left = x1, right = x0 - 1, bottom = y1, top = y0 - 1;
            for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++) if (pixels[y * texture.width + x].a > 64)
            { left = Mathf.Min(left, x); right = Mathf.Max(right, x); bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); }
            if (right < left || top < bottom) throw new InvalidOperationException("Empty atlas cell: " + Names[cell]);
            left = Mathf.Max(x0, left - 2); right = Mathf.Min(x1 - 1, right + 2); bottom = Mathf.Max(y0, bottom - 2); top = Mathf.Min(y1 - 1, top + 2);
            return new Rect(left, bottom, right - left + 1, top - bottom + 1);
        }
        static void MakeFootSlices(Texture2D texture, Color32[] pixels, CharacterRigDefinition definition, bool near)
        {
            Sprite full = definition.parts[near ? 8 : 10]; Rect r = full.rect;
            float cut = Mathf.Round(r.height * .22f), footTop = Mathf.Round(r.height * .28f), ankle = Mathf.Round(r.height * .24f);
            var calfRect = new Rect(r.x, r.y + cut, r.width, r.height - cut);
            var footRect = new Rect(r.x, r.y, r.width, footTop);
            var calf = Sprite.Create(texture, calfRect, new Vector2(full.pivot.x / r.width, (full.pivot.y - cut) / calfRect.height), 100, 0, SpriteMeshType.FullRect);
            var foot = Sprite.Create(texture, footRect, new Vector2(full.pivot.x / r.width, ankle / footTop), 100, 0, SpriteMeshType.FullRect);
            calf.name = near ? "NearCalf" : "FarCalf"; foot.name = near ? "NearFoot" : "FarFoot";
            AssetDatabase.AddObjectToAsset(calf, definition); AssetDatabase.AddObjectToAsset(foot, definition);
            if (near) { definition.nearCalf = calf; definition.nearFoot = foot; definition.nearFootSole = SolePoints(texture, pixels, foot); }
            else { definition.farCalf = calf; definition.farFoot = foot; definition.farFootSole = SolePoints(texture, pixels, foot); }
        }
        static Vector2[] SolePoints(Texture2D texture, Color32[] pixels, Sprite sprite)
        {
            var points = new List<Vector2>(); Rect rect = sprite.rect;
            for (int x = 0; x < (int)rect.width; x++)
                for (int y = 0; y < (int)rect.height; y++)
                    if (pixels[((int)rect.y + y) * texture.width + (int)rect.x + x].a > 64)
                    {
                        points.Add(new Vector2((x + .5f - sprite.pivot.x) / rect.height, (y + .5f - sprite.pivot.y) / rect.height));
                        break;
                    }
            return points.ToArray();
        }
        static Vector2 Pivot(int cell, bool ranger)
        {
            switch (cell)
            {
                case 0: return ranger ? new Vector2(.72f, .27f) : new Vector2(.52f, .08f);
                case 1: return new Vector2(.60f, .06f);
                case 2: return new Vector2(.50f, .80f);
                case 3: return new Vector2(.65f, .93f);
                case 5: return new Vector2(.58f, .93f);
                case 4: case 6: return new Vector2(.42f, .94f);
                case 8: case 10: return new Vector2(.46f, .95f);
                case 11: return ranger ? new Vector2(.69f, .46f) : new Vector2(.50f, .36f);
                default: return new Vector2(.50f, .95f);
            }
        }
        static AnimationClip Bake(CutoutCharacterRig rig, bool walk, string path)
        {
            float length = walk ? .8f : 2.4f; int steps = Mathf.RoundToInt(length * 30);
            var joints = rig.AnimatedJoints(); var rotations = new AnimationCurve[joints.Length];
            for (int i = 0; i < joints.Length; i++) rotations[i] = new AnimationCurve();
            var bodyY = new AnimationCurve(); var bodyX = new AnimationCurve();
            for (int frame = 0; frame <= steps; frame++)
            {
                float t = length * frame / steps; rig.ApplyPose(t, walk);
                for (int i = 0; i < joints.Length; i++) rotations[i].AddKey(t, Mathf.DeltaAngle(0, joints[i].localEulerAngles.z));
                bodyY.AddKey(t, rig.body.localPosition.y); bodyX.AddKey(t, rig.body.localPosition.x);
            }
            var clip = new AnimationClip { name = walk ? "Walk" : "Idle", frameRate = 30 };
            for (int i = 0; i < joints.Length; i++)
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(joints[i], rig.transform), typeof(Transform), "localEulerAnglesRaw.z"), rotations[i]);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(rig.body, rig.transform), typeof(Transform), "m_LocalPosition.y"), bodyY);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(rig.body, rig.transform), typeof(Transform), "m_LocalPosition.x"), bodyX);
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true; AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, path); return clip;
        }
    }
}
