"""Earthen ledges for a theme whose painted path is too flat to read as a platform (a marsh's mud, say).

    python Tools/make_ledge_tiles.py <picture.png> <top_row> <rows> <theme> [x0 width overlap]

Takes `rows` pixel rows of the path starting at top_row for the walking surface, then builds a 72 px slab from it:
a lit lip along the top, the earth darkening underneath, and a ragged underside with hanging clumps (cut from
periodic waves, so the middle tile repeats without a seam). Writes platform_mid / _left / _right into the theme.
"""
import math, os, sys
from PIL import Image, ImageEnhance

H, CAP, WIDTH = 72, 48, 928

def main():
    src, top, rows, theme = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
    x0, width, overlap = (int(v) for v in sys.argv[5:8]) if len(sys.argv) >= 8 else (500, 900, 200)
    im = Image.open(src).convert('RGB')
    strip = im.crop((x0, top, x0 + width + overlap, top + rows))
    tile = strip.crop((0, 0, width, rows)).copy()
    for x in range(overlap):                       # blend the end into the start so it repeats
        t = x / float(overlap)
        tile.paste(Image.blend(strip.crop((width + x, 0, width + x + 1, rows)), strip.crop((x, 0, x + 1, rows)), t), (x, 0))
    tile = tile.resize((WIDTH, H), Image.LANCZOS)
    px = tile.load()
    out = Image.new('RGBA', (WIDTH, H))
    op = out.load()
    w = 2 * math.pi / WIDTH
    for x in range(WIDTH):
        # Underside: between 50 and 72 px deep, with a few longer hanging clumps.
        depth = 60 + 5 * math.sin(7 * w * x + .6) + 4 * math.sin(19 * w * x + 2.1) + 2.5 * math.sin(43 * w * x + 4.0)
        depth += 6 * max(0.0, math.sin(11 * w * x + 1.3)) ** 6
        depth = min(H, depth)
        for y in range(H):
            if y >= depth: continue
            r, g, b = px[x, y]
            if y < 5: k = 1.22 - .05 * y                          # the lit lip
            else: k = 1.0 - .72 * min(1.0, (y - 5) / 50.0) ** .8   # earth falling into shadow
            edge = min(1.0, (depth - y) / 2.0)                    # soft bottom edge
            op[x, y] = (min(255, int(r * k)), min(255, int(g * k)), min(255, int(b * k * .96)), int(255 * edge))
    out_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'Assets', 'Resources', 'Art', 'Themes', theme)
    out.save(os.path.join(out_dir, 'platform_mid.png'))
    end(out.crop((WIDTH - CAP, 0, WIDTH, H)), True).save(os.path.join(out_dir, 'platform_left.png'))
    end(out.crop((0, 0, CAP, H)), False).save(os.path.join(out_dir, 'platform_right.png'))
    print('ledges ->', out_dir)

def end(cap, left):
    """Rounds the outer top corner and tapers the underside up toward the outer edge."""
    p = cap.load()
    for x in range(CAP):
        d = x if left else CAP - 1 - x                 # distance from the outer edge
        cut_top = max(0, 10 - int(math.sqrt(max(0, 100 - (10 - d) ** 2)))) if d < 10 else 0
        keep = H * (.45 + .55 * min(1.0, d / 34.0) ** .6)
        for y in range(H):
            if y < cut_top or y > keep: p[x, y] = (0, 0, 0, 0)
    return cap

if __name__ == '__main__':
    main()
