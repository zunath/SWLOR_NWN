#!/usr/bin/env python3
"""Synthesize the voice set (soundset) for a small utility robot player species.

Every sound is generated from maths alone (sine and FM oscillators, a one-pole
noise filter, and pitch curves); no sample from any film, game or library is
used, and no phrase imitates a specific character. The tool writes one 22050 Hz
mono 16-bit WAV per filled SSF entry, the binary `<prefix>.ssf` that maps the 49
standard voice events to those sounds, and a `manifest.json` describing them.
Output is deterministic: two runs produce byte-identical files.

Usage:
    python -B tools/GenerateAstromechSoundset.py --output EMPTY_DIR [--prefix vs_astromec]

The prefix plus the longest event suffix must fit the 16 character resref limit.
A prefix of 11 characters or fewer also gets the in-game soundset preview for
free (the server plays `<resref>_bat1`). The sounds are plain PCM. The HAK
sound compressor (SWLOR_Haks/tools/CompressSoundResources.py) needs ffmpeg with
libmp3lame and is run over the finished HAK, not by this tool.

Tuning: every event is a row in PHRASES, a list of (primitive, parameters) steps.
Change numbers there; the synthesis primitives below it stay untouched.
"""

import argparse
import hashlib
import io
import json
import math
import random
import struct
import sys
import wave
from pathlib import Path

SAMPLE_RATE = 22050
PEAK_DBFS = -3.0
SEED = 20261115
MIN_SECONDS = 0.3
MAX_SECONDS = 1.6
RESREF_LIMIT = 16
DEFAULT_PREFIX = "vs_astromec"

SSF_ENTRY_COUNT = 49
SSF_HEADER_SIZE = 40
SSF_ENTRY_SIZE = 20
SSF_NO_STRREF = 0xFFFFFFFF

# Standard SSF order with the suffix style used by the existing vs_* sets.
EVENTS = (
    "attk", "bat1", "bat2", "bat3", "heal", "help", "enmy", "flee", "tant", "gard",
    "hold", "atk1", "atk2", "atk3", "hit1", "hit2", "hit3", "dyin", "dead", "sick",
    "spel", "inff", "foll", "look", "grup", "move", "pick", "srch", "hide", "can",
    "cant", "done", "encm", "slct", "hi", "yes", "no", "stop", "rest", "bore",
    "bye", "thnx", "haha", "cuss", "vict", "say", "good", "bad", "warn",
)

