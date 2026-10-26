using System.Collections.Generic;

namespace SlotGame.Data
{
    /// <summary>
    /// Encapsulates the complete outcome of a slot spin evaluation.
    /// Returned by the PayoutManager to be consumed by GameManager, UI, and Audio.
    /// </summary>
    public class SpinResult
    {
        public bool IsWin { get; set; }
        public bool IsJackpot { get; set; }
        public bool IsFreeSpinsTriggered { get; set; }
        public int FreeSpinsAwarded { get; set; }
        public int TotalPayout { get; set; }
        public float WinMultiplier { get; set; }
        public string WinDescription { get; set; }
        public SymbolType WinningSymbol { get; set; }
        public List<SymbolType> LandedSymbols { get; set; } = new List<SymbolType>();
        public List<int> WinningReelIndices { get; set; } = new List<int>();

        public SpinResult()
        {
            IsWin = false;
            IsJackpot = false;
            IsFreeSpinsTriggered = false;
            FreeSpinsAwarded = 0;
            TotalPayout = 0;
            WinMultiplier = 0f;
            WinDescription = "No Win";
            WinningSymbol = SymbolType.Bar;
        }

        public override string ToString()
        {
            return $"[SpinResult] IsWin: {IsWin}, Payout: {TotalPayout}, Multiplier: {WinMultiplier}x, Description: {WinDescription}";
        }
    }
}
