/**
 * RNG Engine providing statistically fair and cryptographically unpredictable outcomes.
 * Implements virtual reel strips identical to the Unity C# RNGManager architecture.
 */
class RNGManager {
    constructor() {
        // Symbol IDs: 0: Seven, 1: Cherry, 2: Bell, 3: Bar, 4: Wild
        this.SYMBOL = {
            SEVEN: 0,
            CHERRY: 1,
            BELL: 2,
            BAR: 3,
            WILD: 4
        };

        this.symbolNames = ['Seven', 'Cherry', 'Bell', 'Bar', 'Wild'];

        // Virtual reel strip layout (24 stops per reel)
        this.virtualStrips = [
            [3, 2, 1, 3, 0, 3, 2, 4, 3, 1, 3, 2, 3, 0, 1, 3, 2, 3, 4, 3, 1, 2, 3, 2],
            [2, 3, 2, 1, 3, 4, 3, 2, 3, 1, 0, 3, 2, 3, 1, 3, 4, 2, 3, 0, 3, 1, 2, 3],
            [3, 0, 1, 3, 2, 3, 4, 3, 1, 2, 3, 2, 3, 2, 1, 3, 0, 3, 2, 4, 3, 1, 3, 2]
        ];

        this.forcedOutcome = null;
    }

    /**
     * Generates a random 32-bit unsigned float [0, 1) using crypto API if available.
     */
    randomFloat() {
        if (window.crypto && window.crypto.getRandomValues) {
            const buf = new Uint32Array(1);
            window.crypto.getRandomValues(buf);
            return buf[0] / (0xFFFFFFFF + 1);
        }
        return Math.random();
    }

    /**
     * Generates standard spin outcome for all 3 reels.
     */
    generateOutcome() {
        if (this.forcedOutcome) {
            const outcome = [...this.forcedOutcome];
            this.forcedOutcome = null; // Single-use debug
            return outcome;
        }

        const outcome = [];
        for (let reel = 0; reel < 3; reel++) {
            const strip = this.virtualStrips[reel];
            const stopIndex = Math.floor(this.randomFloat() * strip.length);
            outcome.push(strip[stopIndex]);
        }
        return outcome;
    }

    /**
     * Allows forcing a specific test outcome for demonstration & evaluation.
     */
    forceOutcome(s0, s1, s2) {
        this.forcedOutcome = [s0, s1, s2];
    }
}

window.RNG = new RNGManager();
