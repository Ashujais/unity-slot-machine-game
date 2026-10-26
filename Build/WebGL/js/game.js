/**
 * Main Slot Game Engine
 * Coordinates asset loading, Canvas 2D/WebGL rendering loop, reel physics,
 * mechanical lever mechanics, UI dispatching, and celebratory particle animations.
 */
class SlotGameApp {
    constructor() {
        this.canvas = document.getElementById('slot-canvas');
        this.ctx = this.canvas.getContext('2d');

        this.confettiCanvas = document.getElementById('confetti-canvas');
        this.confettiCtx = this.confettiCanvas.getContext('2d');

        // Dimensions
        this.width = 1280;
        this.height = 720;
        this.canvas.width = this.width;
        this.canvas.height = this.height;
        this.confettiCanvas.width = this.width;
        this.confettiCanvas.height = this.height;

        // Assets Container
        this.assets = {
            images: {},
            symbols: []
        };

        // Game Economy State
        this.balance = 1000;
        this.bet = 10;
        this.minBet = 10;
        this.maxBet = 100;
        this.betStep = 10;
        this.lastWin = 0;
        this.totalWins = 0;

        // State Machine
        this.state = 'IDLE'; // IDLE, SPINNING, STOPPING, EVALUATING, WIN_CELEBRATION, FREE_SPINS
        this.isAutoSpinning = false;
        this.autoSpinsRemaining = 0;

        // Free Spins Mode
        this.isFreeSpinsActive = false;
        this.freeSpinsRemaining = 0;
        this.freeSpinsMultiplier = 2.0;
        this.freeSpinsAccumulated = 0;

        // Mechanical Lever State
        this.leverDown = false;
        this.leverYOffset = 0;
        this.isDraggingLever = false;
        this.leverStartY = 0;

        // Reels Configuration
        // Slot machine center is at x: 640. Reel box width is ~360px, height ~210px.
        const reelBoxX = 460;
        const reelBoxY = 250;
        const reelWidth = 110;
        const reelHeight = 210;
        const symbolSize = 96;

        this.reels = [
            new window.Reel(0, reelBoxX, reelBoxY, reelWidth, reelHeight, symbolSize),
            new window.Reel(1, reelBoxX + reelWidth + 15, reelBoxY, reelWidth, reelHeight, symbolSize),
            new window.Reel(2, reelBoxX + (reelWidth + 15) * 2, reelBoxY, reelWidth, reelHeight, symbolSize)
        ];

        // Confetti particles
        this.particles = [];

        // Animation timing
        this.lastTime = performance.now();

        this.init();
    }

    async init() {
        this.bindUI();
        await this.loadAssets();
        this.updateHUD();
        this.setStatus('Pull the Lever or press SPIN to play! (Spacebar enabled)');

        // Start 60 FPS Render Loop
        requestAnimationFrame((t) => this.gameLoop(t));
    }

    async loadAssets() {
        const imageFiles = {
            bg: 'assets/sprites/bg_gradient.png',
            machine: 'assets/sprites/slot_machine_body.png',
            leverUp: 'assets/sprites/slot_machine_lever_up.png',
            leverDown: 'assets/sprites/slot_machine_lever_down.png',
            middleBox: 'assets/sprites/slot_machine_middle_box.png',
            popup: 'assets/sprites/popup.png',
            claimBtn: 'assets/sprites/banner_claim.png',
            sym0: 'assets/sprites/symbol_7.png',
            sym1: 'assets/sprites/symbol_cherry.png',
            sym2: 'assets/sprites/symbol_bell.png',
            sym3: 'assets/sprites/symbol_bar.png',
            sym4: 'assets/sprites/symbol_wild.png'
        };

        const loadImg = (src) => new Promise((resolve) => {
            const img = new Image();
            img.onload = () => resolve(img);
            img.onerror = () => {
                console.warn('Asset fallback for', src);
                resolve(null);
            };
            img.src = src;
        });

        for (const [key, path] of Object.entries(imageFiles)) {
            this.assets.images[key] = await loadImg(path);
        }

        this.assets.symbols = [
            this.assets.images.sym0,
            this.assets.images.sym1,
            this.assets.images.sym2,
            this.assets.images.sym3,
            this.assets.images.sym4
        ];
    }

