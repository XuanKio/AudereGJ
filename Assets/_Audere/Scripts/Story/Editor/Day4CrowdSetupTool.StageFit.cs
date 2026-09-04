#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Audere.Story.Editor
{
 public static partial class Day4CrowdSetupTool
 {
  [MenuItem("Audere/Story/Fit Active Day4 Classroom Stage Inside Mask")]
  public static void FitStageActive()
  {
   var scene=SceneManager.GetActiveScene();
   if(scene.path!=ScenePath||scene.isDirty||EditorApplication.isPlaying||EditorApplication.isCompiling)
    throw new InvalidOperationException("Open saved Scene140 in Edit Mode first.");
   var stage=All<Transform>(scene).Single(t=>t.name=="DAY FOUR TILE CLASSROOM");
   var audereTile=stage.Find("Audere Tile");var biancaTile=stage.Find("Bianca Tile");
   Vector3 oldA=audereTile.position,oldB=biancaTile.position;
   // Ground plane is the midpoint of the two adjacent actor tiles. Resize scenery only.
   Vector3 pivot=(oldA+oldB)*.5f;
   float ratio=.20f/audereTile.lossyScale.x;
   foreach(Transform tile in stage)
   {
    tile.position=pivot+(tile.position-pivot)*ratio;
    tile.localScale*=ratio;
   }
   var anchors=All<Transform>(scene).Single(t=>t.name=="DAY FOUR POSE ANCHORS");
   var a=All<Transform>(scene).Single(t=>t.name=="Audere");var b=All<Transform>(scene).Single(t=>t.name=="Bianca");
   Vector3 deltaA=audereTile.position-oldA,deltaB=biancaTile.position-oldB;
   // Translation preserves body scale and the grounded shadow's authored offset/style.
   a.position+=deltaA;b.position+=deltaB;
   foreach(Transform anchor in anchors)
    if(anchor.name.StartsWith("Audere",StringComparison.Ordinal))anchor.position+=deltaA;
    else if(anchor.name.StartsWith("Bianca",StringComparison.Ordinal))anchor.position+=deltaB;
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }
 }
}
#endif