# Event suffix -> ordered synthesis steps. Parameters are the keyword arguments
# of the primitive of the same name; every step also accepts gain.
PHRASES = {
    "attk": [("glide", dict(f0=500, f1=1800, dur=0.12, bright=0.4)),
             ("noise", dict(dur=0.05, lo=300, hi=3500, gain=0.5)),
             ("glide", dict(f0=1800, f1=900, dur=0.10, bright=0.4)),
             ("glide", dict(f0=900, f1=2400, dur=0.18, bright=0.5))],
    "bat1": [("warble", dict(fc=700, f_end=1500, dur=0.5, index=4, index_end=9)),
             ("trill", dict(fa=1500, fb=1100, rate=18, dur=0.3, bright=0.3))],
    "bat2": [("grumble", dict(f=110, dur=0.25, am=14)),
             ("glide", dict(f0=400, f1=2200, dur=0.3, bright=0.5)),
             ("trill", dict(fa=2200, fb=1800, rate=22, dur=0.25))],
    "bat3": [("glide", dict(f0=900, f1=900, dur=0.1, bright=0.5)), ("rest", dict(dur=0.04)),
             ("glide", dict(f0=1200, f1=1200, dur=0.1, bright=0.5)), ("rest", dict(dur=0.04)),
             ("glide", dict(f0=1600, f1=1600, dur=0.1, bright=0.5)), ("rest", dict(dur=0.04)),
             ("whistle", dict(f=2400, dur=0.4, vib_rate=11, vib_depth=1.2, f_end=3000))],
    "heal": [("whistle", dict(f=1500, dur=0.3, vib_rate=7, vib_depth=1.5, f_end=1000)),
             ("glide", dict(f0=1000, f1=800, dur=0.1)),
             ("whistle", dict(f=1300, dur=0.25, vib_rate=7, vib_depth=2.0, f_end=700))],
    "help": [("trill", dict(fa=1800, fb=2600, rate=14, dur=0.4, bright=0.3)),
             ("whistle", dict(f=2800, dur=0.55, vib_rate=9, vib_depth=1.8, f_end=1600))],
    "enmy": [("glide", dict(f0=2000, f1=2000, dur=0.09, bright=0.6)), ("rest", dict(dur=0.06)),
             ("glide", dict(f0=2000, f1=2000, dur=0.09, bright=0.6)), ("rest", dict(dur=0.06)),
             ("glide", dict(f0=2000, f1=2000, dur=0.09, bright=0.6)), ("rest", dict(dur=0.06)),
             ("warble", dict(fc=600, f_end=450, dur=0.3, index=5, index_end=7))],
    "flee": [("glide", dict(f0=900, f1=3200, dur=0.15, bright=0.3)),
             ("glide", dict(f0=1200, f1=3400, dur=0.15, bright=0.3)),
             ("trill", dict(fa=3000, fb=1500, rate=26, dur=0.3)),
             ("glide", dict(f0=3200, f1=900, dur=0.2))],
    "tant": [("whistle", dict(f=1400, dur=0.3, vib_rate=6, vib_depth=2.0)),
             ("glide", dict(f0=1400, f1=700, dur=0.2, bright=0.3)),
             ("noise", dict(dur=0.18, lo=200, hi=1800, gain=0.6)),
             ("trill", dict(fa=700, fb=900, rate=8, dur=0.25))],
    "gard": [("glide", dict(f0=700, f1=1100, dur=0.12, bright=0.4)), ("rest", dict(dur=0.05)),
             ("glide", dict(f0=1100, f1=1100, dur=0.25, bright=0.5)),
             ("trill", dict(fa=1100, fb=1500, rate=12, dur=0.2))],
    "hold": [("glide", dict(f0=1000, f1=1000, dur=0.35, bright=0.5)),
             ("glide", dict(f0=1000, f1=700, dur=0.12))],
    "atk1": [("glide", dict(f0=600, f1=900, dur=0.12, bright=0.5)),
             ("noise", dict(dur=0.06, lo=400, hi=3000)),
             ("glide", dict(f0=1500, f1=500, dur=0.15, bright=0.4))],
    "atk2": [("noise", dict(dur=0.07, lo=300, hi=2500)),
             ("warble", dict(fc=1100, f_end=1900, dur=0.18, index=5, index_end=3)),
             ("glide", dict(f0=1900, f1=800, dur=0.1))],
    "atk3": [("grumble", dict(f=140, dur=0.14, am=20)),
             ("glide", dict(f0=700, f1=2600, dur=0.14, bright=0.6)),
             ("noise", dict(dur=0.08, lo=800, hi=4000, gain=0.6))],
    "hit1": [("warble", dict(fc=1800, f_end=500, dur=0.3, index=6, index_end=10)),
             ("noise", dict(dur=0.08, lo=300, hi=2500, gain=0.6))],
    "hit2": [("glide", dict(f0=2400, f1=300, dur=0.25, bright=0.4)),
             ("trill", dict(fa=600, fb=500, rate=28, dur=0.12))],
    "hit3": [("power_down", dict(f0=1600, f1=400, dur=0.35, stutter=14))],
    "dyin": [("whistle", dict(f=900, dur=0.6, vib_rate=5, vib_depth=3.0, f_end=500)),
             ("power_down", dict(f0=520, f1=250, dur=0.3, stutter=22, gain=0.7))],
    "dead": [("glide", dict(f0=1800, f1=2400, dur=0.1)),
             ("power_down", dict(f0=2400, f1=60, dur=1.1, stutter=34)),
             ("noise", dict(dur=0.2, lo=60, hi=500, gain=0.35))],
    "sick": [("warble", dict(fc=800, f_end=600, dur=0.35, index=7, index_end=10)),
             ("trill", dict(fa=600, fb=520, rate=9, dur=0.3))],
    "spel": [("glide", dict(f0=1600, f1=700, dur=0.14, bright=0.4)),
             ("glide", dict(f0=900, f1=450, dur=0.2)),
             ("noise", dict(dur=0.1, lo=500, hi=2500, gain=0.5))],
    "inff": [("glide", dict(f0=1200, f1=1200, dur=0.1, bright=0.5)), ("rest", dict(dur=0.05)),
             ("glide", dict(f0=1200, f1=900, dur=0.1, bright=0.5)),
             ("glide", dict(f0=800, f1=800, dur=0.14))],
    "foll": [("glide", dict(f0=900, f1=1300, dur=0.1, bright=0.4)),
             ("glide", dict(f0=1300, f1=1800, dur=0.12, bright=0.4)), ("rest", dict(dur=0.04)),
             ("whistle", dict(f=1800, dur=0.14, vib_rate=8, vib_depth=0.8, f_end=2200))],
    "look": [("trill", dict(fa=1600, fb=2000, rate=10, dur=0.2)),
             ("glide", dict(f0=1200, f1=2600, dur=0.25, bright=0.3))],
    "grup": [("glide", dict(f0=1000, f1=1400, dur=0.12, bright=0.4)), ("rest", dict(dur=0.04)),
             ("glide", dict(f0=1400, f1=1900, dur=0.12, bright=0.4)), ("rest", dict(dur=0.04)),
             ("trill", dict(fa=1900, fb=2300, rate=16, dur=0.2))],
    "move": [("glide", dict(f0=700, f1=500, dur=0.1)), ("glide", dict(f0=900, f1=600, dur=0.12)),
             ("noise", dict(dur=0.1, lo=300, hi=1500, gain=0.4))],
    "pick": [("noise", dict(dur=0.04, lo=2000, hi=5000, gain=0.7)), ("rest", dict(dur=0.05)),
             ("noise", dict(dur=0.04, lo=2000, hi=5000, gain=0.7)), ("rest", dict(dur=0.05)),
             ("trill", dict(fa=2400, fb=2800, rate=30, dur=0.15))],
    "srch": [("whistle", dict(f=1300, dur=0.2, vib_rate=4, vib_depth=1.0, f_end=1700)),
             ("whistle", dict(f=1700, dur=0.2, vib_rate=4, vib_depth=1.0, f_end=1300))],
    "hide": [("glide", dict(f0=1200, f1=500, dur=0.2)), ("whistle", dict(f=500, dur=0.2, vib_rate=3, vib_depth=0.5))],
    "can": [("glide", dict(f0=800, f1=1600, dur=0.12, bright=0.4)), ("rest", dict(dur=0.03)),
            ("glide", dict(f0=1000, f1=2000, dur=0.15, bright=0.4))],
    "cant": [("glide", dict(f0=900, f1=500, dur=0.2, bright=0.4)),
             ("grumble", dict(f=150, dur=0.25, am=9)),
             ("glide", dict(f0=600, f1=350, dur=0.2))],
    "done": [("glide", dict(f0=1000, f1=1000, dur=0.1, bright=0.4)), ("rest", dict(dur=0.03)),
             ("glide", dict(f0=1500, f1=1500, dur=0.1, bright=0.4)), ("rest", dict(dur=0.03)),
             ("glide", dict(f0=2000, f1=2000, dur=0.12, bright=0.4)),
             ("trill", dict(fa=2000, fb=2500, rate=20, dur=0.3))],
    "encm": [("grumble", dict(f=95, dur=0.3, am=6)), ("glide", dict(f0=700, f1=400, dur=0.25)),
             ("grumble", dict(f=85, dur=0.2, am=8))],
    "slct": [("glide", dict(f0=1200, f1=1800, dur=0.1, bright=0.3)), ("rest", dict(dur=0.03)),
             ("glide", dict(f0=1800, f1=1500, dur=0.1, bright=0.3)), ("rest", dict(dur=0.03)),
             ("glide", dict(f0=1500, f1=2100, dur=0.1, bright=0.3))],
    "hi": [("glide", dict(f0=700, f1=2000, dur=0.25, bright=0.3)),
           ("trill", dict(fa=2000, fb=2400, rate=14, dur=0.25)),
           ("glide", dict(f0=2400, f1=1600, dur=0.15))],
    "yes": [("glide", dict(f0=1000, f1=1400, dur=0.14, bright=0.4)),
            ("glide", dict(f0=1400, f1=1900, dur=0.2, bright=0.4)),
            ("trill", dict(fa=1900, fb=2200, rate=24, dur=0.1))],
    "no": [("glide", dict(f0=1100, f1=700, dur=0.15, bright=0.6)), ("rest", dict(dur=0.04)),
           ("glide", dict(f0=800, f1=450, dur=0.25, bright=0.6))],
    "stop": [("glide", dict(f0=2200, f1=2200, dur=0.25, bright=0.7)),
             ("glide", dict(f0=2200, f1=1100, dur=0.2, bright=0.7))],
    "rest": [("whistle", dict(f=900, dur=0.5, vib_rate=3, vib_depth=0.8, f_end=600)),
             ("glide", dict(f0=600, f1=500, dur=0.2))],
    "bore": [("glide", dict(f0=800, f1=700, dur=0.2)), ("glide", dict(f0=700, f1=900, dur=0.2)),
             ("grumble", dict(f=120, dur=0.25, am=4))],
    "bye": [("glide", dict(f0=2000, f1=1400, dur=0.15, bright=0.3)),
            ("glide", dict(f0=1700, f1=1100, dur=0.15, bright=0.3)),
            ("whistle", dict(f=1300, dur=0.4, vib_rate=6, vib_depth=1.2, f_end=700))],
    "thnx": [("trill", dict(fa=1500, fb=1900, rate=12, dur=0.25)),
             ("glide", dict(f0=1900, f1=2400, dur=0.15, bright=0.3)),
             ("whistle", dict(f=2400, dur=0.25, vib_rate=7, vib_depth=1.0, f_end=2000))],
    "haha": [("glide", dict(f0=900, f1=1500, dur=0.07, bright=0.4)), ("rest", dict(dur=0.05)),
             ("glide", dict(f0=950, f1=1550, dur=0.07, bright=0.4)), ("rest", dict(dur=0.05)),
             ("glide", dict(f0=850, f1=1400, dur=0.07, bright=0.4)), ("rest", dict(dur=0.05)),
             ("glide", dict(f0=800, f1=1300, dur=0.07, bright=0.4)), ("rest", dict(dur=0.05)),
             ("glide", dict(f0=700, f1=1100, dur=0.09, bright=0.4)),
             ("trill", dict(fa=1100, fb=1500, rate=22, dur=0.15))],
    "cuss": [("grumble", dict(f=90, dur=0.3, am=18)), ("noise", dict(dur=0.1, lo=200, hi=2500, gain=0.7)),
             ("trill", dict(fa=300, fb=250, rate=25, dur=0.2)),
             ("warble", dict(fc=500, f_end=300, dur=0.25, index=10, index_end=14))],
    "vict": [("glide", dict(f0=800, f1=2500, dur=0.2, bright=0.3)),
             ("trill", dict(fa=2500, fb=3000, rate=20, dur=0.3)),
             ("whistle", dict(f=3000, dur=0.35, vib_rate=10, vib_depth=1.0, f_end=3400))],
    "say": [("whistle", dict(f=1400, dur=0.2, vib_rate=6, vib_depth=0.8, f_end=1800)),
            ("glide", dict(f0=1800, f1=1800, dur=0.1, bright=0.4)),
            ("glide", dict(f0=1500, f1=2100, dur=0.15, bright=0.4))],
    "good": [("glide", dict(f0=1100, f1=1100, dur=0.1, bright=0.4)), ("rest", dict(dur=0.03)),
             ("glide", dict(f0=1400, f1=2200, dur=0.2, bright=0.4)),
             ("trill", dict(fa=2200, fb=2600, rate=16, dur=0.15))],
    "bad": [("glide", dict(f0=800, f1=600, dur=0.15, bright=0.5)),
            ("grumble", dict(f=130, dur=0.2, am=10)),
            ("glide", dict(f0=500, f1=400, dur=0.15))],
    "warn": [("grumble", dict(f=70, dur=0.4, am=16)),
             ("warble", dict(fc=300, f_end=600, dur=0.3, index=8, index_end=12)),
             ("noise", dict(dur=0.1, lo=100, hi=1200, gain=0.6))],
}

