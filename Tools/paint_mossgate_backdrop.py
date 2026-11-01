"""Paints the Mossgate Hollow backdrop procedurally (same framing as the Emberfields picture, at 2x size).

    python Tools/paint_mossgate_backdrop.py      writes Assets/Resources/Art/Backdrops/mossgate_hollow.jpg

Uses the mossgate theme tiles for the path and wall at the bottom, so run Tools/recolor_theme.py first if those change.
Change the seed (rng / np_rng) for a different arrangement of the same scene.

Layers back to front: sky and clouds, mountain ridges, mist, far pines (a few autumn trees on the left),
waterfall cliff, the mossy stone gate, mid trees, framing giant trees with canopies and hanging moss,
light rays, undergrowth, fireflies, and the mossy path and wall at the bottom (from the mossgate tiles).
"""
import math, os, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageChops

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
W, H = 4344, 1448
PATH = 1216                     # walking line (608 in the 724 px original)
rng = random.Random(7)
np_rng = np.random.default_rng(7)

def mix(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))

def fbm(w, h, octaves=5, base=6, seed=0, persistence=.55):
    g = np.random.default_rng(seed)
    out = np.zeros((h, w), np.float32)
    amp, total = 1.0, 0.0
    for o in range(octaves):
        cells = base * (2 ** o)
        small = g.random((max(2, int(cells * h / w) + 2), cells + 2)).astype(np.float32)
        im = Image.fromarray((small * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC)
        out += np.asarray(im, np.float32) / 255.0 * amp
        total += amp
        amp *= persistence
    return out / total

def layer():
    return Image.new('RGBA', (W, H), (0, 0, 0, 0))

def comp(base, lay, blur=0):
    if blur:
        lay = lay.filter(ImageFilter.GaussianBlur(blur))
    return Image.alpha_composite(base, lay)

def leaf_poly(x, y, size, angle):
    pts = []
    for t in range(8):
        a = t / 8 * 2 * math.pi
        r = size * (0.5 if t % 4 in (1, 3) else (1.0 if t % 4 == 0 else 0.5))
        lx, ly = math.cos(a) * size, math.sin(a) * size * 0.45
        ca, sa = math.cos(angle), math.sin(angle)
        pts.append((x + lx * ca - ly * sa, y + lx * sa + ly * ca))
    return pts

def foliage(d, cx, cy, rx, ry, colors, leaves, size=(10, 26), light=(.6, -.8), alpha=255, alt=None, alt_left=0.0):
    """A canopy mass made of lumps; each leaf is shaded by where it sits in its lump (lit from the upper right)."""
    lx, ly = light; ln = math.hypot(lx, ly); lx, ly = lx / ln, ly / ln
    lumps = []
    for k in range(max(5, int(leaves / 450))):
        a = rng.random() * 2 * math.pi; r = math.sqrt(rng.random()) * .72
        lumps.append((cx + math.cos(a) * rx * r, cy + math.sin(a) * ry * r, rx * rng.uniform(.28, .5), ry * rng.uniform(.32, .55)))
    items = []
    for _ in range(leaves):
        ux, uy, rxl, ryl = lumps[rng.randrange(len(lumps))]
        a = rng.random() * 2 * math.pi; r = math.sqrt(rng.random())
        ox, oy = math.cos(a) * r, math.sin(a) * r
        x, y = ux + ox * rxl, uy + oy * ryl
        shade = .5 + .42 * (ox * lx + oy * ly) * r              # lit side of the lump
        shade += .22 * ((cy - y) / (ry * 2))                     # tops of the whole mass a bit lighter
        shade += rng.uniform(-.12, .12)
        use_alt = alt is not None and rng.random() < alt_left * np.clip((cx + rx - x) / (2 * rx), 0, 1) ** 1.5
        items.append((shade, x, y, use_alt))
    items.sort()
    for shade, x, y, use_alt in items:
        pal = alt if use_alt else colors
        n = len(pal)
        i = min(n - 1, max(0, int(shade * n)))
        c = tuple(min(255, max(0, v + rng.randint(-8, 8))) for v in pal[i])
        s = rng.uniform(*size)
        d.polygon(leaf_poly(x, y, s, rng.random() * math.pi), fill=c + (alpha,))

# ---------------------------------------------------------------- sky
y = np.linspace(0, 1, H)[:, None]
top, horizon = np.array([96, 168, 196], np.float32), np.array([228, 238, 214], np.float32)
t = np.clip(y / .62, 0, 1) ** .9
sky = (top * (1 - t) + horizon * t)[:, :, None].repeat(1, 2) if False else None
sky = np.zeros((H, W, 3), np.float32)
sky[:] = (top[None, None, :] * (1 - t[:, :, None]) + horizon[None, None, :] * t[:, :, None])
clouds = fbm(W, H, 6, 5, seed=3)
cmask = np.clip((clouds - .5) * 4.0, 0, 1) * np.clip(1.2 - y * 1.9, 0, 1)
sky = sky * (1 - cmask[:, :, None] * .85) + np.array([250, 248, 238], np.float32) * cmask[:, :, None] * .85
shade = np.clip((fbm(W, H, 4, 8, seed=4) - .45) * 2, 0, 1) * cmask
sky -= shade[:, :, None] * np.array([40, 30, 15], np.float32)
img = Image.fromarray(np.clip(sky, 0, 255).astype(np.uint8)).convert('RGBA')

haze = (214, 230, 212)

# ---------------------------------------------------------------- ridges
def ridge(base_y, height, color, seed, rough=1.0, fade=0.0, blur=3):
    n = fbm(W, 1, 6, 3, seed=seed)[0]
    n2 = fbm(W, 1, 4, 12, seed=seed + 1)[0]
    lay = layer(); d = ImageDraw.Draw(lay)
    pts = [(0, H)]
    for x in range(0, W, 4):
        yy = base_y - height * (n[x] * .8 + n2[x] * .2 * rough)
        pts.append((x, yy))
    pts += [(W, H)]
    d.polygon(pts, fill=mix(color, haze, fade) + (255,))
    return lay.filter(ImageFilter.GaussianBlur(blur))

img = comp(img, ridge(.60 * H, .30 * H, (150, 182, 186), 11, fade=.35))
img = comp(img, ridge(.66 * H, .24 * H, (112, 152, 150), 12, fade=.2))
# mist band
mist = np.zeros((H, W), np.float32)
band = np.exp(-((y[:, 0] - .64) / .07) ** 2)
mist[:] = band[:, None] * (.55 + .45 * fbm(W, H, 4, 6, seed=5))
m = Image.fromarray((np.clip(mist, 0, 1) * 200).astype(np.uint8))
mist_l = Image.new('RGBA', (W, H), haze + (0,)); mist_l.putalpha(m)
img = Image.alpha_composite(img, mist_l)

# ---------------------------------------------------------------- waterfall cliff (right, far)
lay = layer(); d = ImageDraw.Draw(lay)
cliff = [(int(.70 * W), int(.74 * H)), (int(.71 * W), int(.46 * H)), (int(.745 * W), int(.40 * H)), (int(.80 * W), int(.38 * H)),
         (int(.86 * W), int(.41 * H)), (int(.89 * W), int(.50 * H)), (int(.90 * W), int(.74 * H))]
d.polygon(cliff, fill=mix((120, 140, 128), haze, .3) + (255,))
for k in range(26):       # rock strata
    xx = rng.uniform(.71, .89) * W; yy = rng.uniform(.42, .72) * H
    d.line([(xx, yy), (xx + rng.uniform(40, 160), yy + rng.uniform(-10, 10))], fill=mix((92, 112, 102), haze, .3) + (200,), width=5)
for k in range(140):      # moss and trees on the cliff top
    xx = rng.uniform(.71, .89) * W; yy = .40 * H + abs(xx - .80 * W) * .12 + rng.uniform(-12, 18)
    d.ellipse([xx - 26, yy - 20, xx + 26, yy + 14], fill=mix((70, 120, 80), haze, .35) + (255,))
img = comp(img, lay, 2)
lay = layer(); d = ImageDraw.Draw(lay)
for k in range(60):       # falling water
    xx = .795 * W + rng.uniform(-38, 38)
    d.line([(xx, .395 * H), (xx + rng.uniform(-6, 6), .70 * H)], fill=(235, 245, 248, rng.randint(90, 200)), width=rng.randint(3, 9))
img = comp(img, lay, 2)
lay = layer(); d = ImageDraw.Draw(lay)
for k in range(40):
    xx = .795 * W + rng.uniform(-120, 120); yy = .70 * H + rng.uniform(-40, 30)
    d.ellipse([xx - 70, yy - 40, xx + 70, yy + 40], fill=(240, 246, 244, 60))
img = comp(img, lay, 18)

# ---------------------------------------------------------------- far pines (and a few autumn trees on the left)
def pine(d, x, base, h, col, alpha=255):
    tiers = 6
    dark = mix(col, (20, 40, 36), .25); light = mix(col, (230, 240, 200), .12)
    for i in range(tiers):
        ty = base - h * (i / tiers)
        w = h * .28 * (1 - i / tiers) + 6
        tip = ty - h / tiers * 1.9
        d.polygon([(x - w, ty), (x, ty + 4), (x, tip)], fill=dark + (alpha,))
        d.polygon([(x, ty + 4), (x + w, ty), (x, tip)], fill=light + (alpha,))
    d.rectangle([x - 4, base - 4, x + 4, base + 30], fill=col + (alpha,))

def round_tree(d, x, base, h, col):
    d.rectangle([x - 6, base - h * .45, x + 6, base + 20], fill=mix(col, (60, 40, 30), .6) + (255,))
    pal = (mix(col, (40, 30, 30), .45), mix(col, (40, 30, 30), .2), col, mix(col, (255, 240, 200), .25))
    foliage(d, x, base - h * .62, h * .36, h * .34, pal, int(h * 3), size=(6, 12))

for row, (base, hmin, hmax, col, fade, blur) in enumerate([(.70, .10, .17, (88, 126, 112), .45, 3), (.75, .13, .24, (62, 104, 86), .25, 2),
                                                         (.79, .16, .28, (44, 86, 66), .1, 1)]):
    lay = layer(); d = ImageDraw.Draw(lay)
    x = -40
    while x < W + 40:
        h = rng.uniform(hmin, hmax) * H
        left = x / W
        if left < .36 and rng.random() < .55 - left:        # autumn lingering on the trail side
            round_tree(d, x, base * H, h * .8, mix(rng.choice([(206, 112, 48), (226, 160, 62), (184, 70, 40)]), haze, fade + .1))
        else:
            pine(d, x, base * H, h, mix(col, haze, fade))
        x += rng.uniform(26, 70) * (1.3 if row == 0 else 1.0)
    img = comp(img, lay, blur)
    if row == 0:   # thin mist between the rows
        img = Image.alpha_composite(img, mist_l.filter(ImageFilter.GaussianBlur(4)).point(lambda v: v) if False else img)

# low mist over the far forest
mist2 = np.zeros((H, W), np.float32)
mist2[:] = np.exp(-((y[:, 0] - .80) / .05) ** 2)[:, None] * (.5 + .5 * fbm(W, H, 4, 10, seed=8))
m2 = Image.fromarray((np.clip(mist2, 0, 1) * 150).astype(np.uint8))
mist_low = Image.new('RGBA', (W, H), (205, 226, 205, 0)); mist_low.putalpha(m2)
img = Image.alpha_composite(img, mist_low)

# ---------------------------------------------------------------- the mossy stone gate (centre)
gx, gbase = .50 * W, .845 * H
gw, gh = .17 * W, .36 * H
glow = layer(); d = ImageDraw.Draw(glow)
d.ellipse([gx - gw * .32, gbase - gh * .88, gx + gw * .32, gbase + gh * .05], fill=(214, 240, 150, 170))
img = comp(img, glow, 60)
lay = layer(); d = ImageDraw.Draw(lay)
stone, stone_dk = (128, 136, 118), (90, 98, 84)
pw = gw * .16
for side in (-1, 1):                                    # pillars
    px0 = gx + side * (gw * .5 - pw * .5)
    d.rectangle([px0 - pw / 2, gbase - gh * .62, px0 + pw / 2, gbase], fill=stone + (255,))
    for k in range(9):                                  # blocks
        by = gbase - gh * .62 + k * gh * .07
        d.line([(px0 - pw / 2, by), (px0 + pw / 2, by)], fill=stone_dk + (255,), width=5)
        off = pw * .3 if k % 2 else -pw * .2
        d.line([(px0 + off, by), (px0 + off, by + gh * .07)], fill=stone_dk + (255,), width=4)
    d.rectangle([px0 - pw * .62, gbase - gh * .66, px0 + pw * .62, gbase - gh * .60], fill=stone_dk + (255,))
# arch: ring of voussoirs
cxa, cya, r_out, r_in = gx, gbase - gh * .62, gw * .5 + pw * .1, gw * .5 - pw * 1.0
for k in range(13):
    a0, a1 = math.pi + k * math.pi / 13, math.pi + (k + 1) * math.pi / 13
    poly = [(cxa + math.cos(a0) * r_in, cya + math.sin(a0) * r_in * .9), (cxa + math.cos(a0) * r_out, cya + math.sin(a0) * r_out * .9),
            (cxa + math.cos(a1) * r_out, cya + math.sin(a1) * r_out * .9), (cxa + math.cos(a1) * r_in, cya + math.sin(a1) * r_in * .9)]
    d.polygon(poly, fill=mix(stone, (150, 156, 136), rng.random() * .5) + (255,), outline=stone_dk + (255,))
# keystone with a rune
ks = [(cxa - 34, cya - r_out * .9 - 16), (cxa + 34, cya - r_out * .9 - 16), (cxa + 24, cya - r_in * .9 + 8), (cxa - 24, cya - r_in * .9 + 8)]
d.polygon(ks, fill=(150, 158, 138, 255), outline=stone_dk + (255,))
d.ellipse([cxa - 14, cya - r_out * .82 - 6, cxa + 14, cya - r_out * .82 + 22], outline=(190, 240, 150, 255), width=5)
img = comp(img, lay, 1)
# moss draped over the gate
lay = layer(); d = ImageDraw.Draw(lay)
for k in range(420):
    a = math.pi + rng.random() * math.pi
    rr = rng.uniform(r_in, r_out + 20)
    xx, yy = cxa + math.cos(a) * rr, cya + math.sin(a) * rr * .9 - rng.uniform(0, 14)
    d.ellipse([xx - 9, yy - 6, xx + 9, yy + 6], fill=rng.choice([(78, 128, 58), (104, 152, 64), (140, 182, 80)]) + (255,))
for side in (-1, 1):
    px0 = gx + side * (gw * .5 - pw * .5)
    for k in range(110):
        xx = px0 + rng.uniform(-pw * .6, pw * .6); yy = gbase - rng.random() ** 2.2 * gh * .66
        d.ellipse([xx - 8, yy - 6, xx + 8, yy + 6], fill=rng.choice([(70, 118, 52), (96, 146, 60), (130, 176, 76)]) + (255,))
for k in range(70):                                     # moss strands hanging from the arch
    a = math.pi + rng.uniform(.12, .88) * math.pi
    xx, yy = cxa + math.cos(a) * r_in, cya + math.sin(a) * r_in * .9
    length = rng.uniform(30, 160)
    d.line([(xx, yy), (xx + rng.uniform(-8, 8), yy + length)], fill=(140, 184, 86, 220), width=rng.randint(3, 6))
img = comp(img, lay, 1)
# light motes inside the gate
lay = layer(); d = ImageDraw.Draw(lay)
for k in range(60):
    xx = gx + rng.uniform(-r_in * .8, r_in * .8); yy = gbase - rng.uniform(.05, .55) * gh
    s = rng.uniform(4, 10)
    d.ellipse([xx - s, yy - s, xx + s, yy + s], fill=(230, 255, 170, 200))
img = comp(img, lay, 3)

# ---------------------------------------------------------------- mid trees
bark_noise = fbm(W, H, 4, 6, seed=21)
grain = np.asarray(Image.fromarray((np.random.default_rng(22).random((H // 40, W // 3)) * 255).astype(np.uint8)).resize((W, H), Image.BICUBIC), np.float32) / 255.0
moss_noise = fbm(W, H, 5, 10, seed=23)

def trunk_layer(x0, base, top_x, top_y, w_base, w_top, light_side, bark=((52, 40, 30), (92, 74, 54), (140, 116, 84)), moss=.0, roots=True):
    """A tree trunk as an image: a curving tapered shape, shaded across its width, with bark grain and moss."""
    y0, y1 = int(max(0, min(base, top_y))), int(min(H, max(base, top_y)))
    ys = np.arange(y0, y1)
    tt = (ys - base) / float(top_y - base)
    cx = x0 + (top_x - x0) * np.clip(tt, 0, 1) ** 1.6 + np.sin(tt * 3.1) * w_base * .12
    w = w_base + (w_top - w_base) * np.clip(tt, 0, 1) ** .6
    if roots:
        w = w * (1 + 1.1 * np.clip(.12 - tt, 0, .12) / .12)      # flare out into roots at the bottom
    xs = np.arange(W)[None, :]
    u = (xs - cx[:, None]) / (w[:, None] / 2)                     # -1 .. 1 across the trunk
    inside = np.clip((1 - np.abs(u)) * w[:, None] / 3, 0, 1)      # soft edge
    lit = np.clip(.5 + .42 * u * light_side, 0, 1)
    lit = lit - .25 * (1 - np.sqrt(np.clip(1 - u ** 2, 0, 1)))    # rounder: darker at both edges
    g = grain[y0:y1] * .5 + bark_noise[y0:y1] * .5
    lit = np.clip(lit + (g - .5) * .55, 0, 1)
    c0, c1, c2 = (np.array(c, np.float32) for c in bark)
    col = np.where(lit[:, :, None] < .5, c0 + (c1 - c0) * (lit[:, :, None] / .5), c1 + (c2 - c1) * ((lit[:, :, None] - .5) / .5))
    if moss > 0:
        mm = np.clip((moss_noise[y0:y1] - (.62 - moss)) * 5 + (.35 * u * light_side), 0, 1) * np.clip(1 - np.abs(u) * .2, 0, 1)
        mcol = np.array([82, 132, 58], np.float32) + (np.array([150, 190, 84], np.float32) - np.array([82, 132, 58], np.float32)) * lit[:, :, None]
        col = col * (1 - mm[:, :, None]) + mcol * mm[:, :, None]
    out = np.zeros((H, W, 4), np.uint8)
    out[y0:y1, :, :3] = np.clip(col, 0, 255).astype(np.uint8)
    out[y0:y1, :, 3] = (inside * 255).astype(np.uint8)
    return Image.fromarray(out, 'RGBA')

def branch(d, p0, p1, p2, w0, w1, col, light_col):
    """A tapered curved branch (quadratic curve from p0 through p1 to p2)."""
    left, right = [], []
    for i in range(25):
        t = i / 24
        x = (1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0]
        y = (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]
        dx = 2 * (1 - t) * (p1[0] - p0[0]) + 2 * t * (p2[0] - p1[0]); dy = 2 * (1 - t) * (p1[1] - p0[1]) + 2 * t * (p2[1] - p1[1])
        n = math.hypot(dx, dy) or 1; nx, ny = -dy / n, dx / n
        w = (w0 + (w1 - w0) * t) / 2
        left.append((x + nx * w, y + ny * w)); right.append((x - nx * w, y - ny * w))
    d.polygon(left + right[::-1], fill=col + (255,))
    d.line(left, fill=light_col + (255,), width=max(2, int(w0 * .12)))

for (tx, tw, canopy_c, side) in [(.30, .032, ((30, 66, 46), (46, 92, 56), (72, 122, 64), (112, 160, 82)), 1),
                                 (.67, .028, ((28, 62, 44), (42, 86, 54), (66, 116, 62), (104, 152, 78)), 1)]:
    img = comp(img, trunk_layer(tx * W, .83 * H, tx * W + 50, .30 * H, tw * W, tw * W * .45, side,
                                bark=((50, 44, 34), (82, 70, 54), (118, 100, 76)), moss=.12), 1.2)
    lay = layer(); d = ImageDraw.Draw(lay)
    branch(d, (tx * W + 20, .45 * H), (tx * W - .04 * W, .38 * H), (tx * W - .07 * W, .30 * H), 26, 6, (74, 62, 48), (104, 90, 70))
    branch(d, (tx * W + 30, .40 * H), (tx * W + .05 * W, .34 * H), (tx * W + .08 * W, .27 * H), 24, 6, (74, 62, 48), (104, 90, 70))
    foliage(d, tx * W + 30, .26 * H, .12 * W, .12 * H, canopy_c, 4200, size=(12, 26))
    img = comp(img, lay, 1.2)

# ---------------------------------------------------------------- light rays from the upper right
rays = layer(); d = ImageDraw.Draw(rays)
for k in range(7):
    x0 = rng.uniform(.52, .86) * W
    wid = rng.uniform(40, 130)
    d.polygon([(x0, 0), (x0 + wid, 0), (x0 - .32 * W + wid * 3, H * .95), (x0 - .32 * W, H * .95)], fill=(255, 246, 200, rng.randint(26, 46)))
img = comp(img, rays, 30)

# ---------------------------------------------------------------- framing giant trees
canopy_left = ((26, 58, 40), (40, 84, 52), (60, 112, 62), (92, 146, 74), (146, 186, 92))
canopy_right = ((22, 54, 40), (34, 78, 50), (54, 106, 60), (86, 140, 72), (140, 182, 90))
autumn = ((120, 46, 28), (168, 72, 34), (208, 112, 44), (236, 162, 66))
for side in (-1, 1):
    if side < 0:
        img = comp(img, trunk_layer(.05 * W, 1.02 * H, .15 * W, -.05 * H, .115 * W, .05 * W, 1, moss=.22), .6)
    else:
        img = comp(img, trunk_layer(.95 * W, 1.02 * H, .85 * W, -.05 * H, .11 * W, .05 * W, -1, moss=.26), .6)
    lay = layer(); d = ImageDraw.Draw(lay)
    bx = (.11 if side < 0 else .89) * W
    for (by, ex, ey, w0) in [(.36, .27, .14, 64), (.22, .38, .05, 52), (.30, .20, .10, 44)]:
        x2 = (ex if side < 0 else 1 - ex) * W
        mid = (bx + (x2 - bx) * .55, by * H - .02 * H)
        branch(d, (bx, by * H), mid, (x2, ey * H), w0, 12, (78, 62, 46), (118, 98, 72))
    if side < 0:
        # the last autumn leaves mixed into the left canopy, most of them on its left (trail) side
        foliage(d, .17 * W, .07 * H, .26 * W, .18 * H, canopy_left, 10500, size=(14, 32), alt=autumn, alt_left=.55)
        foliage(d, .36 * W, .05 * H, .10 * W, .08 * H, canopy_left, 3200, size=(14, 30))
    else:
        foliage(d, .83 * W, .06 * H, .24 * W, .18 * H, canopy_right, 9600, size=(14, 32))
        foliage(d, .64 * W, .03 * H, .10 * W, .08 * H, canopy_right, 3000, size=(14, 30))
    img = comp(img, lay, 0)

# hanging moss from the canopies
lay = layer(); d = ImageDraw.Draw(lay)
for k in range(520):
    side = rng.choice([-1, 1])
    xx = rng.uniform(.0, .44) * W if side < 0 else rng.uniform(.58, 1.0) * W
    edge = (.20 if side < 0 else .21) * H - abs(xx - (.16 if side < 0 else .84) * W) * .35
    yy = max(.02 * H, edge + rng.uniform(-40, 40))
    length = rng.uniform(60, 340)
    pts = [(xx, yy)]
    for s in range(1, 9):
        pts.append((xx + math.sin(s * .9 + k) * 6, yy + length * s / 8))
    d.line(pts, fill=rng.choice([(118, 162, 74), (148, 188, 90), (96, 140, 66)]) + (rng.randint(150, 230),), width=rng.randint(3, 7))
img = comp(img, lay, 1)

# ---------------------------------------------------------------- undergrowth along the base
lay = layer(); d = ImageDraw.Draw(lay)
for k in range(150):                                    # bushes
    xx = rng.uniform(0, W); yy = rng.uniform(.765, .80) * H
    s = rng.uniform(50, 150)
    pal = rng.choice([((24, 58, 40), (38, 84, 50), (62, 114, 60), (104, 154, 76)), ((30, 62, 46), (50, 96, 62), (82, 134, 72), (130, 176, 90)),
                      ((40, 66, 38), (64, 100, 50), (98, 140, 62), (150, 186, 86))])
    foliage(d, xx, yy - rng.uniform(0, .03) * H, s, s * rng.uniform(.5, .75), pal, int(s * 9), size=(8, 18))
for k in range(60):                                     # mossy rocks
    xx = rng.uniform(0, W); yy = rng.uniform(.78, .80) * H; s = rng.uniform(40, 110)
    d.ellipse([xx - s, yy - s * .6, xx + s, yy + s * .3], fill=(104, 112, 98, 255))
    d.ellipse([xx - s * .9, yy - s * .62, xx + s * .7, yy - s * .15], fill=(96, 146, 62, 255))
for k in range(170):                                    # ferns
    xx = rng.uniform(0, W); yy = rng.uniform(.785, .80) * H
    for f in range(7):
        a = -math.pi / 2 + (f - 3) * .32
        L = rng.uniform(50, 110)
        ex, ey = xx + math.cos(a) * L, yy + math.sin(a) * L
        d.line([(xx, yy), (ex, ey)], fill=(70, 130, 60, 255), width=4)
        for s in range(1, 7):
            px, py = xx + (ex - xx) * s / 7, yy + (ey - yy) * s / 7
            d.line([(px, py), (px + math.cos(a + 1.2) * 14, py + math.sin(a + 1.2) * 14)], fill=(98, 160, 70, 255), width=3)
            d.line([(px, py), (px + math.cos(a - 1.2) * 14, py + math.sin(a - 1.2) * 14)], fill=(98, 160, 70, 255), width=3)
mushroom_spots = [rng.uniform(0, W) for _ in range(9)]
for k in range(34):                                     # mushrooms, in little groups
    xx = rng.choice(mushroom_spots) + rng.gauss(0, 50); yy = rng.uniform(.78, .80) * H; s = rng.uniform(14, 40)
    d.rectangle([xx - s * .18, yy - s * .9, xx + s * .18, yy], fill=(236, 226, 200, 255))
    d.pieslice([xx - s, yy - s * 1.5, xx + s, yy - s * .3], 180, 360, fill=rng.choice([(214, 120, 70), (230, 180, 90), (200, 90, 70)]) + (255,))
    for dot in range(3):
        dx = rng.uniform(-s * .5, s * .5)
        d.ellipse([xx + dx - 3, yy - s * 1.05 - 3, xx + dx + 3, yy - s * 1.05 + 3], fill=(255, 245, 220, 255))
flower_spots = [rng.uniform(0, W) for _ in range(16)]
for k in range(170):                                    # little flowers, in patches
    xx = rng.choice(flower_spots) + rng.gauss(0, 90); yy = rng.uniform(.74, .80) * H; s = rng.uniform(4, 8)
    d.ellipse([xx - s, yy - s, xx + s, yy + s], fill=rng.choice([(250, 248, 236), (250, 224, 120), (190, 220, 255), (240, 170, 120)]) + (255,))
img = comp(img, lay, .8)

# fireflies / floating motes
lay = layer(); d = ImageDraw.Draw(lay)
for k in range(150):
    xx = rng.uniform(0, W); yy = rng.uniform(.25, .86) * H; s = rng.uniform(3, 8)
    d.ellipse([xx - s, yy - s, xx + s, yy + s], fill=(240, 255, 170, 230))
glowl = lay.filter(ImageFilter.GaussianBlur(10))
img = comp(img, glowl, 0); img = comp(img, lay, 1)

# ---------------------------------------------------------------- path and wall from the mossgate tiles
T = os.path.join(ROOT, 'Assets', 'Resources', 'Art', 'Themes', 'mossgate') + os.sep
scale = (W / 42.0) / 100.0                              # backdrop px per unit / tile px per unit
g = Image.open(T + 'ground.png').convert('RGBA'); gb = Image.open(T + 'ground_back.png').convert('RGBA')
g = g.resize((int(g.width * scale), int(g.height * scale)), Image.LANCZOS)
gb = gb.resize((int(gb.width * scale), int(gb.height * scale)), Image.LANCZOS)
strip = Image.new('RGBA', (W, H), (0, 0, 0, 0))
for x in range(0, W, g.width):
    strip.alpha_composite(gb, (x, PATH - gb.height))
    strip.alpha_composite(g, (x, PATH))
# fill anything below the tile with the wall's darkest tone
bottom = PATH + g.height
if bottom < H:
    ImageDraw.Draw(strip).rectangle([0, bottom, W, H], fill=(30, 34, 22, 255))
img = Image.alpha_composite(img, strip)

# ---------------------------------------------------------------- finishing: brush texture, soft bloom, grade, vignette
rgb = img.convert('RGB')
arr = np.asarray(rgb)
strokes = Image.new('RGBA', (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(strokes)
for k in range(90000):
    xx = rng.randrange(W); yy = rng.randrange(int(H * .82))      # leave the tiled path alone
    c = arr[yy, xx].astype(int)
    j = rng.randint(-14, 14)
    col = tuple(int(min(255, max(0, v + j))) for v in c)
    a = rng.uniform(-.9, -.3) if rng.random() < .7 else rng.uniform(.3, .9)
    L = rng.uniform(5, 13)
    d.line([(xx, yy), (xx + math.cos(a) * L, yy + math.sin(a) * L)], fill=col + (110,), width=rng.randint(3, 5))
rgb = Image.alpha_composite(rgb.convert('RGBA'), strokes).convert('RGB')
bloom = rgb.filter(ImageFilter.GaussianBlur(26))
rgb = ImageChops.screen(rgb, Image.blend(Image.new('RGB', rgb.size, (0, 0, 0)), bloom, .22))
a = np.asarray(rgb, np.float32)
a = a * np.array([1.0, 1.02, .98], np.float32)          # a touch greener
yy, xx = np.mgrid[0:H, 0:W]
v = 1 - .28 * (((xx / W - .5) * 1.6) ** 2 + ((yy / H - .45) * 1.3) ** 2)
a *= np.clip(v, .7, 1)[:, :, None]
a += (np_rng.random((H, W, 1)) - .5) * 6
out = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))
out.convert('RGB').save(os.path.join(ROOT, 'Assets', 'Resources', 'Art', 'Backdrops', 'mossgate_hollow.jpg'), quality=93, subsampling=0, optimize=True)
print('saved', out.size)
