using System.Collections.Generic;
using UnityEngine;

namespace Audere.GameplayInput
{
    public readonly struct GameplayInputToken
    {
        internal GameplayInputToken(GameplayInputGate gate, int claimId)
        {
            Gate = gate;
            ClaimId = claimId;
        }

        internal GameplayInputGate Gate { get; }
        internal int ClaimId { get; }

        public bool IsValid => Gate != null && ClaimId != 0;
    }

    [DisallowMultipleComponent]
    public sealed class GameplayInputGate : MonoBehaviour
    {
        private readonly List<Claim> claims = new List<Claim>();
        private int nextClaimId;

        public GameplayInputMode CurrentMode
        {
            get
            {
                RemoveDestroyedOwnerClaims();

                for (int index = claims.Count - 1; index >= 0; index--)
                {
                    if (claims[index].Mode == GameplayInputMode.Dialogue)
                        return GameplayInputMode.Dialogue;
                }

                return claims.Count > 0
                    ? claims[claims.Count - 1].Mode
                    : GameplayInputMode.None;
            }
        }

        public int ActiveClaimCount
        {
            get
            {
                RemoveDestroyedOwnerClaims();
                return claims.Count;
            }
        }

        private void OnDisable()
        {
            claims.Clear();
        }

        public GameplayInputToken PushMode(Object owner, GameplayInputMode mode)
        {
            if (!isActiveAndEnabled)
            {
                Debug.LogError("[GameplayInputGate] Enable the gate before pushing an input claim.", this);
                return default;
            }

            if (owner == null)
            {
                Debug.LogError("[GameplayInputGate] A claim requires an owner.", this);
                return default;
            }

            if (mode == GameplayInputMode.None)
            {
                Debug.LogError("[GameplayInputGate] None cannot be pushed as an input claim.", owner);
                return default;
            }

            int claimId = NextClaimId();
            claims.Add(new Claim(claimId, owner, mode));
            return new GameplayInputToken(this, claimId);
        }

        public bool Release(GameplayInputToken token)
        {
            if (token.Gate != this || token.ClaimId == 0)
                return false;

            for (int index = claims.Count - 1; index >= 0; index--)
            {
                if (claims[index].Id != token.ClaimId)
                    continue;

                claims.RemoveAt(index);
                return true;
            }

            return false;
        }

        public bool IsActive(GameplayInputToken token)
        {
            if (token.Gate != this || token.ClaimId == 0)
                return false;

            RemoveDestroyedOwnerClaims();

            for (int index = claims.Count - 1; index >= 0; index--)
            {
                if (claims[index].Id == token.ClaimId)
                    return true;
            }

            return false;
        }

        public bool Allows(GameplayInputMode mode)
        {
            return CurrentMode == mode;
        }

        private int NextClaimId()
        {
            do
            {
                nextClaimId++;
            }
            while (nextClaimId == 0);

            return nextClaimId;
        }

        private void RemoveDestroyedOwnerClaims()
        {
            for (int index = claims.Count - 1; index >= 0; index--)
            {
                if (claims[index].Owner == null)
                    claims.RemoveAt(index);
            }
        }

        private readonly struct Claim
        {
            public Claim(int id, Object owner, GameplayInputMode mode)
            {
                Id = id;
                Owner = owner;
                Mode = mode;
            }

            public int Id { get; }
            public Object Owner { get; }
            public GameplayInputMode Mode { get; }
        }
    }
}
