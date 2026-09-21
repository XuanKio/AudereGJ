#if UNITY_EDITOR
using System;
using System.Linq;
using Audere.Dialogue;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Audere.Combat.Editor
{
    // Deliberately scoped to the final encounter. Does not rebuild any Story hierarchy.
    public static class TimorFinalPolishAuthoring
    {
        public const string Root = "Assets/_Audere/Data/Combat/TimorReturn/";
        public const string Moves = Root + "FinalMoves/";
        private const string DialogueRoot = "Assets/_Audere/Data/Dialogue/Day4/TimorFinal/";
        [MenuItem("Audere/Combat/Polish Timor Finale")]
        public static void Author()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Timor authoring requires Edit Mode after compilation.");
            var basic = Required<CombatBulletView>("Assets/_Audere/Prefabs/Combat/Bullets/EnemyBullet.prefab");
            var bullet = Projectile("Bullet_TimorReadable", basic, new Vector2(15f,15f), null);
            var footprint = Projectile("Bullet_TimorFootprint", basic, new Vector2(34f,34f), null);
            string[] messages = { "Lại làm hỏng — lại làm hỏng", "Quay lại đi — quay lại đi", "Họ đang chờ — họ đang chờ", "Đừng đi nữa — đừng đi nữa" };
            var words = messages.Select((text,i)=>Projectile("Bullet_TimorWord_"+i,basic,new Vector2(300f,32f),text)).ToArray();
            var echo = Move<FootstepEchoMove>("Move_TimorFootsteps",6.6f,s=> {
                Ref(s,"projectile",footprint); Float(s,"sampleInterval",1.3f); Float(s,"warningDuration",1f); });
            var breaker = Move<TargetedDiceBreakMove>("Move_TimorDiceBreak",4.5f,s=> {
                Ref(s,"tailSprite",FindSprite("Enemyy/đuôi.png")); });
            var release = Move<TargetedDiceBreakMove>("Move_TimorReleaseChoice",4.5f,s=> {
                Ref(s,"tailSprite",FindSprite("Enemyy/đuôi.png")); s.FindProperty("releaseChoice").boolValue=true; });
            var clones = Move<OrbitingCloneVolleyMove>("Move_TimorClockwiseClones",11.6f,s=> {
                Ref(s,"cloneSprite",FindSprite("Enemyy/timor.png")); Ref(s,"projectile",basic);
                Float(s,"telegraph",.65f); Float(s,"betweenShots",.4f); Float(s,"speed",265f);
                Float(s,"fieldWidth",.5f);Float(s,"orbitAngularSpeed",.8f);
                s.FindProperty("projectilesPerFan").intValue=5;Float(s,"fanSpacing",13f);Float(s,"followupDelay",.14f); });
            MigrateMove("Move_TimorEchoBoxes", "Move_TimorPressureWaves");
            MigrateMove("Move_TimorSupportedBoxes", "Move_TimorSupportedWaves");
            var opening = Waves("Move_TimorOpeningRhythm",8.4f,bullet,false,150f,195f,.78f,false,.56f);
            var cross = Waves("Move_TimorCrossRhythm",8.4f,bullet,false,144f,215f,.7f,true,.5f);
            var waves = Waves("Move_TimorPressureWaves",9.5f,bullet,false,124f,230f,.64f,false,.48f);
            var peakClones = Move<OrbitingCloneVolleyMove>("Move_TimorPeakClones",11.6f,s=> {
                Ref(s,"cloneSprite",FindSprite("Enemyy/timor.png")); Ref(s,"projectile",basic);
                Float(s,"telegraph",.65f); Float(s,"betweenShots",.4f); Float(s,"speed",285f);
                Float(s,"fieldWidth",.5f);Float(s,"orbitAngularSpeed",1.05f);
                s.FindProperty("projectilesPerFan").intValue=5;Float(s,"fanSpacing",13f);Float(s,"followupDelay",.14f); });
            var supportWaves = Waves("Move_TimorSupportedWaves",10.5f,bullet,true,144f,185f,.8f,false,.6f);
            var corridor = Corridor("Move_TimorWordCorridor",16f,words,false);
            var supportedCorridor = Corridor("Move_TimorSupportedCorridor",9.5f,words,true);

            var teacherMemory = Projection("Teacher", "Cô giáo", -1,
                "Assets/_Audere/Data/Combat/Teacher/Enemy_Teacher_PLACEHOLDER.asset", "Enemyy/co giao enemy.png",
                new Vector2(420, 560),
                "Assets/_Audere/Data/Combat/Teacher/Moves/Move_TeacherGeometrySketch.asset",
                "Assets/_Audere/Data/Combat/Teacher/Moves/Move_TeacherSpiralSketch.asset");
            var biancaMemory = Projection("Bianca", "Bianca", 1,
                "Assets/_Audere/Data/Combat/BiancaSupplies/Enemy_BiancaSupplies_PLACEHOLDER.asset", "Enemyy/biancaenemy.png",
                new Vector2(420, 560),
                "Assets/_Audere/Data/Combat/BiancaSupplies/Moves/Move_Bianca_RibbonFanSweep.asset");
            var crowdMemory = Projection("Crowd", "Mọi người", 0,
                "Assets/_Audere/Data/Combat/Crowd/Enemy_Crowd.asset", "Enemyy/dam dong enemy.png",
                new Vector2(530, 390),
                "Assets/_Audere/Data/Combat/Crowd/Move_MountDive_Counter.asset",
                "Assets/_Audere/Data/Combat/Crowd/Move_DiagonalHands.asset");
            SetMoves(1,opening,Required<CombatMoveDefinition>(Moves+"Move_TimorTailThrow.asset"),cross);
            SetMoves(2,corridor,clones,waves);
            SetMoves(3,teacherMemory,biancaMemory,crowdMemory,supportWaves);

            var teacher=Memory("SUPPORT_TEACHER",DialogueCharacterId.Teacher,
                "L|Cô đã nói với mình…", "R|Em chưa cần trả lời ngay.","R|Cô không trách em.");
            var bianca=Memory("SUPPORT_BIANCA",DialogueCharacterId.Bianca,
                "L|Lúc mình ngã…", "R|Vịn vào tớ nhé?", "L|Ừ. Mình đã nhận lời.");
            var crowd=Memory("SUPPORT_TOGETHER",DialogueCharacterId.Bianca,
                "L|Mình đã nhờ mọi người giúp.", "L|Và mình không phải làm hết một mình.");
            var enemy=Required<CombatEnemyDefinition>(Root+"Enemy_TimorReturn.asset");
            var so=new SerializedObject(enemy);var phases=so.FindProperty("phases");
            so.FindProperty("suppressHitFlash").boolValue=true;
            // Preserve checkpoints and the three existing opening exchanges.
            for(int i=0;i<3;i++)
                Ref(phases.GetArrayElementAtIndex(i),"moveSet",Required<CombatMoveSet>(Moves+"MoveSet_TimorFinal_P"+(i+1)+".asset"));
            // Nine counters clear all three memories even on Hard (ceil(6 * 1.36) = 9).
            phases.GetArrayElementAtIndex(2).FindPropertyRelative("maxHealth").intValue=6;
            var p2=phases.GetArrayElementAtIndex(1).FindPropertyRelative("dialogueCues");
            p2.arraySize=2;
            Cue(p2.GetArrayElementAtIndex(1),"timor-echoes-finished",CombatDialogueCueTrigger.MoveCompleted,waves,
                Required<DialogueData>(DialogueRoot+"Dialogue_D4_TIMOR_FINAL_AVOID.asset"),false,true);
            var p3=phases.GetArrayElementAtIndex(2).FindPropertyRelative("dialogueCues");
            p3.arraySize=5;
            Cue(p3.GetArrayElementAtIndex(1),"memory-teacher",CombatDialogueCueTrigger.MoveCompleted,teacherMemory,teacher,true,false);
            Cue(p3.GetArrayElementAtIndex(2),"memory-bianca",CombatDialogueCueTrigger.MoveCompleted,biancaMemory,bianca,true,false);
            Cue(p3.GetArrayElementAtIndex(3),"memory-crowd",CombatDialogueCueTrigger.MoveCompleted,crowdMemory,crowd,true,false);
            Cue(p3.GetArrayElementAtIndex(4),"timor-final-choice",CombatDialogueCueTrigger.MoveCompleted,supportWaves,
                Required<DialogueData>(DialogueRoot+"Dialogue_D4_TIMOR_FINAL_FINAL_GATE.asset"),true,false);
            so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(enemy);
            var encounter=Required<CombatEncounterData>(Root+"CombatEncounter_D4_TIMOR_RETURN.asset");
            var encounterSo=new SerializedObject(encounter);
            // The gated final sequence is ~60 active seconds. Leave room for missed dice and hits.
            Float(encounterSo,"encounterDuration",120f);
            Float(encounterSo,"playerHitInvulnerability",.85f);
            Float(encounterSo,"bulletTimePenaltySeconds",3f);
            Float(encounterSo,"diceBreakChance",.3f);
            Ref(encounterSo,"diceBreakTailSprite",FindSprite("Enemyy/đuôi.png"));
            var recovery=encounterSo.FindProperty("phaseRecovery");
            recovery.FindPropertyRelative("enabled").boolValue=true;
            recovery.FindPropertyRelative("timePerHealDie").floatValue=6f;
            recovery.FindPropertyRelative("dropInterval").floatValue=.48f;
            recovery.FindPropertyRelative("healAnimationDuration").floatValue=.24f;
            recovery.FindPropertyRelative("healEnemyOnDrop").boolValue=true;
            recovery.FindPropertyRelative("enemyHealthPerDie").intValue=1;
            recovery.FindPropertyRelative("droppedDiceLifetime").floatValue=3f;
            encounterSo.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(encounter);
            if(!enemy.Validate(out string error))throw new InvalidOperationException(error);
            AssetDatabase.SaveAssets();
            Debug.Log("Timor finale polished: exterior Timor clones, words, one Battle Box, support and completion gates.");
        }
        private static DefeatableProjectionMove Projection(string id,string label,int side,string enemyPath,
            string spritePath,Vector2 size,params string[] attacks)
        {
            var enemy=Required<CombatEnemyDefinition>(enemyPath);
            var signatures=attacks.Select(Required<CombatMoveDefinition>).ToArray();
            return Move<DefeatableProjectionMove>("Move_TimorMemory"+id,signatures.Sum(x=>x.Duration)+3f,s=>{
                Ref(s,"actorPrefab",enemy.ActorPrefab);Ref(s,"sprite",FindSprite(spritePath));
                s.FindProperty("displayName").stringValue=label;s.FindProperty("side").intValue=side;
                s.FindProperty("visualSize").vector2Value=size;s.FindProperty("attacksToDefeat").intValue=3;
                var array=s.FindProperty("signatureMoves");array.arraySize=signatures.Length;
                for(int i=0;i<signatures.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=signatures[i];
            });
        }
        private static void MigrateMove(string oldName, string newName)
        {
            string oldPath=Moves+oldName+".asset", newPath=Moves+newName+".asset";
            if(AssetDatabase.LoadAssetAtPath<CombatMoveDefinition>(oldPath)!=null &&
                AssetDatabase.LoadAssetAtPath<CombatMoveDefinition>(newPath)==null)
            {
                string error=AssetDatabase.MoveAsset(oldPath,newPath);
                if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);
            }
        }
        private static TimorPressureWaveMove Waves(string name,float duration,CombatBulletView bullet,bool support,float gap,float speed,
            float width,bool horizontal,float interval)
            =>Move<TimorPressureWaveMove>(name,duration,s=>{
                Ref(s,"projectile",bullet);Float(s,"gapWidth",gap);Float(s,"speed",speed);
                Float(s,"waveInterval",interval);Float(s,"warningDuration",.38f);Float(s,"phraseRest",.55f);
                Float(s,"minimumWidth",width);Float(s,"sway",support?.45f:.78f);
                s.FindProperty("wavesPerPhrase").intValue=4;
                s.FindProperty("horizontalVolley").boolValue=horizontal;
                s.FindProperty("supported").boolValue=support;
                var lines=s.FindProperty("encouragement");lines.arraySize=3;
                lines.GetArrayElementAtIndex(0).stringValue="Cô giáo: Cô không trách em.";
                lines.GetArrayElementAtIndex(1).stringValue="Bianca: Vịn vào tớ nhé?";
                lines.GetArrayElementAtIndex(2).stringValue="Các bạn: Tụi mình cùng làm nhé.";
            });
        private static ScrollingWordCorridorMove Corridor(string name,float duration,CombatBulletView[] words,bool support)
            =>Move<ScrollingWordCorridorMove>(name,duration,s=>{
                var array=s.FindProperty("wordProjectiles");array.arraySize=words.Length;
                for(int i=0;i<words.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=words[i];
                s.FindProperty("supported").boolValue=support;Float(s,"waveInterval",support?2.1f:1.9f);
                Float(s,"warningDuration",support?.7f:.6f);
                Ref(s,"timorSprite",FindSprite("Enemyy/timor.png"));
                Ref(s,"handProjectile",Required<CombatBulletView>("Assets/_Audere/Prefabs/Combat/Bullets/Bullet_CrowdHand.prefab"));
                Float(s,"travelSpeed",support?640f:960f);Float(s,"corridorHeight",support?.94f:.82f);
                s.FindProperty("wordsPerTrain").intValue=support?2:3;Float(s,"trainBeat",support?.4f:.34f);Float(s,"rowStagger",.03f);
            });
        private static T Move<T>(string name,float duration,Action<SerializedObject> configure) where T:CombatMoveDefinition
        {
            string path=Moves+name+".asset";
            var asset=AssetDatabase.LoadAssetAtPath<T>(path);
            bool create=asset==null;if(create)asset=ScriptableObject.CreateInstance<T>();
            asset.name=name;var so=new SerializedObject(asset);Float(so,"duration",duration);Float(so,"leadInDuration",.35f);
            if(so.FindProperty("silhouetteMaterial")!=null)
                Ref(so,"silhouetteMaterial",Required<Material>("Assets/_Audere/Materials/Combat/MountRainbowEcho.mat"));
            configure(so);so.ApplyModifiedPropertiesWithoutUndo();
            if(!asset.Validate(out string error))throw new InvalidOperationException(name+": "+error);
            if(create)AssetDatabase.CreateAsset(asset,path);else EditorUtility.SetDirty(asset);
            return asset;
        }
        private static void SetMoves(int phase,params CombatMoveDefinition[] moves)
        {
            var asset=Required<CombatMoveSet>(Moves+"MoveSet_TimorFinal_P"+phase+".asset");
            var so=new SerializedObject(asset);so.FindProperty("selectionPolicy").enumValueIndex=0;
            var entries=so.FindProperty("entries");entries.arraySize=moves.Length;
            for(int i=0;i<moves.Length;i++) {var entry=entries.GetArrayElementAtIndex(i);Ref(entry,"move",moves[i]);entry.FindPropertyRelative("weight").floatValue=1;}
            so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(asset);
        }
        private static void Cue(SerializedProperty cue,string id,CombatDialogueCueTrigger trigger,CombatMoveDefinition move,
            DialogueData dialogue,bool victory,bool phase)
        {
            cue.FindPropertyRelative("cueId").stringValue=id;cue.FindPropertyRelative("oneShotKey").stringValue=id;
            cue.FindPropertyRelative("trigger").enumValueIndex=(int)trigger;Ref(cue,"triggerMove",move);
            cue.FindPropertyRelative("triggerValue").floatValue=0;
            cue.FindPropertyRelative("triggerCueId").stringValue="";
            cue.FindPropertyRelative("filterBySymbol").boolValue=false;
            cue.FindPropertyRelative("instruction").stringValue="";
            cue.FindPropertyRelative("isTutorial").boolValue=false;
            cue.FindPropertyRelative("interruptsAutoDialogue").boolValue=false;
            cue.FindPropertyRelative("repeatOnTrigger").boolValue=false;
            var sequence=cue.FindPropertyRelative("sequence");sequence.arraySize=1;sequence.GetArrayElementAtIndex(0).objectReferenceValue=dialogue;
            cue.FindPropertyRelative("presentation").enumValueIndex=1;
            cue.FindPropertyRelative("minimumLineDuration").floatValue=1.4f;
            cue.FindPropertyRelative("charactersPerSecond").floatValue=21;
            cue.FindPropertyRelative("interLineGap").floatValue=.2f;
            cue.FindPropertyRelative("requiredBeforeVictory").boolValue=victory;
            cue.FindPropertyRelative("requiredBeforePhaseAdvance").boolValue=phase;
            cue.FindPropertyRelative("requiredBeforePlayerDefeat").boolValue=false;
        }
        private static DialogueData Memory(string id,DialogueCharacterId character,params string[] lines)
        {
            string path=DialogueRoot+"Dialogue_D4_TIMOR_FINAL_"+id+".asset";
            var data=AssetDatabase.LoadAssetAtPath<DialogueData>(path);bool create=data==null;
            if(create)data=ScriptableObject.CreateInstance<DialogueData>();
            var so=new SerializedObject(data);so.FindProperty("dialogueId").stringValue="D4_TIMOR_FINAL_"+id;
            so.FindProperty("leftCharacter").enumValueIndex=1;so.FindProperty("rightCharacter").enumValueIndex=(int)character;
            var array=so.FindProperty("lines");array.arraySize=lines.Length;
            for(int i=0;i<lines.Length;i++)
            {
                var line=array.GetArrayElementAtIndex(i);line.FindPropertyRelative("speaker").enumValueIndex=lines[i][0]=='L'?0:1;
                line.FindPropertyRelative("text").stringValue=lines[i].Substring(2);
                line.FindPropertyRelative("characterOverride").enumValueIndex=0;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            if(create)AssetDatabase.CreateAsset(data,path);else EditorUtility.SetDirty(data);return data;
        }
        private static CombatBulletView Projectile(string name,CombatBulletView original,Vector2 size,string text)
        {
            string path="Assets/_Audere/Prefabs/Combat/Bullets/"+name+".prefab";
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var go=Object.Instantiate(existing!=null?existing:original.gameObject);go.name=name;
            go.GetComponent<RectTransform>().sizeDelta=size;
            foreach(var image in go.GetComponentsInChildren<Image>())
            {
                image.color=text==null?new Color(.95f,.5f,.7f,1f):new Color(.28f,.08f,.22f,.95f);
                image.raycastTarget=false;
                if(text!=null){image.sprite=null;image.enabled=false;image.color=Color.clear;}
            }
            if(text!=null)
            {
                var label=go.GetComponentInChildren<TextMeshProUGUI>();
                if(label==null)
                {
                    var child=new GameObject("Words",typeof(RectTransform),typeof(TextMeshProUGUI));child.transform.SetParent(go.transform,false);label=child.GetComponent<TextMeshProUGUI>();
                }
                label.font=FindFont();label.fontSharedMaterial=label.font.material;label.fontSize=23f;label.alignment=TextAlignmentOptions.Center;
                label.enableAutoSizing=false;
                label.text=text;label.color=new Color(1f,.83f,.87f,1f);label.raycastTarget=false;
                label.textWrappingMode=TextWrappingModes.NoWrap;
                size.x=Mathf.Max(1f,label.GetPreferredValues(text).x+8f);
                go.GetComponent<RectTransform>().sizeDelta=size;label.rectTransform.sizeDelta=size;
            }
            var result=PrefabUtility.SaveAsPrefabAsset(go,path).GetComponent<CombatBulletView>();Object.DestroyImmediate(go);return result;
        }
        private static TMP_FontAsset FindFont()=>Required<TMP_FontAsset>("Assets/_Audere/AssetGame/Font/Mynerve-Regular SDF.asset");
        private static Sprite FindSprite(string suffix)
        {
            string path=AssetDatabase.GetAllAssetPaths().FirstOrDefault(p=>p.EndsWith(suffix,StringComparison.OrdinalIgnoreCase));
            if(path==null)throw new InvalidOperationException("Missing sprite "+suffix);
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
        }
        private static T Required<T>(string path)where T:Object=>AssetDatabase.LoadAssetAtPath<T>(path)??throw new InvalidOperationException("Missing "+path);
        private static void Ref(SerializedObject so,string key,Object value)=>so.FindProperty(key).objectReferenceValue=value;
        private static void Ref(SerializedProperty so,string key,Object value)=>so.FindPropertyRelative(key).objectReferenceValue=value;
        private static void Float(SerializedObject so,string key,float value)=>so.FindProperty(key).floatValue=value;
    }
}
#endif

