/**
 * AudioManager handles sound effects, mechanical audio loops, and celebratory fanfares.
 * Utilizes pre-rendered WAV assets with Web Audio API context for zero latency.
 */
class SoundEngine {
    constructor() {
        this.ctx = null;
        this.isMuted = false;
        this.volume = 1.0;
        this.audioClips = {};
        this.spinLoopAudio = null;

        this.soundPaths = {
            lever: 'assets/audio/lever_pull.wav',
            spin: 'assets/audio/reel_spin.wav',
            stop: 'assets/audio/reel_stop.wav',
            anticipation: 'assets/audio/anticipation.wav',
            winSmall: 'assets/audio/win_small.wav',
            jackpot: 'assets/audio/win_jackpot.wav',
            coins: 'assets/audio/coin_payout.wav',
            click: 'assets/audio/button_click.wav'
        };

        this.init();
    }

    init() {
        // Pre-create Audio elements
        for (const [key, path] of Object.entries(this.soundPaths)) {
            const audio = new Audio(path);
            audio.preload = 'auto';
            this.audioClips[key] = audio;
        }

        // Special loop setup for reel spin
        if (this.audioClips.spin) {
            this.spinLoopAudio = this.audioClips.spin;
            this.spinLoopAudio.loop = true;
        }

        // Unlock audio context on first user interaction
        const unlockAudio = () => {
            if (!this.ctx) {
                const AudioCtx = window.AudioContext || window.webkitAudioContext;
                if (AudioCtx) this.ctx = new AudioCtx();
            }
            if (this.ctx && this.ctx.state === 'suspended') {
                this.ctx.resume();
            }
            window.removeEventListener('pointerdown', unlockAudio);
            window.removeEventListener('keydown', unlockAudio);
        };
        window.addEventListener('pointerdown', unlockAudio);
        window.addEventListener('keydown', unlockAudio);
    }

    play(name, volume = 1.0) {
        if (this.isMuted) return;

        const clip = this.audioClips[name];
        if (clip) {
            // Clone or reset to allow rapid overlapping triggers
            const sound = clip.cloneNode();
            sound.volume = Math.min(1.0, Math.max(0.0, volume * this.volume));
            sound.play().catch(e => {
                // Audio autoplay policy fallback (silent catch)
            });
        }
    }

    playLever() { this.play('lever', 0.9); }
    playReelStop() { this.play('stop', 0.85); }
    playAnticipation() { this.play('anticipation', 1.0); }
    playWinSmall() { this.play('winSmall', 0.9); }
    playJackpot() { this.play('jackpot', 1.0); }
    playCoins() { this.play('coins', 0.85); }
    playClick() { this.play('click', 0.7); }

    playReelSpinLoop() {
        if (this.isMuted || !this.spinLoopAudio) return;
        this.spinLoopAudio.currentTime = 0;
        this.spinLoopAudio.volume = 0.65 * this.volume;
        this.spinLoopAudio.play().catch(() => {});
    }

    stopReelSpinLoop() {
        if (this.spinLoopAudio) {
            this.spinLoopAudio.pause();
            this.spinLoopAudio.currentTime = 0;
        }
    }

    toggleMute() {
        this.isMuted = !this.isMuted;
        if (this.isMuted) {
            this.stopReelSpinLoop();
        }
        return this.isMuted;
    }
}

window.AudioManager = new SoundEngine();