# Entries that must always carry a sound (the player-facing voice events).
REQUIRED_SUFFIXES = (
    "attk", "bat1", "bat2", "bat3", "help", "enmy", "flee", "tant", "atk1", "atk2", "atk3",
    "hit1", "hit2", "hit3", "dyin", "dead", "can", "cant", "done", "slct", "hi", "yes", "no",
    "stop", "bye", "thnx", "haha", "cuss", "vict", "warn", "foll", "look",
)


def _count(seconds):
    return max(1, int(round(seconds * SAMPLE_RATE)))


def _envelope(count, attack=0.006, release=0.02):
    """Raised-cosine attack and release so each step starts and ends at zero."""
    attack_n = min(count // 2, max(1, _count(attack)))
    release_n = min(count // 2, max(1, _count(release)))
    env = [1.0] * count
    for i in range(attack_n):
        env[i] = 0.5 - 0.5 * math.cos(math.pi * i / attack_n)
    for i in range(release_n):
        env[count - 1 - i] = min(env[count - 1 - i], 0.5 - 0.5 * math.cos(math.pi * i / release_n))
    return env


def _oscillate(freqs, bright=0.2, mod_ratio=0.0, index=None):
    """Phase-accumulating oscillator with optional harmonics and FM."""
    out = []
    phase = 0.0
    mod_phase = 0.0
    norm = 1.0 + 1.5 * bright
    for i, freq in enumerate(freqs):
        step = 2.0 * math.pi * freq / SAMPLE_RATE
        phase += step
        mod = 0.0
        if index is not None:
            mod_phase += step * mod_ratio
            mod = index[i] * math.sin(mod_phase)
        p = phase + mod
        out.append((math.sin(p) + bright * math.sin(2.0 * p) + 0.5 * bright * math.sin(3.0 * p)) / norm)
    return out


def _shape(samples, gain, attack=0.006, release=0.02):
    env = _envelope(len(samples), attack, release)
    return [s * e * gain for s, e in zip(samples, env)]


def _curve(f0, f1, count, power=1.0):
    ratio = f1 / f0
    return [f0 * ratio ** ((i / max(1, count - 1)) ** power) for i in range(count)]


def rest(dur, rng=None, gain=1.0):
    """Silence."""
    return [0.0] * _count(dur)


def glide(f0, f1, dur, bright=0.2, curve=1.0, rng=None, gain=1.0):
    """Exponential pitch glide from f0 to f1 (equal pitches give a plain beep)."""
    count = _count(dur)
    return _shape(_oscillate(_curve(f0, f1, count, curve), bright), gain)


def trill(fa, fb, rate, dur, bright=0.2, rng=None, gain=1.0):
    """Rapid smoothed alternation between two notes."""
    count = _count(dur)
    freqs = []
    for i in range(count):
        mix = 0.5 + 0.5 * math.tanh(3.0 * math.sin(2.0 * math.pi * rate * i / SAMPLE_RATE))
        freqs.append(fa * (fb / fa) ** mix)
    return _shape(_oscillate(freqs, bright), gain)


def whistle(f, dur, vib_rate=6.0, vib_depth=1.0, f_end=None, rng=None, gain=1.0):
    """Pure tone with vibrato (depth in semitones), optionally gliding to f_end."""
    count = _count(dur)
    base = _curve(f, f_end if f_end else f, count)
    freqs = [b * 2.0 ** (vib_depth * math.sin(2.0 * math.pi * vib_rate * i / SAMPLE_RATE) / 12.0)
             for i, b in enumerate(base)]
    return _shape(_oscillate(freqs, 0.05), gain, attack=0.02)


def warble(fc, dur, index=5.0, index_end=None, f_end=None, mod_ratio=1.5, rng=None, gain=1.0):
    """FM tone whose modulation depth sweeps, giving a gargling electronic timbre."""
    count = _count(dur)
    end_index = index if index_end is None else index_end
    freqs = _curve(fc, f_end if f_end else fc, count)
    indexes = [index + (end_index - index) * i / max(1, count - 1) for i in range(count)]
    return _shape(_oscillate(freqs, 0.0, mod_ratio, indexes), gain)


def power_down(f0, f1, dur, stutter=16.0, rng=None, gain=1.0):
    """Falling pitch with a widening wobble and a fading, stuttering amplitude."""
    count = _count(dur)
    base = _curve(f0, f1, count, 0.7)
    freqs = []
    amps = []
    for i in range(count):
        t = i / max(1, count - 1)
        wobble = 1.0 + 0.04 * t * math.sin(2.0 * math.pi * (6.0 + 14.0 * t) * i / SAMPLE_RATE)
        freqs.append(base[i] * wobble)
        gate = 0.575 + 0.425 * math.tanh(4.0 * math.sin(2.0 * math.pi * stutter * t * t * dur))
        amps.append((1.0 - t) ** 0.6 * (0.3 + 0.7 * gate * t + 0.7 * (1.0 - t)))
    indexes = [4.0 * (1.0 - i / max(1, count - 1)) for i in range(count)]
    tone = _oscillate(freqs, 0.25, 1.0, indexes)
    return _shape([s * a for s, a in zip(tone, amps)], gain, release=0.04)


def noise(dur, lo=300.0, hi=3000.0, rng=None, gain=1.0):
    """Band-limited noise burst with a fast decay (difference of two one-pole filters)."""
    count = _count(dur)
    a_hi = 1.0 - math.exp(-2.0 * math.pi * hi / SAMPLE_RATE)
    a_lo = 1.0 - math.exp(-2.0 * math.pi * lo / SAMPLE_RATE)
    low = high = 0.0
    out = []
    for i in range(count):
        x = rng.uniform(-1.0, 1.0)
        high += a_hi * (x - high)
        low += a_lo * (x - low)
        out.append((high - low) * math.exp(-3.0 * i / count) * 2.5)
    return _shape(out, gain, attack=0.003)


def grumble(f, dur, am=12.0, rng=None, gain=1.0):
    """Low filtered sawtooth with slow amplitude flutter."""
    count = _count(dur)
    cutoff = 1.0 - math.exp(-2.0 * math.pi * f * 5.0 / SAMPLE_RATE)
    phase = 0.0
    smooth = 0.0
    out = []
    for i in range(count):
        t = i / SAMPLE_RATE
        phase = (phase + f * (1.0 + 0.03 * math.sin(2.0 * math.pi * 5.0 * t)) / SAMPLE_RATE) % 1.0
        smooth += cutoff * ((2.0 * phase - 1.0) - smooth)
        out.append(smooth * (0.65 + 0.35 * math.sin(2.0 * math.pi * am * t)))
    return _shape(out, gain, attack=0.015, release=0.04)


PRIMITIVES = {
    "rest": rest, "glide": glide, "trill": trill, "whistle": whistle, "warble": warble,
    "power_down": power_down, "noise": noise, "grumble": grumble,
}


def render_phrase(suffix, index):
    """Render one event to a list of 16-bit sample integers."""
    rng = random.Random(SEED + index)
    samples = []
    for name, params in PHRASES[suffix]:
        samples.extend(PRIMITIVES[name](rng=rng, **params))
    fade_in, fade_out = _count(0.003), _count(0.01)
    for i in range(min(fade_in, len(samples))):
        samples[i] *= i / fade_in
    for i in range(min(fade_out, len(samples))):
        samples[-1 - i] *= i / fade_out
    peak = max(abs(s) for s in samples)
    scale = 32767.0 * 10.0 ** (PEAK_DBFS / 20.0) / peak
    return [int(round(s * scale)) for s in samples]


def wav_bytes(samples):
    buffer = io.BytesIO()
    with wave.open(buffer, "wb") as handle:
        handle.setnchannels(1)
        handle.setsampwidth(2)
        handle.setframerate(SAMPLE_RATE)
        handle.writeframes(struct.pack("<%dh" % len(samples), *samples))
    return buffer.getvalue()


def write_ssf(entries):
    """Build SSF V1.0 bytes from 49 (resref, strref) pairs; resref None or '' means no sound."""
    if len(entries) != SSF_ENTRY_COUNT:
        raise ValueError("an SSF holds exactly %d entries" % SSF_ENTRY_COUNT)
    table_size = 4 * SSF_ENTRY_COUNT
    data = bytearray(b"SSF V1.0")
    data += struct.pack("<II", SSF_ENTRY_COUNT, SSF_HEADER_SIZE) + bytes(24)
    first = SSF_HEADER_SIZE + table_size
    data += struct.pack("<%dI" % SSF_ENTRY_COUNT,
                        *[first + SSF_ENTRY_SIZE * i for i in range(SSF_ENTRY_COUNT)])
    for resref, strref in entries:
        raw = (resref or "").encode("ascii")
        if len(raw) > RESREF_LIMIT:
            raise ValueError("resref longer than %d characters: %s" % (RESREF_LIMIT, resref))
        data += raw.ljust(RESREF_LIMIT, b"\0") + struct.pack("<I", strref)
    return bytes(data)


def parse_ssf(data):
    """Return the 49 (resref, strref) pairs of an SSF V1.0 file."""
    if data[:8] != b"SSF V1.0":
        raise ValueError("not an SSF V1.0 file")
    count, table = struct.unpack_from("<II", data, 8)
    offsets = struct.unpack_from("<%dI" % count, data, table)
    entries = []
    for offset in offsets:
        resref = data[offset:offset + RESREF_LIMIT].rstrip(b"\0").decode("ascii")
        entries.append((resref, struct.unpack_from("<I", data, offset + RESREF_LIMIT)[0]))
    return entries


def resref_for(prefix, suffix):
    return "%s_%s" % (prefix, suffix)


def build_soundset(prefix=DEFAULT_PREFIX):
    """Return (ssf_bytes, sounds) where sounds lists dicts with index, event, resref, wav bytes."""
    sounds = []
    entries = []
    for index, suffix in enumerate(EVENTS):
        if suffix not in PHRASES:
            entries.append(("", SSF_NO_STRREF))
            continue
        resref = resref_for(prefix, suffix)
        samples = render_phrase(suffix, index)
        sounds.append({"index": index, "event": suffix, "resref": resref,
                       "samples": samples, "wav": wav_bytes(samples)})
        entries.append((resref, SSF_NO_STRREF))
    return write_ssf(entries), sounds


def write_output(output, prefix):
    output = Path(output)
    output.mkdir(parents=True, exist_ok=True)
    if any(output.iterdir()):
        raise ValueError("output directory is not empty: %s" % output)
    ssf, sounds = build_soundset(prefix)
    manifest = []
    for sound in sounds:
        (output / (sound["resref"] + ".wav")).write_bytes(sound["wav"])
        manifest.append({"index": sound["index"], "event": sound["event"], "resref": sound["resref"],
                         "duration_seconds": round(len(sound["samples"]) / SAMPLE_RATE, 3),
                         "sha256": hashlib.sha256(sound["wav"]).hexdigest()})
    (output / (prefix + ".ssf")).write_bytes(ssf)
    (output / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="ascii", newline="\n")
    return manifest


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--output", required=True, help="empty (or new) directory to write into")
    parser.add_argument("--prefix", default=DEFAULT_PREFIX, help="resref prefix (default %(default)s)")
    args = parser.parse_args(argv)
    try:
        manifest = write_output(args.output, args.prefix)
    except ValueError as error:
        print("error: %s" % error, file=sys.stderr)
        return 1
    total = sum(row["duration_seconds"] for row in manifest)
    print("Wrote %d sounds (%.2f s total), %s.ssf and manifest.json to %s"
          % (len(manifest), total, args.prefix, args.output))
    return 0


if __name__ == "__main__":
    sys.exit(main())
