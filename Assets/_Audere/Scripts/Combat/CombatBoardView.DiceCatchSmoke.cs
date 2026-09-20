using UnityEngine;

namespace Audere.Combat
{
    public sealed partial class CombatBoardView
    {
        private CombatDiceCatchSmokeGraphic diceCatchSmoke;

        public void PlayDiceCatchVfx(CombatDieView die)
        {
            if (die == null || die.RectTransform == null || feedbackRoot == null)
                return;

            if (diceCatchSmoke == null)
                diceCatchSmoke = feedbackRoot.GetComponent<CombatDiceCatchSmokeGraphic>();
            if (diceCatchSmoke == null)
                return;

            diceCatchSmoke.Emit(GetCenterInSpace(die.RectTransform, feedbackRoot), die.CatchVfxColor);
        }

        private void ClearDiceCatchVfx()
        {
            if (diceCatchSmoke == null && feedbackRoot != null)
                diceCatchSmoke = feedbackRoot.GetComponent<CombatDiceCatchSmokeGraphic>();
            diceCatchSmoke?.Clear();
        }
    }
}
