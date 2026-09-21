using TMPro;
using System.Collections.Generic;
using UnityEngine;

namespace Audere.Combat
{
    public sealed partial class CombatBoardView
    {
        [SerializeField] private TMP_FontAsset teacherQuestionFont;
        private object teacherQuestionOwner;
        private TextMeshProUGUI teacherQuestion;
        private TeacherMathChoiceMove.QuestionHistory teacherQuestionHistory;
        private int teacherQuestionSession;

        internal TeacherMathChoiceMove.Question NextTeacherQuestion(ICombatRandom random, int sessionVersion)
        {
            if (teacherQuestionHistory == null || teacherQuestionSession != sessionVersion)
            {
                teacherQuestionHistory = new TeacherMathChoiceMove.QuestionHistory();
                teacherQuestionSession = sessionVersion;
            }
            return teacherQuestionHistory.Next(random);
        }

        private void ResetTeacherQuestionHistory() => teacherQuestionHistory = null;

        // All overlapping choices share the same catch gesture; the closest center owns it.
        // A wide cursor must never let array order override the die directly under the Heart.
        public int FindClosestChoiceUnderCursor(IReadOnlyList<CombatDieView> choices)
        {
            if (choices == null || catchCursor == null || playArea == null) return -1;
            Vector2 cursorCenter = CatchZoneCenter;
            float closestDistance = float.PositiveInfinity;
            int closest = -1;
            for (int i = 0; i < choices.Count; i++)
            {
                var die = choices[i];
                if (die == null || !die.gameObject.activeInHierarchy || !die.CanInteract || !CursorOverlaps(die)) continue;
                Vector2 center = GetCenterInSpace(die.RectTransform, playArea);
                float distance = (center - cursorCenter).sqrMagnitude;
                if (distance >= closestDistance) continue;
                closestDistance = distance;
                closest = i;
            }
            return closest;
        }

        public bool IsTeacherQuestionVisible => teacherQuestion != null && teacherQuestion.gameObject.activeSelf;
        public TMP_FontAsset TeacherQuestionFont => teacherQuestionFont != null ? teacherQuestionFont : damageNumberFont;

        public void ShowTeacherQuestion(object owner, string message)
        {
            if (owner == null || playArea == null) return;
            if (teacherQuestionOwner != null && !ReferenceEquals(teacherQuestionOwner, owner)) return;
            teacherQuestionOwner = owner;
            if (teacherQuestion == null)
            {
                var go = new GameObject("Teacher question (runtime)", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                go.layer = playArea.gameObject.layer;
                go.transform.SetParent(playArea.parent, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(380f, 64f);
                teacherQuestion = go.GetComponent<TextMeshProUGUI>();
                teacherQuestion.font = TeacherQuestionFont != null ? TeacherQuestionFont : TMP_Settings.defaultFontAsset;
                teacherQuestion.fontSize = 43f;
                teacherQuestion.alignment = TextAlignmentOptions.Center;
                teacherQuestion.color = Color.white;
                teacherQuestion.raycastTarget = false;
            }
            teacherQuestion.text = message;
            teacherQuestion.rectTransform.anchoredPosition =
                playArea.anchoredPosition + Vector2.up * (playArea.rect.height * .5f + 42f);
            teacherQuestion.transform.SetAsLastSibling();
            teacherQuestion.gameObject.SetActive(true);
        }

        public void HideTeacherQuestion(object owner)
        {
            if (!ReferenceEquals(owner, teacherQuestionOwner)) return;
            ClearTeacherQuestion();
        }

        private void ClearTeacherQuestion()
        {
            teacherQuestionOwner = null;
            if (teacherQuestion != null) teacherQuestion.gameObject.SetActive(false);
        }
    }
}
