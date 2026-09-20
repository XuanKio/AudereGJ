#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using Audere.Dialogue;
using Audere.Puzzle;
using Audere.Puzzle.Board;
using Audere.Puzzle.PathPieces;
using Audere.Story;
using Audere.Story.Steps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using static Audere.EditorTools.Day2SchoolMorningSetupTool;

namespace Audere.EditorTools
{
    // Design Intent: one role-exchange puzzle and short bus approaches; keep bus scenery fixed.
    public static class ShortPuzzleAuthoring
    {
        public const string Day1 = "Assets/_Audere/Scenes/20_D1_Home_Morning.unity";
        public const string Day2 = "Assets/_Audere/Scenes/50_D2_Home_Morning.unity";
        private static Vector2Int P(int x,int y) => new Vector2Int(x,y);
        public static T[] All<T>(Scene s) where T : Component => s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();

        [MenuItem("Audere/Story/Shorten Bus Puzzles And Merge Cooperative Puzzle")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
            foreach(string path in new[]{Day1,Day2})
            {
                var scene=EditorSceneManager.OpenScene(path);
                CompactBus(scene);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            var school=EditorSceneManager.OpenScene(Day2SchoolMorningSetupTool.ScenePath);
            SingleCoop(school);
            EditorSceneManager.MarkSceneDirty(school);EditorSceneManager.SaveScene(school);
            AssetDatabase.SaveAssets();
        }

        public static void SingleCoop(Scene scene)
        {
            var school=scene.GetRootGameObjects().Single(r=>r.name=="SCHOOL").transform;
            var levels=school.Find("COOP PUZZLES");
            var story=scene.GetRootGameObjects().Single(r=>r.name=="STORY").transform;
            var keep=levels.Find("PZ_D2_COOP_02") ?? levels.Find("PZ_D2_COOP_01");
            var controller=keep.GetComponentInChildren<PuzzleController>(true);
            var pair=controller.Puzzle.Cooperative;
            var e=story.Find("D2_SCHOOL_COOP_01").GetComponent<StoryEvent>();
            var tiles=keep.GetComponentsInChildren<BoardTile>(true);
            if(tiles.Length!=9 || keep.GetComponentsInChildren<CooperativeRedTileBehaviour>(true).Length!=2)
                throw new InvalidOperationException("Expected the authored two-hold puzzle before migration.");
            var data=AssetDatabase.LoadAssetAtPath<PuzzleData>("Assets/_Audere/Data/Puzzle/Day2/School/PZ_D2_COOP_01.asset");
            Set(data,"requireAllPathPieces",true,"availablePathPieces",Pieces("L_Corner","L_Corner_3","Line_2","L_Corner_3","Line_2"));
            Set(controller.Puzzle,"puzzleData",data);
            Set(pair,"openingActor",(int)CooperativePuzzleSession.OpeningActor.Bianca);
            var coordinator=levels.GetComponent<PuzzleRootCoordinator>() ?? levels.GetComponentInParent<PuzzleRootCoordinator>();
            if(coordinator==null) coordinator=All<PuzzleRootCoordinator>(scene).Single();
            Set(coordinator,"puzzles",new[]{controller});
            var prepare=e.transform.Find("030_PrepareBothStarts").GetComponent<PuzzleSequencePrepareStep>();
            Set(prepare,"startingPuzzle",controller,"followingPuzzles",new PuzzleController[0],"alignToPreviousGoal",false);
            var aTile=tiles.Single(t=>t.name=="Tile_1_0");var bTile=tiles.Single(t=>t.name=="Tile_0_1");
            Set(e.transform.Find("040_KeepBothStandingTiles").GetComponent<SetActiveStep>(),"objectsToEnable",new[]{aTile.gameObject,bTile.gameObject});
            var pose=school.Find("STAGING TARGETS/Camera_Coop_01");
            controller.Puzzle.Board.RegisterExistingTiles(); controller.Puzzle.Board.TryGetWorldBounds(out Bounds bounds);
            pose.position=new Vector3(bounds.center.x,bounds.center.y+.11f,pose.position.z);
            Set(e.transform.Find("050_FrameBothActors").GetComponent<MoveActorStep>(),"targetTransform",pose);
            Set(e.transform.Find("070_RevealCooperativeBoard").GetComponent<BoardTileTransitionStep>(),"revealPuzzle",controller,
                "objectsToReveal",tiles.Where(t=>t!=aTile&&t!=bTile).OrderBy(t=>Vector3.Distance(t.transform.position,aTile.transform.position)).Select(t=>t.transform).ToArray());
            Set(e.transform.Find("110_PlayTogether").GetComponent<PuzzleStep>(),"puzzleController",controller,"puzzleRoot",keep.gameObject);
            Set(e.transform.Find("120_KeepBothGoalAnchors").GetComponent<BoardTileTransitionStep>(),"sourcePuzzle",controller,
                "goalToBecomeAnchor",pair.AudereGoal.GetComponent<GoalTileBehaviour>(),
                "objectsToHide",tiles.Where(t=>t!=pair.AudereGoal&&t!=pair.PartnerGoal).Select(t=>t.transform).Reverse().ToArray());
            var obsolete=e.transform.Find("090_TimorChoosesWhoGoesFirst");
            if(obsolete!=null) Object.DestroyImmediate(obsolete.gameObject);
            Set(e.transform.Find("100_BiancaNeedsAudereToo").GetComponent<DialogueStep>(),"dialogueData",
                Day2SchoolMorningSetupTool.Dialogue("COOP_SINGLE_HOLD",DialogueCharacterId.Bianca,"R|Tớ giữ ô đỏ đầu. Cậu qua trước nhé.","L|Ừ... rồi tớ giữ ô bên kia."));
            var encourage=(DialogueStep)new SerializedObject(pair).FindProperty("encouragement").objectReferenceValue;
            Set(encourage,"dialogueData",Day2SchoolMorningSetupTool.Dialogue("COOP_SINGLE_EXCHANGE",DialogueCharacterId.Bianca,
                "L|Được rồi. Cậu qua đi.","R|Ừ. Chừa khúc rẽ cho tớ nhé."));
            Set(e,"autoPlayNextEvent",true,"nextEvent",story.Find("D2_SCHOOL_WRONG_SUPPLIES").GetComponent<StoryEvent>());
            // Remove only the now-unreachable puzzle sections. Shared actors/runtime and combat stay intact.
            foreach(Transform child in levels.Cast<Transform>().ToArray())
                if(child!=keep && child.GetComponentInChildren<PuzzleController>(true)!=null)Object.DestroyImmediate(child.gameObject);
            keep.name="PZ_D2_COOP_01";
            foreach(string id in new[]{"D2_SCHOOL_COOP_02","D2_SCHOOL_COOP_03"})
            {var old=story.Find(id);if(old!=null)Object.DestroyImmediate(old.gameObject);}
            foreach(string id in new[]{"Camera_Coop_02","Camera_Coop_03"})
            {var old=school.Find("STAGING TARGETS/"+id);if(old!=null)Object.DestroyImmediate(old.gameObject);}
            story.GetComponent<StoryDirector>().RefreshRegistry();
            if(!coordinator.ValidateConfiguration(false))throw new InvalidOperationException("Single puzzle bindings invalid.");
        }

        public static string BusScenerySnapshot(PuzzleController bus)
        {
            var goal=bus.Puzzle.Board.LevelObjectiveRoot.GetComponentInChildren<GoalTileBehaviour>(true);
            return string.Join("\n",goal.GetComponentsInChildren<Transform>(true).Select(t=>
                t.name+"|"+t.position.ToString("F7")+"|"+t.rotation.ToString("F7")+"|"+t.lossyScale.ToString("F7")));
        }

        public static void CompactBus(Scene scene)
        {
            bool day2=scene.path==Day2;
            if(!day2 && scene.path!=Day1)throw new InvalidOperationException("Expected a home morning scene.");
            string day=day2?"D2":"D1";
            var levels=All<PuzzleController>(scene);
            var bus=levels.Single(p=>p.PuzzleRoot.name=="PZ_"+day+"_BUS_STOP");
            var breakfast=levels.Single(p=>p.PuzzleRoot.name=="PZ_"+day+"_BREAKFAST");
            var wash=levels.Single(p=>p.PuzzleRoot.name=="PZ_"+day+"_WASHROOM");
            string before=BusScenerySnapshot(bus);
            var board=bus.Puzzle.Board;var grid=board.GridSpace;
            var goal=board.LevelObjectiveRoot.GetComponentInChildren<GoalTileBehaviour>(true).GetComponent<BoardTile>();
            Vector3 goalWorld=goal.transform.position;
            // The existing Day1 goal is a fraction off the shared grid. Align the grid math
            // to that fixed art, rather than moving the stop's sign/platform/items.
            Vector2Int goalCell=grid.WorldToCell(goalWorld);
            var gso=new SerializedObject(grid);
            gso.FindProperty("localOrigin").vector2Value+=(Vector2)grid.transform.InverseTransformVector(goalWorld-grid.CellToWorldCenter(goalCell));
            gso.ApplyModifiedPropertiesWithoutUndo();
            Vector2Int startCell=goalCell-P(3,2);
            Vector3 startWorld=grid.CellToWorldCenter(startCell);
            var cells=day2?new[]{P(0,0),P(1,0),P(1,1),P(1,2),P(2,0),P(2,1),P(3,0),P(3,1),P(3,2)}:
                new[]{P(0,0),P(1,0),P(1,1),P(1,2),P(2,0),P(2,2),P(3,0),P(3,1),P(3,2)};
            foreach(var tile in board.BoardVisualRoot.GetComponentsInChildren<BoardTile>(true))Object.DestroyImmediate(tile.gameObject);
            var grass=AssetDatabase.LoadAssetAtPath<GameObject>(PuzzleContentConstants.AssetPaths.GrassPrefab);
            var red=AssetDatabase.LoadAssetAtPath<GameObject>(PuzzleContentConstants.AssetPaths.OneUsePrefab);
            foreach(var cell in cells.Where(c=>c!=P(3,2)))
            {
                bool oneUse=day2&&cell==P(1,0);
                var go=(GameObject)PrefabUtility.InstantiatePrefab(oneUse?red:grass,board.BoardVisualRoot);
                go.name=(oneUse?"OneUse":"Grass")+" ("+cell.x+", "+cell.y+")";
                go.transform.position=grid.CellToWorldCenter(startCell+cell);
                go.transform.localRotation=Quaternion.identity;go.transform.localScale=Vector3.one;
            }
            bus.Puzzle.PlayerStartTransform.position=startWorld;
            Set(bus.Puzzle.PuzzleData,"availablePathPieces",day2?Pieces("Line_3","L_Corner","L_Corner_3"):Pieces("Line_3","Line_2","L_Corner_3"),"requireAllPathPieces",true);
            // Preserve the two hand-offs exactly by anchoring the approach from the fixed stop backwards.
            var breakfastGoal=breakfast.Puzzle.Board.LevelObjectiveRoot.GetComponentInChildren<GoalTileBehaviour>(true).transform;
            breakfast.PuzzleRoot.position+=startWorld-breakfastGoal.position;
            var washGoal=wash.Puzzle.Board.LevelObjectiveRoot.GetComponentInChildren<GoalTileBehaviour>(true).transform;
            wash.PuzzleRoot.position+=breakfast.Puzzle.PlayerStartTransform.position-washGoal.position;
            foreach(var c in new[]{wash,breakfast,bus})c.Puzzle.Board.RegisterExistingTiles();
            // Reveal lists must refer to the new authored cells, never the retired tiles.
            foreach(var transition in All<BoardTileTransitionStep>(scene))
            {
                var so=new SerializedObject(transition);
                if(so.FindProperty("revealPuzzle").objectReferenceValue==bus)
                {Set(transition,"objectsToReveal",bus.PuzzleRoot.GetComponentsInChildren<BoardTile>(true).OrderBy(t=>Vector3.Distance(t.transform.position,startWorld)).Select(t=>t.transform).ToArray());}
                if(so.FindProperty("sourcePuzzle").objectReferenceValue==bus)
                {Set(transition,"objectsToHide",bus.PuzzleRoot.GetComponentsInChildren<BoardTile>(true).Where(t=>t!=goal).Select(t=>t.transform).ToArray());}
            }
            if(BusScenerySnapshot(bus)!=before)throw new InvalidOperationException("Bus scenery moved.");
            if(Vector3.Distance(breakfastGoal.position,bus.Puzzle.PlayerStartTransform.position)>.0001f ||
                Vector3.Distance(washGoal.position,breakfast.Puzzle.PlayerStartTransform.position)>.0001f)
                throw new InvalidOperationException("Puzzle hand-off changed position.");
        }

        private static PathPieceData[] Pieces(params string[] names) => names.Select(n=>AssetDatabase.LoadAssetAtPath<PathPieceData>(
            "Assets/_Audere/Data/Puzzle/PathPieces/PathPiece_"+n+".asset")).ToArray();
    }
}
#endif
