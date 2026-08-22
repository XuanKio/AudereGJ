using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Audere.Story.Steps
{
    public sealed class SetActiveStep : StoryStep
    {
        [SerializeField] private GameObject[] objectsToEnable = new GameObject[0];
        [SerializeField] private GameObject[] objectsToDisable = new GameObject[0];

        public IReadOnlyList<GameObject> ObjectsToEnable => objectsToEnable;
        public IReadOnlyList<GameObject> ObjectsToDisable => objectsToDisable;

        protected override IEnumerator Execute()
        {
            HashSet<GameObject> enableSet = CollectEnableObjects();

            for (int index = 0; index < objectsToDisable.Length; index++)
            {
                GameObject target = objectsToDisable[index];
                if (target == null)
                {
                    LogNullEntry("Objects To Disable", index);
                    continue;
                }

                if (enableSet.Contains(target))
                {
                    Debug.LogWarning(
                        $"[SetActiveStep] '{target.name}' appears in both Objects To Disable and " +
                        "Objects To Enable. Disable runs first, then Enable wins.",
                        this);
                }

                target.SetActive(false);
            }

            for (int index = 0; index < objectsToEnable.Length; index++)
            {
                GameObject target = objectsToEnable[index];
                if (target == null)
                    continue;

                target.SetActive(true);
            }

            CompleteStep();
            yield break;
        }

        private HashSet<GameObject> CollectEnableObjects()
        {
            HashSet<GameObject> enableSet = new HashSet<GameObject>();

            for (int index = 0; index < objectsToEnable.Length; index++)
            {
                GameObject target = objectsToEnable[index];
                if (target == null)
                {
                    LogNullEntry("Objects To Enable", index);
                    continue;
                }

                enableSet.Add(target);
            }

            return enableSet;
        }

        private void LogNullEntry(string listName, int index)
        {
            Debug.LogWarning(
                $"[SetActiveStep] '{name}' has a null entry in {listName} at index {index}. " +
                "The entry was skipped.",
                this);
        }
    }
}
