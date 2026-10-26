/**
 * Reel represents an individual mechanical reel with continuous symbol wrap-around,
 * realistic acceleration, motion blur, and elastic overshoot bounce-back.
 */
class Reel {
    constructor(index, x, y, width, height, symbolSize) {
        this.index = index;
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
        this.symbolSize = symbolSize; // 96px

        this.spacing = 14;
        this.totalSlotHeight = this.symbolSize + this.spacing;

        // Reel state
        this.isSpinning = false;
        this.isStopping = false;
        this.speed = 0;
        this.maxSpeed = 1600; // px/sec
        this.scrollOffset = 0;
        this.bounceOffset = 0;

        // Anticipation kickback & bounce states
        this.kickbackTimer = 0;
        this.bounceTimer = 0;
        this.isBouncing = false;

        this.targetSymbol = 3;
        this.symbols = [0, 1, 2, 3, 4]; // Initial symbols in the reel strip
        this.centerSymbol = 3;

        this.isWinning = false;
        this.winPulseTimer = 0;
    }

    startSpin() {
        this.isSpinning = true;
        this.isStopping = false;
        this.isBouncing = false;
        this.isWinning = false;
        this.speed = 0;
        this.kickbackTimer = 0.16; // 0.16s kickback
        this.bounceOffset = 0;
    }

    stopAt(targetSymbol) {
        this.targetSymbol = targetSymbol;
        this.isStopping = true;
    }

    update(dt) {
        if (!this.isSpinning && !this.isBouncing) {
            if (this.isWinning) {
                this.winPulseTimer += dt * 5;
            }
            return;
        }

        // 1. Kickback Anticipation (slight upward move)
        if (this.kickbackTimer > 0) {
            this.kickbackTimer -= dt;
            this.bounceOffset = -Math.sin((0.16 - this.kickbackTimer) / 0.16 * Math.PI) * 20;
            return;
        }

        // 2. Acceleration / Spinning
        if (!this.isStopping) {
            this.bounceOffset = 0;
            this.speed = Math.min(this.maxSpeed, this.speed + 3500 * dt);
            this.scrollOffset += this.speed * dt;
        } else {
            // Deceleration
            if (!this.isBouncing) {
                this.speed = Math.max(600, this.speed - 2400 * dt);
                this.scrollOffset += this.speed * dt;

                // Stop condition reached
                if (this.speed <= 650) {
                    this.isSpinning = false;
                    this.isBouncing = true;
                    this.bounceTimer = 0.28;
                    this.centerSymbol = this.targetSymbol;
                    // Snap layout
                    this.symbols = [
                        (this.targetSymbol + 4) % 5,
                        this.targetSymbol,
                        (this.targetSymbol + 1) % 5
                    ];
                    this.scrollOffset = 0;

                    if (window.AudioManager) {
                        window.AudioManager.playReelStop();
                    }
                }
            }
        }

        // 3. Elastic Bounce-back physics (EaseOutBack)
        if (this.isBouncing) {
            this.bounceTimer -= dt;
            const progress = 1.0 - Math.max(0, this.bounceTimer / 0.28);
            if (progress >= 1.0) {
                this.isBouncing = false;
                this.bounceOffset = 0;
            } else {
                // Physical spring formula: overshoot then snap back
                const p = progress;
                const c1 = 1.70158;
                const c3 = c1 + 1;
                const overshoot = Math.sin(p * Math.PI) * (1 - p) * 26;
                this.bounceOffset = overshoot;
            }
        }

        // Symbol cycling
        while (this.scrollOffset >= this.totalSlotHeight) {
            this.scrollOffset -= this.totalSlotHeight;
            // Shift symbols and push random new
            this.symbols.pop();
            const randSym = Math.floor(Math.random() * 5);
            this.symbols.unshift(randSym);
        }
    }

    render(ctx, symbolImages) {
        ctx.save();

        // 1. Clip to Reel Viewport
        ctx.beginPath();
        ctx.rect(this.x, this.y, this.width, this.height);
        ctx.clip();

        const centerY = this.y + this.height / 2;

        if (this.isSpinning) {
            // Render fast cycling symbols with vertical motion blur effect
            const drawCount = 5;
            for (let i = -1; i < drawCount; i++) {
                const sym = this.symbols[(i + 10) % this.symbols.length];
                const img = symbolImages[sym];
                if (!img) continue;

                const symY = this.y + i * this.totalSlotHeight + (this.scrollOffset % this.totalSlotHeight);
                const symX = this.x + (this.width - this.symbolSize) / 2;

                ctx.save();
                // Motion blur when speed > 1000
                if (this.speed > 1000) {
                    ctx.globalAlpha = 0.85;
                    ctx.drawImage(img, symX, symY - 10, this.symbolSize, this.symbolSize + 20);
                } else {
                    ctx.drawImage(img, symX, symY, this.symbolSize, this.symbolSize);
                }
                ctx.restore();
            }
        } else {
            // Render resting / bouncing 3-symbol grid (Top, Center, Bottom)
            const offsets = [-this.totalSlotHeight, 0, this.totalSlotHeight];
            const symList = [
                (this.centerSymbol + 4) % 5,
                this.centerSymbol,
                (this.centerSymbol + 1) % 5
            ];

            for (let i = 0; i < 3; i++) {
                const sym = symList[i];
                const img = symbolImages[sym];
                if (!img) continue;

                let symY = centerY - this.symbolSize / 2 + offsets[i] + this.bounceOffset;
                let symX = this.x + (this.width - this.symbolSize) / 2;
                let scale = 1.0;

                // Center symbol winning glow / pulse
                if (i === 1 && this.isWinning) {
                    scale = 1.0 + Math.sin(this.winPulseTimer) * 0.12;
                    ctx.save();
                    ctx.shadowColor = '#ffe600';
                    ctx.shadowBlur = 20;
                    const w = this.symbolSize * scale;
                    const h = this.symbolSize * scale;
                    ctx.drawImage(img, symX - (w - this.symbolSize) / 2, symY - (h - this.symbolSize) / 2, w, h);
                    ctx.restore();
                } else {
                    ctx.drawImage(img, symX, symY, this.symbolSize, this.symbolSize);
                }
            }
        }

        ctx.restore();
    }
}

window.Reel = Reel;
