namespace SlotGame.Core
{
    /// <summary>
    /// Discrete states for the slot machine game state machine.
    /// Governs button interactions, lever behavior, animations, and payout flows.
    /// </summary>
    public enum GameState
    {
        Idle,            // Ready for player input (Bet changes, Spin, Lever pull)
        LeverPulling,    // Lever is being pulled / animated
        Spinning,        // Reels are in active spinning acceleration and high-speed cycle
        ReelStopping,    // Reels are decelerating and stopping in staggered sequence
        Evaluating,      // Evaluating paylines and calculating payouts via PayoutManager
        CelebratingWin,  // Payout celebration, rolling credits count-up, win particles/modal
        FreeSpinsBonus,  // Executing free spins round with active multiplier
        InsufficientBet  // Player credits are lower than current bet
    }
}
