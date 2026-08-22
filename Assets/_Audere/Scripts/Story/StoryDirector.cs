using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Audere.Story
{
    [DisallowMultipleComponent]
    public sealed class StoryDirector : MonoBehaviour
    {
        [SerializeField] private Transform storyEventsRoot;

        [Header("Scene Startup")]
        [SerializeField] private bool playOnStart;
        [SerializeField] private StoryEvent startingEvent;

        private readonly List<StoryEvent> registeredEvents = new List<StoryEvent>();
        private readonly Dictionary<string, StoryEvent> eventsById =
            new Dictionary<string, StoryEvent>(StringComparer.Ordinal);

        private StoryEvent currentEvent;
        private Action<StoryEventResult> activeCompletion;
        private Coroutine pendingAutoNextRoutine;
        private uint continuationVersion;

        public Transform StoryEventsRoot => storyEventsRoot;
        public StoryEvent CurrentEvent => currentEvent;
        public bool IsPlaying => currentEvent != null && currentEvent.IsPlaying;

        private void Awake()
        {
            RefreshRegistry();
        }

        private void Start()
        {
            if (!playOnStart)
                return;

            if (startingEvent == null)
            {
                Debug.LogError(
                    "[StoryDirector] Play On Start is enabled but Starting Event is not assigned.",
                    this);
                return;
            }

            PlayEvent(startingEvent);
        }

        private void OnDisable()
        {
            CancelCurrentEvent();
        }

        public void RefreshRegistry()
        {
            registeredEvents.Clear();
            eventsById.Clear();

            if (storyEventsRoot == null)
            {
                Debug.LogError("[StoryDirector] Assign Story Events Root.", this);
                return;
            }

            StoryEvent[] foundEvents = storyEventsRoot.GetComponentsInChildren<StoryEvent>(true);
            for (int index = 0; index < foundEvents.Length; index++)
            {
                StoryEvent storyEvent = foundEvents[index];
                registeredEvents.Add(storyEvent);

                if (string.IsNullOrWhiteSpace(storyEvent.EventId))
                {
                    Debug.LogWarning("[StoryDirector] StoryEvent has an empty EventId.", storyEvent);
                    continue;
                }

                if (eventsById.TryGetValue(storyEvent.EventId, out StoryEvent existing))
                {
                    Debug.LogWarning(
                        $"[StoryDirector] Duplicate EventId '{storyEvent.EventId}' on " +
                        $"'{existing.gameObject.name}' and '{storyEvent.gameObject.name}'. " +
                        "PlayEventById will keep the first registered event.",
                        storyEvent);
                    continue;
                }

                eventsById.Add(storyEvent.EventId, storyEvent);
            }

            ValidateRegisteredEventChains();
        }

        public bool PlayEvent(
            StoryEvent eventReference,
            Action<StoryEventResult> onEnded = null)
        {
            if (eventReference == null)
            {
                Debug.LogWarning("[StoryDirector] Cannot play a null StoryEvent reference.", this);
                return false;
            }

            if (IsPlaying)
            {
                Debug.LogWarning(
                    $"[StoryDirector] Cannot start '{eventReference.EventId}' while " +
                    $"'{currentEvent.EventId}' is running.",
                    eventReference);
                return false;
            }

            InvalidatePendingAutoNext();
            return StartEvent(eventReference, onEnded);
        }

        private bool StartEvent(
            StoryEvent eventReference,
            Action<StoryEventResult> onEnded)
        {
            if (!registeredEvents.Contains(eventReference))
            {
                RefreshRegistry();
                if (!registeredEvents.Contains(eventReference))
                {
                    Debug.LogWarning(
                        $"[StoryDirector] StoryEvent '{eventReference.gameObject.name}' is not registered " +
                        "under Story Events Root.",
                        eventReference);
                    return false;
                }
            }

            currentEvent = eventReference;
            activeCompletion = null;
            activeCompletion = onEnded;

            bool started = eventReference.Play(result => HandleEventEnded(eventReference, result));
            if (started)
                return true;

            if (currentEvent == eventReference)
            {
                currentEvent = null;
                activeCompletion = null;
            }
            return false;
        }

        public bool PlayEventById(
            string eventId,
            Action<StoryEventResult> onEnded = null)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                Debug.LogWarning("[StoryDirector] EventId cannot be empty.", this);
                return false;
            }

            if (!eventsById.TryGetValue(eventId, out StoryEvent storyEvent))
            {
                RefreshRegistry();
                if (!eventsById.TryGetValue(eventId, out storyEvent))
                {
                    Debug.LogWarning($"[StoryDirector] EventId '{eventId}' is not registered.", this);
                    return false;
                }
            }

            return PlayEvent(storyEvent, onEnded);
        }

        public void CancelCurrentEvent()
        {
            InvalidatePendingAutoNext();

            if (currentEvent == null || !currentEvent.IsPlaying)
                return;

            currentEvent.Cancel();
        }

        private void HandleEventEnded(StoryEvent source, StoryEventResult result)
        {
            if (currentEvent != source)
                return;

            currentEvent = null;
            Action<StoryEventResult> completion = activeCompletion;
            activeCompletion = null;
            uint versionBeforeCallback = continuationVersion;
            completion?.Invoke(result);

            if (result != StoryEventResult.Completed ||
                !source.AutoPlayNextEvent ||
                versionBeforeCallback != continuationVersion ||
                currentEvent != null)
            {
                return;
            }

            if (!TryGetValidNextEvent(source, out StoryEvent nextEvent))
                return;

            uint scheduledVersion = ++continuationVersion;
            pendingAutoNextRoutine = StartCoroutine(
                PlayNextEventDeferred(source, nextEvent, scheduledVersion));
        }

        private IEnumerator PlayNextEventDeferred(
            StoryEvent source,
            StoryEvent nextEvent,
            uint scheduledVersion)
        {
            yield return null;

            if (scheduledVersion != continuationVersion)
                yield break;

            pendingAutoNextRoutine = null;

            if (currentEvent != null)
            {
                Debug.LogWarning(
                    $"[StoryDirector] Cannot auto-start '{nextEvent.EventId}' after '{source.EventId}' " +
                    "because another StoryEvent is already running.",
                    source);
                yield break;
            }

            StartEvent(nextEvent, null);
        }

        private bool TryGetValidNextEvent(StoryEvent source, out StoryEvent nextEvent)
        {
            nextEvent = source.NextEvent;

            if (nextEvent == null)
            {
                Debug.LogWarning(
                    $"[StoryDirector] StoryEvent '{source.EventId}' has Auto Play Next Event enabled " +
                    "but Next Event is not assigned.",
                    source);
                return false;
            }

            if (nextEvent == source)
            {
                Debug.LogWarning(
                    $"[StoryDirector] StoryEvent '{source.EventId}' cannot use itself as Next Event.",
                    source);
                return false;
            }

            if (nextEvent.AutoPlayNextEvent && nextEvent.NextEvent == source)
            {
                Debug.LogWarning(
                    $"[StoryDirector] Direct StoryEvent cycle detected: " +
                    $"'{source.EventId}' -> '{nextEvent.EventId}' -> '{source.EventId}'.",
                    source);
                return false;
            }

            if (!registeredEvents.Contains(nextEvent))
            {
                Debug.LogWarning(
                    $"[StoryDirector] Next Event '{nextEvent.EventId}' referenced by '{source.EventId}' " +
                    "is not registered under this Story Events Root.",
                    source);
                return false;
            }

            return true;
        }

        private void ValidateRegisteredEventChains()
        {
            for (int index = 0; index < registeredEvents.Count; index++)
            {
                StoryEvent storyEvent = registeredEvents[index];
                if (storyEvent.AutoPlayNextEvent)
                    TryGetValidNextEvent(storyEvent, out _);
            }
        }

        private void InvalidatePendingAutoNext()
        {
            continuationVersion++;

            if (pendingAutoNextRoutine != null)
                StopCoroutine(pendingAutoNextRoutine);
            pendingAutoNextRoutine = null;
        }
    }
}
