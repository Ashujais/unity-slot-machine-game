using System;
using UnityEngine;

namespace SlotGame.Data
{
    /// <summary>
    /// Defines a payline configuration across the slot grid.
    /// In a classic 3-reel slot machine, the center horizontal row is the primary payline.
    /// Can be extended to multi-line games (Top, Center, Bottom, Diagonal).
    /// </summary>
    [System.Serializable]
    public class PaylineConfig
    {
        [SerializeField] private string lineName = "Center Payline";
        [SerializeField] private int paylineIndex = 0;
        [Tooltip("The row index evaluated for each reel [Reel0Row, Reel1Row, Reel2Row]")]
        [SerializeField] private int[] rowOffsets = new int[3] { 0, 0, 0 };
        [SerializeField] private Color paylineColor = Color.yellow;

        public string LineName => lineName;
        public int PaylineIndex => paylineIndex;
        public int[] RowOffsets => rowOffsets;
        public Color PaylineColor => paylineColor;

        public PaylineConfig(string name, int index, int[] offsets, Color color)
        {
            lineName = name;
            paylineIndex = index;
            rowOffsets = offsets;
            paylineColor = color;
        }
    }
}
