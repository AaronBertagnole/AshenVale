"""Paints the ladder tiles and portal props in the style of the painted maps (soft shading, slight colour jitter).

    python Tools/make_props.py

Writes, at 100 px per unit, drawn 3x and shrunk for smooth edges:
  Assets/Resources/Art/Themes/<theme>/ladder.png    60 x 100, one section that tiles upward (pivot: bottom centre)
  Assets/Resources/Art/Props/portal.png             120 x 230 stone arch (map portals on hunting maps)
  Assets/Resources/Art/Props/return_portal.png      the same arch, warmer stone (town return portals, unused on scenes)
  Assets/Resources/Art/Props/portal_swirl.png       90 x 190 white swirl, tinted in code (blue map, gold return, violet town)
Themes: autumn (autumn leaves), mossgate / forest (moss), road / town / rivertown / meadow (clean oak), cave (bleached,
bluish), marsh (rope ladder).
"""
import os, math, random
from PIL import Image, ImageDraw, ImageFilter, ImageChops

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, 'Assets', 'Resources', 'Art')
S = 3   # supersample

def canvas(w, h): return Image.new('RGBA', (w * S, h * S), (0, 0, 0, 0))
def shrink(im, w, h): return im.resize((w, h), Image.LANCZOS)
def jit(c, rng, amount=10):
    return tuple(max(0, min(255, v + rng.randint(-amount, amount))) for v in c[:3]) + ((c[3],) if len(c) > 3 else ())
def mix(a, b, t): return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))

def wood_plank(d, box, base, rng, grain=True, vertical=True):
    """A plank with a light edge, a dark edge and a few grain lines (box in supersampled px)."""
    x0, y0, x1, y1 = box
    d.rectangle(box, fill=jit(base, rng, 6))
    light, dark = mix(base, (255, 240, 210), .35), mix(base, (40, 25, 15), .45)
    if vertical:
        d.rectangle([x0, y0, x0 + 2 * S, y1], fill=light)
        d.rectangle([x1 - 2 * S, y0, x1, y1], fill=dark)
        if grain:
            for _ in range(int((y1 - y0) / (40 * S)) + 2):
                gx = rng.uniform(x0 + 3 * S, x1 - 3 * S); gy = rng.uniform(y0, y1); L = rng.uniform(15, 45) * S
                d.line([(gx, gy), (gx + rng.uniform(-1, 1) * S, min(y1, gy + L))], fill=mix(base, (40, 25, 15), .3), width=S)
    else:
        d.rectangle([x0, y0, x1, y0 + 2 * S], fill=light)
        d.rectangle([x0, y1 - 2 * S, x1, y1], fill=dark)

def speckle(d, x, y, spread, stone, rng, n=7):
    """Pebbly texture: a few darker and lighter flecks around (x, y)."""
    for _ in range(n):
        px, py = x + rng.uniform(-spread, spread), y + rng.uniform(-spread, spread)
        r = rng.uniform(.8, 2.2) * S
        c = mix(stone, (60, 50, 40), rng.uniform(.12, .3)) if rng.random() < .6 else mix(stone, (255, 255, 245), .4)
        d.ellipse([px - r, py - r, px + r, py + r], fill=c)

def block(d, box, stone, rng):
    """One painted stone block: uneven edge, lit top edge, shadowed bottom, flecks."""
    x0, y0, x1, y1 = box
    base = jit(stone, rng, 10)
    d.rounded_rectangle([x0 + rng.uniform(0, 1.5) * S, y0, x1 - rng.uniform(0, 1.5) * S, y1], radius=4 * S, fill=base)
    d.rounded_rectangle([x0 + 2 * S, y0 + S, x1 - 2 * S, y0 + 4 * S], radius=2 * S, fill=mix(base, (255, 255, 240), .35))
    d.rounded_rectangle([x0 + 2 * S, y1 - 4 * S, x1 - 2 * S, y1 - S], radius=2 * S, fill=mix(base, (60, 50, 40), .3))
    speckle(d, (x0 + x1) / 2, (y0 + y1) / 2, min(x1 - x0, y1 - y0) * .35, base, rng, n=int((x1 - x0) / (6 * S)) + 3)

def leaf(d, x, y, size, col, angle, rng):
    pts = []
    for i in range(10):
        a = i / 10 * 2 * math.pi
        r = size * (1 + .55 * math.cos(2 * a)) * .5
        pts.append((x + r * math.cos(a + angle), y + r * math.sin(a + angle) * .7))
    d.polygon(pts, fill=jit(col, rng, 14))

def moss(d, x, y, size, rng, cols=((110, 150, 55, 230), (150, 185, 70, 230), (80, 120, 45, 230))):
    for _ in range(rng.randint(5, 9)):
        c = rng.choice(cols); r = rng.uniform(.25, .5) * size
        mx, my = x + rng.uniform(-size, size) * .6, y + rng.uniform(-size, size) * .4
        d.ellipse([mx - r, my - r, mx + r, my + r], fill=jit(c, rng, 12))