    bindUI() {
        // Buttons
        document.getElementById('btn-spin').addEventListener('click', () => {
            window.AudioManager.playClick();
            this.handleSpinRequest();
        });

        document.getElementById('btn-bet-plus').addEventListener('click', () => {
            window.AudioManager.playClick();
            if (this.bet + this.betStep <= this.maxBet && this.state === 'IDLE') {
                this.bet += this.betStep;
                this.updateHUD();
            }
        });

        document.getElementById('btn-bet-minus').addEventListener('click', () => {
            window.AudioManager.playClick();
            if (this.bet - this.betStep >= this.minBet && this.state === 'IDLE') {
                this.bet -= this.betStep;
                this.updateHUD();
            }
        });

        document.getElementById('btn-max-bet').addEventListener('click', () => {
            window.AudioManager.playClick();
            if (this.state === 'IDLE') {
                this.bet = this.maxBet;
                this.updateHUD();
            }
        });

        document.getElementById('btn-auto-spin').addEventListener('click', () => {
            window.AudioManager.playClick();
            this.toggleAutoSpin();
        });

        // Top bar buttons
        document.getElementById('btn-paytable').addEventListener('click', () => {
            window.AudioManager.playClick();
            this.openPaytable();
        });

        document.getElementById('close-paytable').addEventListener('click', () => {
            window.AudioManager.playClick();
            this.closePaytable();
        });

        document.getElementById('btn-sound').addEventListener('click', () => {
            const isMuted = window.AudioManager.toggleMute();
            document.getElementById('btn-sound').textContent = isMuted ? 'UNMUTE' : 'SOUND ON';
        });

        document.getElementById('btn-fullscreen').addEventListener('click', () => {
            if (!document.fullscreenElement) {
                document.getElementById('game-container').requestFullscreen().catch(() => {});
            } else {
                document.exitFullscreen().catch(() => {});
            }
        });

        // Win popup claim button
        document.getElementById('btn-claim-win').addEventListener('click', () => {
            window.AudioManager.playClick();
            this.closeWinPopup();
        });

        // Mechanical Lever interaction
        const leverHitbox = document.getElementById('lever-hitbox');
        leverHitbox.addEventListener('mousedown', (e) => this.onLeverDown(e.clientY));
        window.addEventListener('mousemove', (e) => this.onLeverMove(e.clientY));
        window.addEventListener('mouseup', () => this.onLeverUp());

        // Touch support for lever
        leverHitbox.addEventListener('touchstart', (e) => {
            if (e.touches.length > 0) this.onLeverDown(e.touches[0].clientY);
        });
        window.addEventListener('touchmove', (e) => {
            if (e.touches.length > 0) this.onLeverMove(e.touches[0].clientY);
        });
        window.addEventListener('touchend', () => this.onLeverUp());

        // Keyboard Shortcut: Spacebar to spin / pull lever
        window.addEventListener('keydown', (e) => {
            if (e.code === 'Space' && !e.repeat) {
                e.preventDefault();
                this.triggerLeverVisualPull();
            }
        });
    }

    onLeverDown(clientY) {
        if (this.state !== 'IDLE' && this.state !== 'FREE_SPINS') return;
        this.isDraggingLever = true;
        this.leverStartY = clientY;
        this.leverDown = true;
        window.AudioManager.playLever();
    }

    onLeverMove(clientY) {
        if (!this.isDraggingLever) return;
        const dy = clientY - this.leverStartY;
        this.leverYOffset = Math.max(0, Math.min(60, dy));
        if (this.leverYOffset >= 40 && !this.leverTriggered) {
            this.leverTriggered = true;
            this.handleSpinRequest();
        }
    }

    onLeverUp() {
        if (!this.isDraggingLever) return;
        this.isDraggingLever = false;
        this.leverTriggered = false;
        this.leverDown = false;
        this.leverYOffset = 0;
    }

    triggerLeverVisualPull() {
        if (this.state !== 'IDLE' && this.state !== 'FREE_SPINS') return;
        this.leverDown = true;
        window.AudioManager.playLever();
        this.handleSpinRequest();
        setTimeout(() => {
            this.leverDown = false;
        }, 220);
    }

