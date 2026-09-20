using System.Collections.Generic;
using UnityEngine;

namespace Audere.Story.Presentation
{
    // Environment presentation opens one persistent row ahead of the actor.
    public sealed class TileRippleField : MonoBehaviour
    {
        [SerializeField] private TileRippleWalkProfile profile;
        [SerializeField] private SpriteRenderer[] tiles;
        [SerializeField] private SpriteRenderer startingTile;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private CanvasGroup whiteCover;
        [SerializeField] private GameObject originalFloor;
        [SerializeField] private StoryEvent ownerEvent;
        private Vector3[] positions;
        private Color[] colors;
        private Color cameraColor;
        private CameraClearFlags cameraFlags;
        private float originalCover;
        private bool originalFloorActive;
        private bool prepared;
        private Vector3 rowOrigin;

        public TileRippleWalkProfile Profile => profile;
        public IReadOnlyList<SpriteRenderer> Tiles => tiles;
        public bool IsPrepared => prepared;
        public float CoverAlpha => whiteCover != null ? whiteCover.alpha : 0f;

        private void OnEnable() { if (Application.isPlaying) Prepare(); }
        public bool Prepare()
        {
            if (prepared) return true;
            if (profile == null || worldCamera == null || whiteCover == null ||
                ownerEvent == null || startingTile == null || tiles == null || tiles.Length == 0) return false;
            foreach (var tile in tiles) if (tile == null) return false;
            positions = new Vector3[tiles.Length]; colors = new Color[tiles.Length];
            cameraColor = worldCamera.backgroundColor; cameraFlags = worldCamera.clearFlags;
            originalCover = whiteCover.alpha;
            originalFloorActive = originalFloor != null && originalFloor.activeSelf;
            rowOrigin = startingTile.transform.position;
            for (int i = 0; i < tiles.Length; i++)
            {
                positions[i] = tiles[i].transform.position;
                colors[i] = tiles[i].color;
                tiles[i].color = tiles[i] == startingTile ? Color.white : Color.clear;
            }
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.backgroundColor = profile.Background;
            whiteCover.alpha = 0f;
            if (originalFloor != null) originalFloor.SetActive(false);
            ownerEvent.Ended += HandleEnded;
            prepared = true;
            return true;
        }
        public void Tick(float time, float travelledDistance)
        {
            if (!prepared) return;
            float front = profile.PathLeadDistance * profile.RevealProgress(time) + travelledDistance;
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == null) continue;
                float distance = positions[i].x - rowOrigin.x;
                bool onRow = Mathf.Abs(positions[i].y - rowOrigin.y) < .01f && distance >= -.01f;
                float alpha = tiles[i] == startingTile ? 1f : onRow
                    ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((front - distance) / profile.TileRevealDistance)) : 0f;
                tiles[i].color = new Color(1f, 1f, 1f, alpha);
                tiles[i].transform.position = positions[i];
            }
            whiteCover.alpha = profile.WhiteAlpha(time);
        }
        public void Finish()
        {
            if (!prepared) return;
            for (int i = 0; i < tiles.Length; i++) if (tiles[i] != null)
            { tiles[i].color = Color.clear; tiles[i].transform.position = positions[i]; }
            whiteCover.alpha = 1f;
        }
        public void Restore()
        {
            if (!prepared) return;
            prepared = false;
            if (ownerEvent != null) ownerEvent.Ended -= HandleEnded;
            for (int i = 0; i < tiles.Length; i++) if (tiles[i] != null)
            { tiles[i].color = colors[i]; tiles[i].transform.position = positions[i]; }
            if (worldCamera != null) { worldCamera.backgroundColor = cameraColor; worldCamera.clearFlags = cameraFlags; }
            if (whiteCover != null) whiteCover.alpha = originalCover;
            if (originalFloor != null) originalFloor.SetActive(originalFloorActive);
            gameObject.SetActive(false);
        }
        private void HandleEnded(StoryEventResult result) { if (result != StoryEventResult.Completed) Restore(); }
        private void OnDisable() => Restore();
    }
}
