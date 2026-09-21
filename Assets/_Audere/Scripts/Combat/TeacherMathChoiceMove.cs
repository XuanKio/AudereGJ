using System;
using System.Collections.Generic;
using Audere.Audio;
using UnityEngine;

namespace Audere.Combat
{
    // The question owns the three visible dice; regular combat batches resume on the next move.
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Arithmetic Choice")]
    public sealed class TeacherMathChoiceMove : CombatMoveDefinition, ICombatExclusiveDiceMove
    {
        [SerializeField, Min(.3f)] private float compactDuration = .65f;
        [SerializeField, Min(.3f)] private float expandDuration = .65f;
        [SerializeField] private CombatMoveDefinition incorrectAnswerFollowUp;
        [SerializeField, Range(180f, 420f)] private float compactWidth = 360f;
        [SerializeField, Range(130f, 260f)] private float compactHeight = 190f;
        [SerializeField, Range(0f, 220f)] private float orbitWidth = 140f;
        [SerializeField, Range(0f, 110f)] private float orbitHeight = 60f;
        [SerializeField] private Material monochromeEchoMaterial;

        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (compactDuration <= 0f || expandDuration <= 0f ||
                compactWidth < 180f || compactHeight < 130f)
            {
                error = "Arithmetic choice needs room for three dice and positive transition timings.";
                return false;
            }
            error = null;
            return true;
        }

        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context)
        {
            if (!Validate(out string error)) throw new InvalidOperationException(error);
            return new Execution(this, context);
        }

        internal readonly struct Question
        {
            private readonly int[] answers;
            public Question(int left, int right, bool addition, int[] answers, int correctIndex)
            {
                Text = $"{left} {(addition ? "+" : "-")} {right} = ?";
                this.answers = answers;
                CorrectIndex = correctIndex;
            }
            public string Text { get; }
            public int CorrectIndex { get; }
            public string AnswerLabel(int index) => answers[index].ToString();
        }

        // Owned by a board attempt, never by the move asset or a global question index.
        internal sealed class QuestionHistory
        {
            private readonly struct Candidate
            {
                public Candidate(int left, int right, bool addition)
                { Left = left; Right = right; Addition = addition; }
                public readonly int Left, Right;
                public readonly bool Addition;
                public int Answer => Addition ? Left + Right : Left - Right;
            }

            private readonly List<Candidate> candidates = new List<Candidate>();
            private readonly HashSet<int> usedQuestions = new HashSet<int>();
            private readonly HashSet<int> usedAnswers = new HashSet<int>();
            private bool? lastAddition;
            private int lastAnswer = -1;

            public QuestionHistory()
            {
                for (int left = 11; left <= 98; left++)
                for (int right = 11; right <= 98; right++)
                {
                    // Carrying and borrowing keep both operations above single-digit difficulty.
                    if (left <= right && left + right <= 99 && left % 10 + right % 10 >= 10)
                        candidates.Add(new Candidate(left, right, true));
                    if (left - right >= 11 && left % 10 < right % 10)
                        candidates.Add(new Candidate(left, right, false));
                }
            }

            public Question Next(ICombatRandom random)
            {
                bool addition = lastAddition.HasValue ? !lastAddition.Value : random.Value01() < .5f;
                int start = RandomIndex(random, candidates.Count);
                int selected = Find(start, addition, true);
                if (selected < 0) selected = Find(start, !addition, true);
                if (selected < 0)
                {
                    // Exhaust answers before permitting a repeat; equations have their own larger bag.
                    usedAnswers.Clear();
                    selected = Find(start, addition, false);
                    if (selected < 0) selected = Find(start, !addition, false);
                }
                if (selected < 0)
                {
                    usedQuestions.Clear();
                    selected = Find(start, addition, false);
                }
                Candidate candidate = candidates[selected];
                usedQuestions.Add(selected);
                usedAnswers.Add(candidate.Answer);
                lastAddition = candidate.Addition;
                lastAnswer = candidate.Answer;

                int[] answers = { candidate.Answer, 0, 0 };
                int[] offsets = { -10, -2, -1, 1, 2, 10 };
                int offsetStart = RandomIndex(random, offsets.Length), count = 1;
                for (int i = 0; i < offsets.Length && count < answers.Length; i++)
                {
                    int value = candidate.Answer + offsets[(offsetStart + i) % offsets.Length];
                    if (value >= 10 && value <= 99) answers[count++] = value;
                }
                for (int i = answers.Length - 1; i > 0; i--)
                {
                    int j = RandomIndex(random, i + 1);
                    int value = answers[i]; answers[i] = answers[j]; answers[j] = value;
                }
                return new Question(candidate.Left, candidate.Right, candidate.Addition,
                    answers, Array.IndexOf(answers, candidate.Answer));
            }

            private int Find(int start, bool addition, bool avoidUsedAnswers)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    int index = (start + i) % candidates.Count;
                    Candidate candidate = candidates[index];
                    if (candidate.Addition == addition && !usedQuestions.Contains(index) &&
                        candidate.Answer != lastAnswer && (!avoidUsedAnswers || !usedAnswers.Contains(candidate.Answer)))
                        return index;
                }
                return -1;
            }

            private static int RandomIndex(ICombatRandom random, int count) =>
                Mathf.Clamp(Mathf.FloorToInt(random.Value01() * count), 0, count - 1);
        }

        private sealed class Execution : ICombatMoveExecution, ICombatMoveInputHandler, ICombatMoveDamageReward, ICombatMoveFollowUp
        {
            private readonly TeacherMathChoiceMove data;
            private readonly CombatMoveExecutionContext context;
            private readonly CombatDieView[] choices = new CombatDieView[3];
            private readonly Question question;
            private Vector2 enemyRest, orbitCenter, enemyPosition, exitFrom;
            private float elapsed, answerFadeElapsed, exitElapsed;
            private float widthFraction, heightFraction;
            private int pendingDamage;
            private bool initialized, answered, cancelled, orbitActive;
            public CombatMoveDefinition NextMove { get; private set; }

            public Execution(TeacherMathChoiceMove data, CombatMoveExecutionContext context)
            {
                this.data = data;
                this.context = context;
                question = context.Board != null
                    ? context.Board.NextTeacherQuestion(context.Random, context.SessionVersion)
                    : new QuestionHistory().Next(context.Random);
            }

            public bool IsComplete => cancelled || answered && exitElapsed >= data.expandDuration;

            public void Tick(float deltaTime)
            {
                if (IsComplete || deltaTime <= 0f) return;
                if (context.Board == null || !context.Board.isActiveAndEnabled) { Cancel(); return; }
                if (!initialized) Initialize();
                if (answered)
                {
                    answerFadeElapsed += deltaTime;
                    if (answerFadeElapsed < .24f)
                    {
                        if (orbitActive) context.Board.SetMountDivePose(this, exitFrom, 0f, deltaTime, false);
                        float alpha = 1f - answerFadeElapsed / .24f;
                        foreach (var die in choices)
                        {
                            var group = die != null ? die.GetComponent<CanvasGroup>() : null;
                            if (group != null) group.alpha = alpha;
                        }
                        return;
                    }
                    if (choices[0] != null) HideChoices();
                    exitElapsed = Mathf.Min(data.expandDuration, exitElapsed + deltaTime);
                    float t = Mathf.SmoothStep(0f, 1f, exitElapsed / data.expandDuration);
                    context.Board.SetBattleBoxSizeLayout(Mathf.Lerp(widthFraction, 1f, t),
                        Mathf.Lerp(heightFraction, 1f, t), 0f);
                    if (orbitActive)
                    {
                        context.Board.SetMountDivePose(this, Vector2.Lerp(exitFrom, enemyRest, t), 0f, deltaTime, false);
                        if (t >= 1f) { context.Board.EndMountDive(this); orbitActive = false; }
                    }
                    return;
                }

                elapsed += deltaTime;
                float compactT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / data.compactDuration));
                context.Board.SetBattleBoxSizeLayout(Mathf.Lerp(1f, widthFraction, compactT),
                    Mathf.Lerp(1f, heightFraction, compactT), 0f);
                if (orbitActive)
                {
                    Vector2 orbit = orbitCenter + new Vector2(Mathf.Cos(elapsed * 2.75f) * data.orbitWidth,
                        Mathf.Sin(elapsed * 2.75f) * data.orbitHeight);
                    enemyPosition = Vector2.Lerp(enemyRest, orbit, compactT);
                    context.Board.SetMountDivePose(this, enemyPosition, 0f, deltaTime, compactT > .1f);
                }
                if (compactT < 1f) return;
                if (choices[0] == null) ShowChoices();
            }

            private void Initialize()
            {
                initialized = true;
                Rect boardRect = context.Board.PlayArea.rect;
                widthFraction = Mathf.Clamp(data.compactWidth / boardRect.width, .25f, 1f);
                heightFraction = Mathf.Clamp(data.compactHeight / boardRect.height, .25f, 1f);
                orbitActive = context.Board.BeginMountDive(this, context.Actor, data.monochromeEchoMaterial, true);
                if (orbitActive)
                {
                    enemyRest = enemyPosition = orbitCenter = context.Board.MountDiveHome;
                    float lowest = enemyRest.y;
                    var corners = new Vector3[4];
                    foreach (var graphic in context.Actor.GetComponentsInChildren<UnityEngine.UI.Image>())
                    {
                        graphic.rectTransform.GetWorldCorners(corners);
                        foreach (var corner in corners) lowest = Mathf.Min(lowest, context.Board.WorldToPlayArea(corner).y);
                    }
                    // Reserve the prompt and answer row below the complete authored actor silhouette.
                    float safeBottom = boardRect.height * heightFraction * .5f + 86f;
                    orbitCenter.y = Mathf.Max(enemyRest.y, safeBottom + enemyRest.y - lowest + data.orbitHeight);
                }
            }

            private void ShowChoices()
            {
                context.Board.ShowTeacherQuestion(this, question.Text);
                for (int i = 0; i < choices.Length; i++)
                {
                    choices[i] = context.Board.SpawnChoiceDie((CombatSymbol)i, new Vector2(.04f + .46f * i, .34f));
                    choices[i]?.SetChoiceAnswerLabel(question.AnswerLabel(i), context.Board.TeacherQuestionFont);
                }
            }

            public void HandleInput(bool catchPressed, bool rerollPressed)
            {
                if (!catchPressed || answered || cancelled || choices[0] == null) return;
                int i = context.Board.FindClosestChoiceUnderCursor(choices);
                if (i >= 0)
                {
                    var die = choices[i];
                    if (context.Board.IsCursorStunned) { context.Board.PlayBlockedCursorFeedback(); return; }
                    context.Board.PlayDiceCatchVfx(die);
                    AudioService.Instance?.Play(AudioId.Dice_Catch);
                    if (i == question.CorrectIndex)
                    {
                        pendingDamage = 3;
                    }
                    else
                    {
                        NextMove = data.incorrectAnswerFollowUp;
                    }
                    answered = true;
                    exitFrom = enemyPosition;
                    context.Board.HideTeacherQuestion(this);
                    return;
                }
            }

            public int ConsumePendingDamage()
            {
                int result = pendingDamage;
                pendingDamage = 0;
                return result;
            }

            private void HideChoices()
            {
                foreach (var die in choices)
                    die?.ReturnToPool();
                Array.Clear(choices, 0, choices.Length);
            }

            public void Cancel()
            {
                if (cancelled) return;
                cancelled = true;
                NextMove = null;
                pendingDamage = 0;
                HideChoices();
                if (context.Board == null) return;
                context.Board.HideTeacherQuestion(this);
                context.Board.ResetBattleBoxLayout();
                context.Board.EndMountDive(this);
                orbitActive = false;
            }
        }
    }
}
