#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat.Editor
{
    public static class TeacherBossPolishAuthoring
    {
        private const string Root = "Assets/_Audere/Data/Combat/Teacher/";
        private const string Moves = Root + "Moves/";
        private const string StreamPath = Moves + "Move_TeacherReadableStream.asset";

        [MenuItem("Audere/Combat/Polish Teacher Boss")]
        public static void Author()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play before authoring the Teacher boss.");

            var chalk = Required<CombatBulletView>("Assets/_Audere/Prefabs/Combat/Bullets/Bullet_ChalkRod.prefab");
            var stream = EnsureReadableStream();
            var stroke = EnsureChalkStroke();
            var geometry = EnsureMove<TeacherGeometrySketchMove>("Move_TeacherGeometrySketch");
            var spiralStroke = EnsureChalkStroke("Bullet_ChalkSpiralStroke",new Vector2(22f,8f));
            var spiral = EnsureMove<TeacherSpiralSketchMove>("Move_TeacherSpiralSketch");
            var math = EnsureMove<TeacherMathChoiceMove>("Move_TeacherArithmeticChoice");
            Tune(geometry, .3f, s =>
            {
                s.FindProperty("duration").floatValue = 8.8f;
                s.FindProperty("chalkPrefab").objectReferenceValue = stroke;
                s.FindProperty("chalkTip").objectReferenceValue = chalk.GetComponent<Image>().sprite;
                s.FindProperty("chalkMaterial").objectReferenceValue = Required<Material>("Assets/_Audere/Materials/UI/ChalkDrawing.mat");
                s.FindProperty("shapeCount").intValue = 20;
                s.FindProperty("outsideDistance").floatValue = 90f;
                s.FindProperty("beatDuration").floatValue = .3f;
                s.FindProperty("strokeInterval").floatValue = .13f;
                s.FindProperty("warningDuration").floatValue = .26f;
                s.FindProperty("flightDuration").floatValue = 1.7f;
                s.FindProperty("safeRadius").floatValue = 66f;
            });
            Tune(math, .35f, s => {
                s.FindProperty("duration").floatValue = 30f;
                s.FindProperty("incorrectAnswerFollowUp").objectReferenceValue = geometry;
                s.FindProperty("compactWidth").floatValue = 360f;
                s.FindProperty("compactHeight").floatValue = 190f;
                s.FindProperty("orbitWidth").floatValue = 140f;
                s.FindProperty("orbitHeight").floatValue = 60f;
                s.FindProperty("monochromeEchoMaterial").objectReferenceValue = Required<Material>("Assets/_Audere/Materials/Combat/MountRainbowEcho.mat");
            });
            Tune(spiral,.35f,s => {
                s.FindProperty("duration").floatValue=4.05f;
                s.FindProperty("strokePrefab").objectReferenceValue=spiralStroke;
                s.FindProperty("chalkTip").objectReferenceValue=chalk.GetComponent<Image>().sprite;
                s.FindProperty("chalkMaterial").objectReferenceValue=Required<Material>("Assets/_Audere/Materials/UI/ChalkDrawing.mat");
                s.FindProperty("drawDuration").floatValue=.8f;
                s.FindProperty("warningDuration").floatValue=.4f;
                s.FindProperty("sweepRotations").intValue=3;
                s.FindProperty("secondsPerRotation").floatValue=2.5f/3f;
                s.FindProperty("segmentCount").intValue=128;
                s.FindProperty("turns").floatValue=2f;
                s.FindProperty("outerRadius").floatValue=1.45f;
                s.FindProperty("strokeWidth").floatValue=8f;
            });

            var fence = Required<ChalkFenceMove>(Moves + "Move_ChalkFence.asset");
            var sweep = Required<ChalkSweepMove>(Moves + "Move_ChalkSweep.asset");
            var clockwiseShort = Required<ChalkSweepMove>(Moves + "Move_TeacherClockwiseShort.asset");
            var radial = Required<RadialInwardTrailMove>(Moves + "Move_TeacherRadialInwardTrails.asset");
            var laser = Required<NarrativePressurePatternMove>(Moves + "Move_TeacherLaserColumns.asset");
            var shift = Required<ShiftingBattleBoxMove>(Moves + "Move_TeacherFieldShift.asset");
            var shiftSweep = Required<CompositeCombatMove>(Moves + "Move_TeacherShiftAndSweep.asset");
            var final = Required<CompositeCombatMove>(Moves + "Move_TeacherFinalPressure.asset");

            Tune(fence, .3f, s => { s.FindProperty("duration").floatValue=5.6f; s.FindProperty("waveInterval").floatValue=1.8f; s.FindProperty("flightDuration").floatValue=1.2f; s.FindProperty("telegraph").floatValue = .5f; Trail(s, 2.8f); });
            Tune(sweep, .3f, s => { s.FindProperty("clockwiseLunges").boolValue=true; s.FindProperty("interval").floatValue=.28f; s.FindProperty("flightDuration").floatValue=1.3f; s.FindProperty("telegraph").floatValue = .5f; Trail(s, 2.8f); });
            Tune(radial, .45f, s =>
            {
                s.FindProperty("projectileCount").intValue = 8;
                s.FindProperty("telegraphDuration").floatValue = .9f;
                s.FindProperty("flightDuration").floatValue = 3.2f;
                Trail(s, .75f);
                s.FindProperty("stunTrail").FindPropertyRelative("width").floatValue = 10f;
                s.FindProperty("stunTrail").FindPropertyRelative("fadeDuration").floatValue = .18f;
            });
            Tune(clockwiseShort, .55f, s =>
            {
                s.FindProperty("duration").floatValue = 6.5f;
                s.FindProperty("projectilePrefab").objectReferenceValue = chalk;
                s.FindProperty("clockwiseLunges").boolValue = true;
                s.FindProperty("shorteningClockwise").boolValue = true;
                s.FindProperty("finalTravelFraction").floatValue = .52f;
                s.FindProperty("telegraph").floatValue = .45f;
                s.FindProperty("flightDuration").floatValue = 1.2f;
                s.FindProperty("interval").floatValue = .55f;
                s.FindProperty("turns").floatValue = 0f;
                Trail(s, .7f);
                s.FindProperty("stunTrail").FindPropertyRelative("width").floatValue = 11f;
                s.FindProperty("stunTrail").FindPropertyRelative("fadeDuration").floatValue = .18f;
            });
            Tune(laser, .95f, s => s.FindProperty("telegraphDuration").floatValue = 1.1f);
            Tune(shift, 0f, s =>
            {
                s.FindProperty("telegraphDuration").floatValue = .85f;
                s.FindProperty("squeezeDuration").floatValue = .8f;
                s.FindProperty("holdDuration").floatValue = .9f;
                s.FindProperty("returnDuration").floatValue = .7f;
            });
            Tune(shiftSweep, 1f, s =>
            {
                s.FindProperty("duration").floatValue = 7.4f;
                SetChildDelays(s, 0f, .7f);
            });
            Tune(final, 1.1f, s =>
            {
                s.FindProperty("duration").floatValue = 8.4f;
                var children = s.FindProperty("children");
                children.GetArrayElementAtIndex(0).objectReferenceValue = stream;
                SetChildDelays(s, 0f, 1.4f);
            });

            // A clockwise lance sequence leads, followed by the separated fence lanes.
            SetMoves("MoveSet_ChalkCorridor", sweep, fence);
            SetMoves("MoveSet_ForcedRhythm", geometry, math, spiral, math);
            SetMoves("MoveSet_OverlappingPressure", radial, shiftSweep, final, clockwiseShort);

            var enemy = Required<CombatEnemyDefinition>(Root + "Enemy_Teacher_PLACEHOLDER.asset");
            var es = new SerializedObject(enemy);
            es.FindProperty("sharedMaxHealth").intValue = 21;
            var phases = es.FindProperty("phases");
            if (phases.arraySize != 3) throw new InvalidOperationException("Teacher must retain three phases.");
            for (int i = 0; i < 3; i++)
            {
                var phase = phases.GetArrayElementAtIndex(i);
                phase.FindPropertyRelative("maxHealth").intValue = 21;
                phase.FindPropertyRelative("sharedExitThreshold").intValue = new[] { 14, 7, 0 }[i];
            }
            phases.GetArrayElementAtIndex(2).FindPropertyRelative("presentation").objectReferenceValue =
                Required<CombatPhasePresentationProfile>(Root + "Presentation_Teacher_Final.asset");
            es.ApplyModifiedPropertiesWithoutUndo();
            if (!enemy.Validate(out string error)) throw new InvalidOperationException(error);
            AssetDatabase.SaveAssetIfDirty(enemy);

            var encounter = Required<CombatEncounterData>(Root + "CombatEncounter_D3_TEACHER_PRESSURE.asset");
            var enc = new SerializedObject(encounter);
            enc.FindProperty("encounterDuration").floatValue = 120f;
            enc.FindProperty("batchRespawnDelay").floatValue = .45f;
            enc.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(encounter);
            BindQuestionFont();
            AssetDatabase.SaveAssets();
            Debug.Log("[TeacherBossPolish] 21 HP / 120 TIME; chalk, geometry/math and staggered final phase authored.");
        }

        private static CombatBulletView EnsureChalkStroke(string name="Bullet_ChalkStroke",Vector2? size=null)
        {
            string target="Assets/_Audere/Prefabs/Combat/Bullets/"+name+".prefab";
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(target)!=null?target:"Assets/_Audere/Prefabs/Combat/Bullets/Bullet_ChalkRod.prefab";
            var root=PrefabUtility.LoadPrefabContents(source);
            try
            {
                root.name=name;
                root.GetComponent<RectTransform>().sizeDelta=size??new Vector2(96f,7f);
                var image=root.GetComponent<Image>();
                image.sprite=null; image.color=Color.white;
                image.material=Required<Material>("Assets/_Audere/Materials/UI/ChalkDrawing.mat");
                PrefabUtility.SaveAsPrefabAsset(root,target);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return Required<CombatBulletView>(target);
        }

        private static T EnsureMove<T>(string name) where T : CombatMoveDefinition
        {
            string path = Moves + name + ".asset";
            var move = AssetDatabase.LoadAssetAtPath<T>(path);
            if (move == null)
            {
                move = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(move, path);
            }
            return move;
        }

        private static SineProjectileStreamMove EnsureReadableStream()
        {
            var stream = AssetDatabase.LoadAssetAtPath<SineProjectileStreamMove>(StreamPath);
            if (stream == null)
            {
                var original = Required<SineProjectileStreamMove>(Moves + "Move_ChalkSineStream.asset");
                stream = ScriptableObject.CreateInstance<SineProjectileStreamMove>();
                EditorUtility.CopySerialized(original, stream);
                stream.name = "Move_TeacherReadableStream";
                AssetDatabase.CreateAsset(stream, StreamPath);
            }
            Tune(stream, 0f, s =>
            {
                s.FindProperty("interval").floatValue = .25f;
                s.FindProperty("telegraph").floatValue = .65f;
            });
            return stream;
        }

        private static void SetChildDelays(SerializedObject move, params float[] delays)
        {
            var array = move.FindProperty("childStartDelays");
            array.arraySize = delays.Length;
            for (int i = 0; i < delays.Length; i++)
                array.GetArrayElementAtIndex(i).floatValue = delays[i];
        }

        private static void BindQuestionFont()
        {
            const string path = "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var board = root.GetComponent<CombatBoardView>();
                var so = new SerializedObject(board);
                so.FindProperty("stunTrailMaterial").objectReferenceValue=Required<Material>("Assets/_Audere/Materials/UI/ChalkDrawing.mat");
                so.FindProperty("teacherQuestionFont").objectReferenceValue =
                    Required<TMP_FontAsset>("Assets/_Audere/AssetGame/Font/Mynerve-Regular SDF.asset");
                so.ApplyModifiedPropertiesWithoutUndo();
                board.RefreshProjectilePresentationLayers();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void Tune(CombatMoveDefinition move, float lead, Action<SerializedObject> edit)
        {
            var s = new SerializedObject(move);
            s.FindProperty("leadInDuration").floatValue = lead;
            edit?.Invoke(s);
            s.ApplyModifiedPropertiesWithoutUndo();
            if (!move.Validate(out string error)) throw new InvalidOperationException(error);
            AssetDatabase.SaveAssetIfDirty(move);
        }

        private static void Trail(SerializedObject move, float seconds)
        {
            var trail = move.FindProperty("stunTrail");
            trail.FindPropertyRelative("enabled").boolValue = true;
            trail.FindPropertyRelative("blockingDuration").floatValue = seconds;
        }

        private static void SetMoves(string name, params CombatMoveDefinition[] moves)
        {
            var set = Required<CombatMoveSet>(Moves + name + ".asset");
            var s = new SerializedObject(set);
            s.FindProperty("selectionPolicy").intValue = (int)CombatMoveSelectionPolicy.OrderedLoop;
            var entries = s.FindProperty("entries");
            entries.arraySize = moves.Length;
            for (int i = 0; i < moves.Length; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("move").objectReferenceValue = moves[i];
                entry.FindPropertyRelative("weight").floatValue = 1f;
            }
            s.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(set);
        }

        private static T Required<T>(string path) where T : UnityEngine.Object
        {
            var value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value == null) throw new MissingReferenceException(path);
            return value;
        }
    }
}
#endif
