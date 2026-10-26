/**
 * PayoutManager evaluates landed symbols against winning combinations,
 * wild substitution rules, and free spins bonus triggers.
 */
class PayoutManager {
    constructor() {
        // Multipliers for 3 of a kind
        this.multipliers = {
            0: 100, // Seven (Jackpot)
            1: 40,  // Cherry
            2: 25,  // Bell
            3: 15,  // Bar
            4: 150  // Wild (Super Jackpot)
        };

        this.twoOfAKindMultiplier = 2;
        this.symbolNames = ['Lucky 7', 'Cherries', 'Bell', 'BAR', 'WILD'];
    }

    evaluate(outcome, betAmount, featureMultiplier = 1.0) {
        const [s0, s1, s2] = outcome;
        const result = {
            isWin: false,
            isJackpot: false,
            isFreeSpins: false,
            freeSpinsAwarded: 0,
            payout: 0,
            multiplier: 0,
            description: 'No Match. Try again!',
            winningReels: []
        };

        const wildCount = (s0 === 4 ? 1 : 0) + (s1 === 4 ? 1 : 0) + (s2 === 4 ? 1 : 0);

        // 1. Triple Wilds
        if (wildCount === 3) {
            result.isWin = true;
            result.isJackpot = true;
            result.isFreeSpins = true;
            result.freeSpinsAwarded = 10;
            result.multiplier = this.multipliers[4] * featureMultiplier;
            result.payout = Math.round(betAmount * result.multiplier);
            result.description = `SUPER WILD JACKPOT! 3x WILDS! (+10 Free Spins!)`;
            result.winningReels = [0, 1, 2];
            return result;
        }

        // 2. Regular 3 of a kind with Wild substitution
        const nonWilds = outcome.filter(s => s !== 4);
        const isThreeOfAKind = nonWilds.every(s => s === nonWilds[0]);

        if (isThreeOfAKind) {
            const winSym = nonWilds[0];
            result.isWin = true;
            result.winningReels = [0, 1, 2];
            result.multiplier = this.multipliers[winSym] * featureMultiplier;
            result.payout = Math.round(betAmount * result.multiplier);

            if (winSym === 0) {
                result.isJackpot = true;
                result.description = `MEGA JACKPOT! 3x SEVENS! (${result.payout} Credits)`;
            } else {
                const wildText = wildCount > 0 ? ` (With ${wildCount} Wild!)` : '';
                result.description = `WIN! 3x ${this.symbolNames[winSym].toUpperCase()}${wildText}! (${result.payout} Credits)`;
            }

            if (wildCount === 2) {
                result.isFreeSpins = true;
                result.freeSpinsAwarded = 5;
                result.description += ` + 5 FREE SPINS!`;
            }

            return result;
        }

        // 3. Two of a kind (Pair match)
        const checkPair = (a, b) => (a === 4 || b === 4 || a === b);

        if (checkPair(s0, s1)) {
            const sym = s0 !== 4 ? s0 : s1;
            return this.setPairWin(result, sym, [0, 1], betAmount, featureMultiplier);
        }
        if (checkPair(s1, s2)) {
            const sym = s1 !== 4 ? s1 : s2;
            return this.setPairWin(result, sym, [1, 2], betAmount, featureMultiplier);
        }
        if (checkPair(s0, s2)) {
            const sym = s0 !== 4 ? s0 : s2;
            return this.setPairWin(result, sym, [0, 2], betAmount, featureMultiplier);
        }

        return result;
    }

    setPairWin(result, sym, reels, betAmount, featureMultiplier) {
        result.isWin = true;
        result.multiplier = this.twoOfAKindMultiplier * featureMultiplier;
        result.payout = Math.round(betAmount * result.multiplier);
        result.winningReels = reels;
        result.description = `2x MATCH (${this.symbolNames[sym]})! (${result.payout} Credits)`;
        return result;
    }
}

window.Payout = new PayoutManager();