    handleSpinRequest() {
        if (this.state !== 'IDLE' && this.state !== 'FREE_SPINS') return;

        const isFree = this.isFreeSpinsActive;

        // Check balance
        if (!isFree && this.balance < this.bet) {
            this.setStatus('Insufficient credits! Lower your bet.');
            if (this.isAutoSpinning) this.stopAutoSpin();
            return;
        }

        // Deduct bet if regular spin
        if (!isFree) {
            this.balance -= this.bet;
        } else {
            this.freeSpinsRemaining--;
            this.updateFreeSpinsBanner();
        }

        this.lastWin = 0;
        this.updateHUD();
        this.setControlsEnabled(false);
        document.getElementById('anticipation-banner').style.display = 'none';

        this.state = 'SPINNING';
        this.setStatus(isFree ? 'Free Spin running!' : 'Reels spinning...');

        // Start reel spin animations and looping sound
        window.AudioManager.playReelSpinLoop();
        this.reels.forEach(r => r.startSpin());

        // Generate RNG Outcome
        const outcome = window.RNG.generateOutcome();

        // Staggered stop sequence
        this.runStaggeredStops(outcome);
    }

    async runStaggeredStops(outcome) {
        // 1. Initial spin duration (1.2s)
        await this.wait(1200);

        // 2. Stop Reel 0
        this.reels[0].stopAt(outcome[0]);

        // 3. Wait stagger interval (0.4s)
        await this.wait(400);
        this.reels[1].stopAt(outcome[1]);

        // 4. Anticipation Check: If Reel 0 & Reel 1 match (or either is Wild), build suspense!
        const r0 = outcome[0];
        const r1 = outcome[1];
        const hasAnticipation = (r0 === 4 || r1 === 4 || r0 === r1);

        if (hasAnticipation) {
            document.getElementById('anticipation-banner').style.display = 'block';
            window.AudioManager.playAnticipation();
            await this.wait(1200); // Dramatic extra spin delay
        } else {
            await this.wait(400);
        }

        // 5. Stop Reel 2
        this.reels[2].stopAt(outcome[2]);

        // Wait for bounce physics to settle
        await this.wait(450);

        window.AudioManager.stopReelSpinLoop();
        document.getElementById('anticipation-banner').style.display = 'none';

        // Evaluate Payout
        this.evaluateOutcome(outcome);
    }

    evaluateOutcome(outcome) {
        this.state = 'EVALUATING';

        const multiplier = this.isFreeSpinsActive ? this.freeSpinsMultiplier : 1.0;
        const result = window.Payout.evaluate(outcome, this.bet, multiplier);

        this.lastWin = result.payout;

        if (result.isWin) {
            this.balance += result.payout;
            this.totalWins += result.payout;
            this.updateHUD(true);
            this.setStatus(result.description);

            // Highlight winning reels
            result.winningReels.forEach(idx => {
                if (this.reels[idx]) this.reels[idx].isWinning = true;
            });

            // Handle Free Spins Award
            if (result.isFreeSpins) {
                this.isFreeSpinsActive = true;
                this.freeSpinsRemaining += result.freeSpinsAwarded;
                this.freeSpinsMultiplier = 2.0;
                this.updateFreeSpinsBanner();
            }

            if (this.isFreeSpinsActive) {
                this.freeSpinsAccumulated += result.payout;
            }

            // Big Win or Jackpot Celebration
            if (result.isJackpot || result.payout >= this.bet * 25) {
                this.triggerCelebration(result);
            } else {
                window.AudioManager.playWinSmall();
                setTimeout(() => this.concludeRound(), 1200);
            }
        } else {
            this.setStatus(result.description);
            setTimeout(() => this.concludeRound(), 500);
        }
    }

