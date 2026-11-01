"""Turns the owner's painted portal and ladder pictures into the game's prop files.

    python Tools/cut_props.py Portal_Painted.png Ladder_Painted.png

Portal (an upright oval on a transparent background): cropped to what is visible and scaled so the oval stands about
2.2 units tall -> Art/Props/portal.png (blue, map portals). Recoloured copies: return_portal.png (gold, the town return
portal) and town_portal.png (violet, your own T portal).
Ladder (rails with evenly spaced rungs): the stretch from one rung to the one two below it, scaled to 0.72 units wide,
with the cut blended so it tiles upward without a seam -> Art/Props/ladder.png (every map; a theme can still override
it with Art/Themes/<theme>/ladder.png).
"""
import os, sys, colorsys
from PIL import Image
import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROPS = os.path.join(ROOT, 'Assets', 'Resources', 'Art', 'Props')
PPU = 100

def mult4(v): return max(4, int(round(v / 4.0)) * 4)

def hue_shift(im, hue_to, sat_scale=1.0):
    """Moves the blue-cyan pixels' hue to `hue_to` (0..1), keeping their lightness; everything else stays."""
    arr = np.asarray(im).astype(float) / 255.0
    rgb, a = arr[..., :3], arr[..., 3:]
    mx, mn = rgb.max(2), rgb.min(2)
    sat = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0)
    out = rgb.copy()
    h, w = sat.shape
    for y in range(h):
        for x in range(w):
            if sat[y, x] < .12 or a[y, x, 0] < .02: continue
            hh, ll, ss = colorsys.rgb_to_hls(*rgb[y, x])
            if not (.42 < hh < .7): continue       # only the blue-cyan glow; the stone rim and gold fittings stay
            out[y, x] = colorsys.hls_to_rgb(hue_to, ll, min(1, ss * sat_scale))
    return Image.fromarray((np.concatenate([out, a], 2) * 255).astype(np.uint8), 'RGBA')

def portal(path):
    im = Image.open(path).convert('RGBA')
    a = np.asarray(im)[..., 3]
    ys, xs = np.where(a > 24)
    im = im.crop((xs.min() - 6, ys.min() - 6, xs.max() + 7, ys.max() + 7))
    height = 2.2 * PPU
    scale = height / im.height
    im = im.resize((mult4(im.width * scale), mult4(height)), Image.LANCZOS)
    im.save(os.path.join(PROPS, 'portal.png'))
    hue_shift(im, 40 / 360.0).save(os.path.join(PROPS, 'return_portal.png'))
    hue_shift(im, 268 / 360.0, .9).save(os.path.join(PROPS, 'town_portal.png'))
    print('portal', im.size)

def ladder(path, width_units=.72):
    im = Image.open(path).convert('RGBA')
    a = np.asarray(im)[..., 3]
    cols = np.where(a.max(0) > 120)[0]
    im = im.crop((cols.min() - 4, 0, cols.max() + 5, im.height))
    a = np.asarray(im)[..., 3]
    # rung centres: rows where the middle (between the rails) is opaque
    mid = a[:, im.width * 2 // 5: im.width * 3 // 5].mean(1)
    rows = np.where(mid > 120)[0]
    centres, start, prev = [], rows[0], rows[0]
    for r in rows[1:]:
        if r != prev + 1: centres.append((start + prev) // 2); start = r
        prev = r
    centres.append((start + prev) // 2)
    assert len(centres) >= 3, centres
    period = (centres[-1] - centres[0]) / (len(centres) - 1)
    top = int(centres[1] - period / 2)                       # cut half way between rungs: only rails there
    piece = im.crop((0, top, im.width, int(top + 2 * period)))
    scale = width_units * PPU / piece.width
    tile = piece.resize((mult4(piece.width * scale), mult4(piece.height * scale)), Image.LANCZOS)
    # blend the top few rows into the bottom so the rails continue without a seam
    arr = np.asarray(tile).astype(float)
    n = 6
    for i in range(n):
        t = (i + 1) / (n + 1)
        arr[i] = arr[i] * t + arr[tile.height - n + i] * (1 - t)
    Image.fromarray(arr.astype(np.uint8), 'RGBA').save(os.path.join(PROPS, 'ladder.png'))
    print('ladder', tile.size, 'rung spacing %.2f units' % (period * scale / PPU))

if __name__ == '__main__':
    os.makedirs(PROPS, exist_ok=True)
    portal(sys.argv[1]); ladder(sys.argv[2])