def ladder(theme, out):
    rng = random.Random(hash(theme) & 0xffff)
    W, H = 60, 100
    im = canvas(W, H); d = ImageDraw.Draw(im)
    wood = {'autumn': (122, 82, 48), 'mossgate': (96, 78, 48), 'forest': (84, 68, 44), 'cave': (150, 150, 158),
            'road': (150, 104, 60), 'town': (150, 104, 60), 'rivertown': (128, 96, 62), 'meadow': (150, 104, 60),
            'marsh': (120, 92, 58)}[theme]
    rope = theme == 'marsh'
    rail_w = (6 if rope else 9) * S
    for cx in (13 * S, 47 * S):
        box = [cx - rail_w // 2, 0, cx + rail_w // 2, H * S]
        if rope:
            d.rectangle(box, fill=(170, 140, 90))
            for y in range(0, H * S, 4 * S):     # twist
                d.line([(box[0], y), (box[2], y + 4 * S)], fill=(120, 95, 55), width=S)
        else:
            wood_plank(d, box, wood, rng)
    rung_col = mix(wood, (255, 220, 170), .18) if not rope else (140, 100, 60)
    for ry in (18 * S, 68 * S):     # two rungs per tile, 50 px apart -> seamless when tiled
        box = [11 * S, ry - 4 * S, 49 * S, ry + 4 * S]
        wood_plank(d, box, rung_col, rng, grain=False, vertical=False)
        for cx in (13 * S, 47 * S):   # lashing / pegs
            d.ellipse([cx - 3 * S, ry - 3 * S, cx + 3 * S, ry + 3 * S], fill=mix(wood, (30, 20, 10), .5))
    # dressing
    if theme == 'autumn':
        for _ in range(4):
            leaf(d, rng.uniform(8, 52) * S, rng.uniform(5, 95) * S, rng.uniform(9, 14) * S,
                 rng.choice([(200, 70, 30, 240), (235, 120, 35, 240), (240, 190, 90, 240)]), rng.uniform(0, 6.3), rng)
    elif theme in ('mossgate', 'forest'):
        for _ in range(5): moss(d, rng.uniform(8, 52) * S, rng.uniform(5, 95) * S, 9 * S, rng)
    elif theme == 'marsh':
        for _ in range(3): moss(d, rng.uniform(8, 52) * S, rng.uniform(5, 95) * S, 7 * S, rng, cols=((90, 105, 50, 220), (120, 130, 60, 220)))
    elif theme == 'cave':
        for _ in range(4):
            x, y = rng.uniform(8, 52) * S, rng.uniform(5, 95) * S
            d.ellipse([x - 4 * S, y - 4 * S, x + 4 * S, y + 4 * S], fill=(120, 170, 200, 150))
    # soft shadow line under each rung (painted feel)
    sh = Image.new('RGBA', im.size, (0, 0, 0, 0)); ds = ImageDraw.Draw(sh)
    for ry in (18 * S, 68 * S): ds.rectangle([11 * S, ry + 4 * S, 49 * S, ry + 8 * S], fill=(0, 0, 0, 70))
    sh = sh.filter(ImageFilter.GaussianBlur(2 * S))
    sh.putalpha(ImageChops.multiply(sh.getchannel('A'), im.getchannel('A')))
    im = Image.alpha_composite(im, sh)
    shrink(im, W, H).save(out)

def arch(out, stone, mortar, ivy=True, seed=5):
    rng = random.Random(seed)
    W, H = 120, 230
    im = canvas(W, H); d = ImageDraw.Draw(im)
    ox0, ox1, top_r = 4 * S, 116 * S, 56 * S          # outer: 112 wide, round top radius 56
    ix0, ix1, in_r = 28 * S, 92 * S, 32 * S           # opening: 64 wide
    base_y = H * S
    # outer silhouette as a mask
    mask = Image.new('L', im.size, 0); dm = ImageDraw.Draw(mask)
    dm.rectangle([ox0, top_r, ox1, base_y], fill=255); dm.ellipse([ox0, 0, ox1, 2 * top_r], fill=255)
    hole = Image.new('L', im.size, 0); dh = ImageDraw.Draw(hole)
    cy = 26 * S + in_r
    dh.rectangle([ix0, cy, ix1, base_y - 6 * S], fill=255); dh.ellipse([ix0, 26 * S, ix1, 26 * S + 2 * in_r], fill=255)
    mask = ImageChops.subtract(mask, hole)
    # stones: side blocks
    stones = Image.new('RGBA', im.size, mortar + (255,)); ds = ImageDraw.Draw(stones)
    y = base_y
    row = 0
    while y > cy - 10 * S:
        h = rng.uniform(22, 30) * S
        for side in (0, 1):
            x0 = ox0 if side == 0 else ix1; x1 = ix0 if side == 0 else ox1
            if row % 2: split = x0 + (x1 - x0) * rng.uniform(.45, .6); segs = [(x0, split), (split, x1)]
            else: segs = [(x0, x1)]
            for a, b in segs:
                block(ds, [a + S, y - h + S, b - S, y - S], stone, rng)
        y -= h; row += 1
    # voussoirs around the top
    n = 9
    for i in range(n):
        a0 = math.pi + i / n * math.pi; a1 = math.pi + (i + 1) / n * math.pi
        cxa = (ox0 + ox1) / 2; cya = top_r
        pts = []
        for a in (a0 + .015, a1 - .015):
            pts.append((cxa + math.cos(a) * (top_r - 2 * S), cya + math.sin(a) * (top_r - 2 * S)))
        for a in (a1 - .015, a0 + .015):
            pts.append((cxa + math.cos(a) * (in_r + 3 * S), cya + math.sin(a) * (in_r + 3 * S) + (cy - cya) * 0))
        ds.polygon(pts, fill=jit(stone, rng, 9))
        bx = sum(q[0] for q in pts) / 4; by = sum(q[1] for q in pts) / 4
        speckle(ds, bx, by, 9 * S, stone, rng)
    # keystone rune
    ds.ellipse([(W / 2 - 7) * S, 8 * S, (W / 2 + 7) * S, 22 * S], fill=mix(stone, (60, 50, 40), .35))
    stones.putalpha(mask)
    # soft shading: darker to the right/bottom
    shade = Image.new('L', im.size, 0); dsh = ImageDraw.Draw(shade)
    for yy in range(0, im.size[1], S): dsh.line([(0, yy), (im.size[0], yy)], fill=int(60 * yy / im.size[1]))
    dark = Image.new('RGBA', im.size, (40, 30, 20, 255)); dark.putalpha(ImageChops.multiply(shade, mask))
    stones = Image.alpha_composite(stones, dark)
    if ivy:
        dv = ImageDraw.Draw(stones)
        for _ in range(18):
            side = rng.choice([ox0 + 8 * S, ox1 - 8 * S, ix0 + 2 * S, ix1 - 2 * S])
            moss(dv, side + rng.uniform(-6, 6) * S, rng.uniform(40, 220) * S, 8 * S, rng)
        for _ in range(10):
            leaf(dv, rng.uniform(ox0, ox1), rng.uniform(30, 225) * S, 8 * S, rng.choice([(110, 160, 60, 235), (80, 130, 50, 235)]), rng.uniform(0, 6.3), rng)
        stones.putalpha(ImageChops.multiply(stones.getchannel('A'), mask.point(lambda v: 255 if v else 0)))
    # step
    dv = ImageDraw.Draw(stones)
    dv.rounded_rectangle([ix0 - 6 * S, base_y - 8 * S, ix1 + 6 * S, base_y], radius=3 * S, fill=mix(stone, (255, 255, 240), .15))
    shrink(stones, W, H).save(out)

def swirl(out, seed=11):
    rng = random.Random(seed)
    W, H = 90, 190
    im = canvas(W, H)
    cx, cy, rx, ry = W * S / 2, 95 * S, 30 * S, 80 * S
    # misty oval, brighter towards the middle
    base = Image.new('RGBA', im.size, (0, 0, 0, 0)); db = ImageDraw.Draw(base)
    for k in range(12, 0, -1):
        t = k / 12
        db.ellipse([cx - rx * t, cy - ry * t, cx + rx * t, cy + ry * t], fill=(255, 255, 255, int(55 + 110 * (1 - t) ** 1.5)))
    base = base.filter(ImageFilter.GaussianBlur(4 * S))
    # two soft swirl arms
    st = Image.new('RGBA', im.size, (0, 0, 0, 0)); ds = ImageDraw.Draw(st)
    for arm in range(2):
        pts = []
        for i in range(60):
            a = i / 60 * 2.4 * math.pi + arm * math.pi
            r = .15 + .85 * i / 60
            pts.append((cx + math.cos(a) * rx * r * .85, cy + math.sin(a) * ry * r * .85))
        ds.line(pts, fill=(255, 255, 255, 170), width=int(7 * S), joint='curve')
    st = st.filter(ImageFilter.GaussianBlur(4 * S))
    im = Image.alpha_composite(base, st)
    d = ImageDraw.Draw(im)
    for _ in range(26):   # sparkles
        a = rng.uniform(0, 6.3); r = rng.uniform(0, 1)
        x, y = cx + math.cos(a) * rx * r, cy + math.sin(a) * ry * r; s = rng.uniform(1.2, 3) * S
        d.ellipse([x - s, y - s, x + s, y + s], fill=(255, 255, 255, 240))
    shrink(im, W, H).save(out)

def main():
    for theme in ('autumn', 'mossgate', 'forest', 'cave', 'road', 'town', 'rivertown', 'meadow', 'marsh'):
        folder = os.path.join(ART, 'Themes', theme); os.makedirs(folder, exist_ok=True)
        ladder(theme, os.path.join(folder, 'ladder.png'))
    props = os.path.join(ART, 'Props'); os.makedirs(props, exist_ok=True)
    arch(os.path.join(props, 'portal.png'), (232, 218, 182), (168, 150, 118))
    arch(os.path.join(props, 'return_portal.png'), (226, 200, 160), (160, 135, 100), seed=9)
    swirl(os.path.join(props, 'portal_swirl.png'))
    print('props written to', ART)

if __name__ == '__main__':
    main()