    concludeRound() {
        this.reels.forEach(r => r.isWinning = false);

        // Free Spins check
        if (this.isFreeSpinsActive) {
            if (this.freeSpinsRemaining <= 0) {
                this.isFreeSpinsActive = false;
                document.getElementById('free-spins-banner').style.display = 'none';
                this.openWinPopup('BONUS COMPLETE!', this.freeSpinsAccumulated, `Total Bonus Winnings: ${this.freeSpinsAccumulated} Credits!`);
                this.freeSpinsAccumulated = 0;
                this.state = 'IDLE';
                this.setControlsEnabled(true);
                return;
            } else {
                this.state = 'FREE_SPINS';
                setTimeout(() => this.handleSpinRequest(), 800);
                return;
            }
        }

        this.state = 'IDLE';
        this.setControlsEnabled(true);

        // Auto spin continuation
        if (this.isAutoSpinning) {
            if (this.autoSpinsRemaining > 0 && this.balance >= this.bet) {
                this.autoSpinsRemaining--;
                this.updateAutoSpinButton();
                setTimeout(() => this.handleSpinRequest(), 800);
            } else {
                this.stopAutoSpin();
            }
        }
    }

    triggerCelebration(result) {
        this.spawnConfetti();

        if (result.isJackpot) {
            window.AudioManager.playJackpot();
        } else {
            window.AudioManager.playCoins();
        }

        const title = result.isJackpot ? 'MEGA JACKPOT!' : 'BIG WIN!';
        this.openWinPopup(title, result.payout, result.description);
    }

    openWinPopup(title, amount, desc) {
        const modal = document.getElementById('win-popup-modal');
        document.getElementById('popup-title').textContent = title;
        document.getElementById('popup-desc').textContent = desc;

        modal.style.display = 'flex';

        // Animated count-up
        const amountEl = document.getElementById('popup-amount');
        let cur = 0;
        const dur = 1200;
        const start = performance.now();

        const countUp = (now) => {
            const elapsed = now - start;
            const progress = Math.min(1.0, elapsed / dur);
            cur = Math.round(progress * amount);
            amountEl.textContent = `+${cur.toLocaleString()}`;
            if (progress < 1.0) {
                requestAnimationFrame(countUp);
            }
        };
        requestAnimationFrame(countUp);
    }

    closeWinPopup() {
        document.getElementById('win-popup-modal').style.display = 'none';
        this.particles = [];
        this.concludeRound();
    }

    openPaytable() {
        document.getElementById('paytable-modal').style.display = 'flex';
    }

    closePaytable() {
        document.getElementById('paytable-modal').style.display = 'none';
    }

    toggleAutoSpin() {
        if (this.isAutoSpinning) {
            this.stopAutoSpin();
        } else {
            this.isAutoSpinning = true;
            this.autoSpinsRemaining = 10;
            this.updateAutoSpinButton();
            if (this.state === 'IDLE') {
                this.handleSpinRequest();
            }
        }
    }

    stopAutoSpin() {
        this.isAutoSpinning = false;
        this.autoSpinsRemaining = 0;
        this.updateAutoSpinButton();
    }

    updateAutoSpinButton() {
        const btn = document.getElementById('btn-auto-spin');
        if (this.isAutoSpinning) {
            btn.textContent = `STOP (${this.autoSpinsRemaining})`;
            btn.style.borderColor = '#ff4b2b';
        } else {
            btn.textContent = 'AUTO SPIN';
            btn.style.borderColor = '#735ea6';
        }
    }

    updateFreeSpinsBanner() {
        const banner = document.getElementById('free-spins-banner');
        if (this.isFreeSpinsActive) {
            banner.style.display = 'block';
            banner.textContent = `FREE SPINS: ${this.freeSpinsRemaining} LEFT (${this.freeSpinsMultiplier}x MULTIPLIER)`;
        } else {
            banner.style.display = 'none';
        }
    }

    updateHUD(winGlow = false) {
        document.getElementById('display-balance').textContent = this.balance.toLocaleString();
        document.getElementById('display-bet').textContent = this.bet.toString();

        const winEl = document.getElementById('display-win');
        winEl.textContent = this.lastWin > 0 ? `+${this.lastWin.toLocaleString()}` : '0';

        if (winGlow) {
            winEl.classList.add('win-glow');
            setTimeout(() => winEl.classList.remove('win-glow'), 1800);
        }
    }

    setStatus(msg) {
        document.getElementById('status-message').textContent = msg;
    }

