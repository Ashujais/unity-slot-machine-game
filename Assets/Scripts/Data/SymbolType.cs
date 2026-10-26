namespace SlotGame.Data
{
    /// <summary>
    /// Enumeration of all available slot symbol types.
    /// Includes classic high/mid/low paying symbols and a special Wild bonus symbol.
    /// </summary>
    public enum SymbolType
    {
        Seven = 0,   // Highest jackpot symbol (Lucky 7)
        Cherry = 1,  // High paying fruit symbol
        Bell = 2,    // Medium paying golden bell
        Bar = 3,     // Standard classic bar symbol
        Wild = 4     // Bonus Wild card: substitutes for any symbol to complete winning lines
    }
}
