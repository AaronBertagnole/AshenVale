"""Synthesises the Thornmother fight's placeholder sound effects (no recordings: sines, sweeps and filtered noise).

    python Tools/make_thornmother_sfx.py

Writes 44.1 kHz mono WAVs to Assets/Resources/Audio/Sfx/. Replace any of them with a real recording of the
same name and the game uses that instead.
"""
import os, wave
import numpy as np
from scipy.signal import butter, lfilter

SR = 44100
rng = np.random.default_rng(7)

def t(seconds): return np.arange(int(SR * seconds)) / SR
def noise(seconds): return rng.uniform(-1, 1, int(SR * seconds))
def band(x, lo, hi):
    b, a = butter(2, [lo / (SR / 2), hi / (SR / 2)], btype='band'); return lfilter(b, a, x)
def low(x, hz):
    b, a = butter(2, hz / (SR / 2)); return lfilter(b, a, x)
def high(x, hz):
    b, a = butter(2, hz / (SR / 2), btype='high'); return lfilter(b, a, x)
def sweep(seconds, f0, f1, shape=1.0):
    x = t(seconds); f = f0 + (f1 - f0) * (x / seconds) ** shape
    return np.sin(2 * np.pi * np.cumsum(f) / SR)
def decay(seconds, rate): return np.exp(-t(seconds) * rate)
def fade(x, a=.005, r=.03):
    n = len(x); env = np.ones(n); na, nr = int(SR * a), int(SR * r)
    env[:na] = np.linspace(0, 1, na); env[-nr:] = np.linspace(1, 0, nr); return x * env
def mix(*parts):
    n = max(len(p) for p in parts); out = np.zeros(n)
    for p in parts: out[:len(p)] += p
    return out
def at(x, seconds): return np.concatenate([np.zeros(int(SR * seconds)), x])
def save(name, x, peak=.85):
    x = fade(x); x = x / max(1e-6, np.abs(x).max()) * peak
    path = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'Assets', 'Resources', 'Audio', 'Sfx', name + '.wav')
    with wave.open(path, 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes((x * 32767).astype('<i2').tobytes())
    print(name, '%.2fs' % (len(x) / SR))

# Root slam: a heavy thud with a burst of earth.
save('thorn_slam', mix(sweep(.7, 95, 38) * decay(.7, 6) * 1.2, low(noise(.35), 900) * decay(.35, 12) * .8, band(noise(.6), 150, 500) * decay(.6, 5) * .3))
# Warning mark: a dry rustle that tightens and rises.
x = t(.95); save('thorn_warn', mix(band(noise(.95), 1800, 5200) * (x / .95) ** 1.5 * (.6 + .4 * np.sin(2 * np.pi * (6 + 18 * x) * x)), sweep(.95, 180, 420, 2) * (x / .95) ** 2 * .25), .5)
# Eruption: a sharp woody crack and a thump.
save('thorn_erupt', mix(high(noise(.12), 2500) * decay(.12, 40), band(noise(.4), 400, 1800) * decay(.4, 11) * .7, sweep(.4, 150, 60) * decay(.4, 10) * .9))
# Summon / calling an eruption: a rising, breathy swell.
x = t(1.0); swell = np.sin(np.pi * x / 1.0) ** 1.5
save('thorn_summon', mix(band(noise(1.0), 300, 900) * swell * .8, sweep(1.0, 110, 240, 1.5) * swell * .5, sweep(1.0, 660, 990, 1.5) * swell * .12), .7)
# Rooting: a long low creak as she drives her roots down.
x = t(1.1); creak = np.sign(np.sin(2 * np.pi * np.cumsum(52 + 10 * np.sin(2 * np.pi * 3.1 * x)) / SR)) * .5
save('thorn_root', mix(low(creak, 600) * np.sin(np.pi * x / 1.1) , low(noise(1.1), 250) * np.sin(np.pi * x / 1.1) * .9, at(sweep(.5, 80, 40) * decay(.5, 7), .55)))
# Thorn walls rising: a growing rumble full of little cracks.
x = t(1.6); crackle = (rng.uniform(0, 1, len(x)) > .9985) * rng.uniform(.4, 1, len(x)); crackle = high(lfilter([1], [1, -.6], crackle), 1200)
save('thorn_walls', mix(low(noise(1.6), 180) * np.minimum(1, x / .9) * decay(1.6, .6) * 1.1, crackle * 2.5 * np.minimum(1, x / .5), sweep(1.6, 45, 70) * np.minimum(1, x / 1.0) * .5))
# Breaking free / walls crumbling: snapping wood and falling debris.
snaps = mix(*[at(high(noise(.05), 1800) * decay(.05, 70) * a, d) for a, d in ((1, 0), (.8, .09), (.9, .2), (.5, .31), (.6, .42))])
save('thorn_break', mix(snaps, band(noise(.8), 300, 1400) * decay(.8, 5) * .5, sweep(.5, 120, 50) * decay(.5, 8) * .8))
# A sprout's healing mote: two soft chimes.
save('sprout_heal', mix(np.sin(2 * np.pi * 880 * t(.5)) * decay(.5, 9), at(np.sin(2 * np.pi * 1320 * t(.4)) * decay(.4, 10) * .7, .09), band(noise(.3), 3000, 7000) * decay(.3, 14) * .08), .45)
# Her death: a long groan sinking into falling leaves.
x = t(2.0); groan = sweep(2.0, 130, 42, .7) * (1 + .35 * np.sin(2 * np.pi * 7 * x)) * decay(2.0, 1.1)
save('thorn_death', mix(groan, low(np.sign(groan) * .25, 500) * decay(2.0, 1.3), at(band(noise(1.2), 2000, 6000) * decay(1.2, 2.5) * .35, .7), low(noise(.5), 300) * decay(.5, 8) * .7))