    setControlsEnabled(enabled) {
        document.getElementById('btn-spin').disabled = !enabled;
        document.getElementById('btn-bet-plus').disabled = !enabled;
        document.getElementById('btn-bet-minus').disabled = !enabled;
        document.getElementById('btn-max-bet').disabled = !enabled;
    }

    spawnConfetti() {
        this.particles = [];
        const colors = ['#ffd700', '#ff4b2b', '#4efd54', '#3b82f6', '#ff00ea', '#ffffff'];
        for (let i = 0; i < 180; i++) {
            this.particles.push({
                x: this.width / 2 + (Math.random() - 0.5) * 400,
                y: this.height / 2 - 50,
                vx: (Math.random() - 0.5) * 16,
                vy: -Math.random() * 18 - 4,
                size: Math.random() * 8 + 4,
                color: colors[Math.floor(Math.random() * colors.length)],
                rotation: Math.random() * 360,
                rotSpeed: (Math.random() - 0.5) * 15,
                gravity: 0.45,
                life: 1.0,
                decay: Math.random() * 0.015 + 0.008
            });
        }
    }

    updateAndRenderConfetti(dt) {
        this.confettiCtx.clearRect(0, 0, this.width, this.height);
        if (this.particles.length === 0) return;

        for (let i = this.particles.length - 1; i >= 0; i--) {
            const p = this.particles[i];
            p.x += p.vx;
            p.y += p.vy;
            p.vy += p.gravity;
            p.rotation += p.rotSpeed;
            p.life -= p.decay;

            if (p.life <= 0 || p.y > this.height) {
                this.particles.splice(i, 1);
                continue;
            }

            this.confettiCtx.save();
            this.confettiCtx.globalAlpha = p.life;
            this.confettiCtx.translate(p.x, p.y);
            this.confettiCtx.rotate((p.rotation * Math.PI) / 180);
            this.confettiCtx.fillStyle = p.color;
            this.confettiCtx.fillRect(-p.size / 2, -p.size / 2, p.size, p.size * 1.5);
            this.confettiCtx.restore();
        }
    }

    gameLoop(now) {
        const dt = Math.min(0.1, (now - this.lastTime) / 1000);
        this.lastTime = now;

        // 1. Update Physics
        this.reels.forEach(r => r.update(dt));

        // 2. Clear Screen
        this.ctx.clearRect(0, 0, this.width, this.height);

        // 3. Render Background
        if (this.assets.images.bg) {
            this.ctx.drawImage(this.assets.images.bg, 0, 0, this.width, this.height);
        }

        // 4. Render Slot Machine Base
        const machineX = (this.width - 816) / 2;
        const machineY = 40;
        if (this.assets.images.machine) {
            this.ctx.drawImage(this.assets.images.machine, machineX, machineY, 816, 624);
        }

        // 5. Render Reels (clipped to viewport)
        this.reels.forEach(r => r.render(this.ctx, this.assets.symbols));

        // 6. Render Middle Box Frame over the reels
        if (this.assets.images.middleBox) {
            this.ctx.drawImage(this.assets.images.middleBox, 311, 218, 658, 277);
        }

        // 7. Render Payline Center Marker
        this.ctx.save();
        this.ctx.strokeStyle = 'rgba(255, 215, 0, 0.35)';
        this.ctx.lineWidth = 2;
        this.ctx.setLineDash([8, 6]);
        this.ctx.beginPath();
        this.ctx.moveTo(450, 355);
        this.ctx.lineTo(830, 355);
        this.ctx.stroke();
        this.ctx.restore();

        // 8. Render Lever (Up or Down depending on state)
        const leverImg = (this.leverDown || this.isDraggingLever)
            ? this.assets.images.leverDown
            : this.assets.images.leverUp;

        if (leverImg) {
            this.ctx.drawImage(leverImg, machineX, machineY + this.leverYOffset, 816, 624);
        }

        // 9. Confetti particles overlay
        this.updateAndRenderConfetti(dt);

        requestAnimationFrame((t) => this.gameLoop(t));
    }

    wait(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }
}

// Global initialization
window.addEventListener('DOMContentLoaded', () => {
    window.GameApp = new SlotGameApp();
});
