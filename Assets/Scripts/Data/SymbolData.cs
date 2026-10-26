using System;
using UnityEngine;

namespace SlotGame.Data
{
    /// <summary>
    /// Represents the immutable configuration and payout properties for a slot symbol.
    /// Can be configured in the Inspector or created dynamically at runtime.
    /// </summary>
    [System.Serializable]
    public class SymbolData
    {
        [Header("Symbol Identity")]
        [SerializeField] private SymbolType symbolType;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite symbolSprite;

        [Header("RNG & Strip Weight")]
        [Tooltip("Relative probability weight when generating random outcomes (higher = more frequent)")]
        [SerializeField] [Range(1, 100)] private int weight = 10;

        [Header("Payout Multipliers")]
        [Tooltip("Multiplier applied to the active bet when 3 matching symbols land on the payline")]
        [SerializeField] private float threeOfAKindMultiplier = 10f;

        [Tooltip("Multiplier applied to the active bet when 2 matching symbols land on the payline")]
        [SerializeField] private float twoOfAKindMultiplier = 2f;

        [Header("Bonus Properties")]
        [SerializeField] private bool isWild = false;
        [SerializeField] private bool isJackpot = false;

        public SymbolType SymbolType => symbolType;
        public string DisplayName => displayName;
        public Sprite SymbolSprite => symbolSprite;
        public int Weight => weight;
        public float ThreeOfAKindMultiplier => threeOfAKindMultiplier;
        public float TwoOfAKindMultiplier => twoOfAKindMultiplier;
        public bool IsWild => isWild;
        public bool IsJackpot => isJackpot;

        public SymbolData() { }

        public SymbolData(SymbolType type, string name, Sprite sprite, int rngWeight, float threeMultiplier, float twoMultiplier, bool wild = false, bool jackpot = false)
        {
            symbolType = type;
            displayName = name;
            symbolSprite = sprite;
            weight = rngWeight;
            threeOfAKindMultiplier = threeMultiplier;
            twoOfAKindMultiplier = twoMultiplier;
            isWild = wild;
            isJackpot = jackpot;
        }
    }
}
