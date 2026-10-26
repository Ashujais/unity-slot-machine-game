using System.Collections.Generic;
using UnityEngine;
using SlotGame.Data;

namespace SlotGame.Core
{
    /// <summary>
    /// Handles evaluation of spin outcomes, winning combination detection,
    /// Wild substitutions, and payout calculations.
    /// </summary>
    public class PayoutManager : MonoBehaviour
    {
        [Header("Payout Multipliers (Applied to active bet)")]
        [SerializeField] private float threeWildsMultiplier = 150f;
        [SerializeField] private float threeSevensMultiplier = 100f;
        [SerializeField] private float threeCherriesMultiplier = 40f;
        [SerializeField] private float threeBellsMultiplier = 25f;
        [SerializeField] private float threeBarsMultiplier = 15f;
        [SerializeField] private float twoOfAKindMultiplier = 2f;

        [Header("Bonus Feature Settings")]
        [SerializeField] private int freeSpinsOnThreeWilds = 10;
        [SerializeField] private int freeSpinsOnTwoWilds = 5;

        /// <summary>
        /// Evaluates a 3-symbol spin outcome and computes payouts, multipliers, and bonuses.
        /// </summary>
        /// <param name="outcome">Array of 3 symbols landed on the payline</param>
        /// <param name="betAmount">Current active bet in credits</param>
        /// <param name="featureMultiplier">Active bonus multiplier (e.g. 2x or 3x during Free Spins)</param>
        /// <returns>SpinResult containing all payout details</returns>
        public SpinResult EvaluateSpin(SymbolType[] outcome, int betAmount, float featureMultiplier = 1.0f)
        {
            var result = new SpinResult();
            if (outcome == null || outcome.Length < 3) return result;

            result.LandedSymbols = new List<SymbolType>(outcome);

            SymbolType s0 = outcome[0];
            SymbolType s1 = outcome[1];
            SymbolType s2 = outcome[2];

            // 1. Count Wild symbols landed
            int wildCount = 0;
            if (s0 == SymbolType.Wild) wildCount++;
            if (s1 == SymbolType.Wild) wildCount++;
            if (s2 == SymbolType.Wild) wildCount++;

            // 2. Check for 3 of a kind (Direct or via Wild substitution)
            // Case A: 3 Wilds (Ultimate Super Jackpot + 10 Free Spins)
            if (wildCount == 3)
            {
                result.IsWin = true;
                result.IsJackpot = true;
                result.IsFreeSpinsTriggered = true;
                result.FreeSpinsAwarded = freeSpinsOnThreeWilds;
                result.WinningSymbol = SymbolType.Wild;
                result.WinMultiplier = threeWildsMultiplier * featureMultiplier;
                result.TotalPayout = Mathf.RoundToInt(betAmount * result.WinMultiplier);
                result.WinDescription = $"SUPER WILD JACKPOT! 3x WILDS! (+{freeSpinsOnThreeWilds} FREE SPINS)";
                result.WinningReelIndices = new List<int> { 0, 1, 2 };
                return result;
            }

            // Case B: Regular 3 of a kind with Wild substitution
            // Determine non-wild symbols
            List<SymbolType> nonWilds = new List<SymbolType>();
            if (s0 != SymbolType.Wild) nonWilds.Add(s0);
            if (s1 != SymbolType.Wild) nonWilds.Add(s1);
            if (s2 != SymbolType.Wild) nonWilds.Add(s2);

            // Check if all non-wild symbols are identical
            bool isThreeOfAKind = true;
            SymbolType targetSymbol = nonWilds.Count > 0 ? nonWilds[0] : SymbolType.Wild;
            foreach (var sym in nonWilds)
            {
                if (sym != targetSymbol)
                {
                    isThreeOfAKind = false;
                    break;
                }
            }

            if (isThreeOfAKind)
            {
                result.IsWin = true;
                result.WinningSymbol = targetSymbol;
                result.WinningReelIndices = new List<int> { 0, 1, 2 };

                float baseMultiplier = GetThreeOfAKindMultiplier(targetSymbol);
                result.WinMultiplier = baseMultiplier * featureMultiplier;
                result.TotalPayout = Mathf.RoundToInt(betAmount * result.WinMultiplier);

                if (targetSymbol == SymbolType.Seven)
                {
                    result.IsJackpot = true;
                    result.WinDescription = $"MEGA JACKPOT! 3x SEVENS! ({result.TotalPayout} Credits)";
                }
                else
                {
                    string wildSuffix = wildCount > 0 ? $" (With {wildCount} Wild!)" : "";
                    result.WinDescription = $"WIN! 3x {targetSymbol.ToString().ToUpper()}{wildSuffix}! ({result.TotalPayout} Credits)";
                }

                // If 2 wilds were used, also award free spins
                if (wildCount == 2)
                {
                    result.IsFreeSpinsTriggered = true;
                    result.FreeSpinsAwarded = freeSpinsOnTwoWilds;
                    result.WinDescription += $" + {freeSpinsOnTwoWilds} FREE SPINS!";
                }

                return result;
            }

            // 3. Check for 2 of a kind (Partial win)
            // Check matching pairs (including Wild substitutes)
            if (CheckTwoMatch(s0, s1))
            {
                SetTwoOfAKindResult(result, s0 != SymbolType.Wild ? s0 : s1, 0, 1, betAmount, featureMultiplier);
                return result;
            }
            if (CheckTwoMatch(s1, s2))
            {
                SetTwoOfAKindResult(result, s1 != SymbolType.Wild ? s1 : s2, 1, 2, betAmount, featureMultiplier);
                return result;
            }
            if (CheckTwoMatch(s0, s2))
            {
                SetTwoOfAKindResult(result, s0 != SymbolType.Wild ? s0 : s2, 0, 2, betAmount, featureMultiplier);
                return result;
            }

            // 4. No win
            result.IsWin = false;
            result.TotalPayout = 0;
            result.WinDescription = "No Match. Try again!";
            return result;
        }

        private bool CheckTwoMatch(SymbolType a, SymbolType b)
        {
            if (a == SymbolType.Wild || b == SymbolType.Wild) return true;
            return a == b;
        }

        private void SetTwoOfAKindResult(SpinResult result, SymbolType matchedSymbol, int reelA, int reelB, int betAmount, float featureMultiplier)
        {
            result.IsWin = true;
            result.WinningSymbol = matchedSymbol;
            result.WinMultiplier = twoOfAKindMultiplier * featureMultiplier;
            result.TotalPayout = Mathf.RoundToInt(betAmount * result.WinMultiplier);
            result.WinningReelIndices = new List<int> { reelA, reelB };
            result.WinDescription = $"2x MATCH! ({result.TotalPayout} Credits)";
        }

        public float GetThreeOfAKindMultiplier(SymbolType type)
        {
            switch (type)
            {
                case SymbolType.Wild: return threeWildsMultiplier;
                case SymbolType.Seven: return threeSevensMultiplier;
                case SymbolType.Cherry: return threeCherriesMultiplier;
                case SymbolType.Bell: return threeBellsMultiplier;
                case SymbolType.Bar: return threeBarsMultiplier;
                default: return 10f;
            }
        }
    }
}
