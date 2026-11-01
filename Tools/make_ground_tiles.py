"""Cuts seamless ground and platform tiles out of a painted map picture (the kind with a path along the bottom).

    python Tools/make_ground_tiles.py <picture.png> <floor_row> <backdrop_width_units> <theme> [x0 width overlap]

floor_row             pixel row (from the top) of the walking line on the painted path
backdrop_width_units  how wide the picture is shown in the game (MapDef.backdropWidth), which sets its scale
theme                 writes Assets/Resources/Art/Themes/<theme>/ground.png and ground_back.png

ground.png       the path below the walking line and the wall under it; its top edge is the walking surface
ground_back.png  a strip of the path just above the walking line, fading out at the top, drawn behind your feet
platform_mid.png, platform_left.png, platform_right.png
                 a thinner slab of the same path and wall for platforms, with rounded ends
Both are resized to the game's 100 px per unit, and the right edge is blended into the left so they tile
without a visible seam. x0 / width / overlap pick the stretch of the picture to use (avoid big unique details).
"""
import os, sys
from PIL import Image

def size4(w, h, scale):
    # Rounded to multiples of 4 so Unity can compress them.
    return max(4, int(round(w * scale / 4.0)) * 4), max(4, int(round(h * scale / 4.0)) * 4)

def seamless(strip, width, overlap):
    """Columns 0..width-1 of strip, with the first `overlap` columns blended from the columns after width."""
    out = strip.crop((0, 0, width, strip.height)).copy()
    for x in range(overlap):
        t = x / float(overlap)
        a = strip.crop((width + x, 0, width + x + 1, strip.height))
        b = strip.crop((x, 0, x + 1, strip.height))
        out.paste(Image.blend(a, b, t), (x, 0))
    return out

def main():
    src, floor_row, units = sys.argv[1], int(sys.argv[2]), float(sys.argv[3])
    theme = sys.argv[4]
    x0, width, overlap = (int(v) for v in sys.argv[5:8]) if len(sys.argv) >= 8 else (450, 1150, 200)
    im = Image.open(src).convert('RGBA')
    scale = 100.0 / (im.width / units)          # picture px -> game px (100 per unit)
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    out_dir = os.path.join(root, 'Assets', 'Resources', 'Art', 'Themes', theme)
    os.makedirs(out_dir, exist_ok=True)

    below = im.crop((x0, floor_row, x0 + width + overlap, im.height))
    ground = seamless(below, width, overlap)
    ground = ground.resize(size4(ground.width, ground.height, scale), Image.LANCZOS)
    ground.save(os.path.join(out_dir, 'ground.png'))

    lip = max(8, int(36 * im.height / 724))     # about the back half of the path
    above = im.crop((x0, floor_row - lip, x0 + width + overlap, floor_row))
    back = seamless(above, width, overlap)
    alpha = Image.new('L', back.size)
    for y in range(back.height):                 # transparent at the top, solid over the lower 40%
        alpha.paste(int(255 * min(1.0, (y / float(back.height)) / 0.6)), (0, y, back.width, y + 1))
    back.putalpha(alpha)
    back = back.resize(size4(back.width, back.height, scale), Image.LANCZOS)
    back.save(os.path.join(out_dir, 'ground_back.png'))
    make_platforms(ground, out_dir)
    print('ground.png', ground.size, ' ground_back.png', back.size, ' scale x%.3f' % scale, '->', out_dir)

def rounded_end(piece, left, top_radius, bottom_radius):
    """Alpha mask that rounds the outer corners (left side if left, else right side) of a cap piece."""
    w, h = piece.size
    mask = Image.new('L', (w, h))
    for y in range(h):
        for x in range(w):
            dx = x if left else w - 1 - x                     # distance from the outer edge
            a = 1.0
            for r, cy in ((top_radius, top_radius), (bottom_radius, h - 1 - bottom_radius)):
                inside = y < cy if cy == top_radius else y > cy
                if inside and dx < r:
                    dist = ((r - dx) ** 2 + (y - cy) ** 2) ** 0.5
                    a = min(a, max(0.0, min(1.0, r - dist + 0.5)))
            mask.putpixel((x, y), int(255 * a * (piece.getpixel((x, y))[3] / 255.0)))
    out = piece.copy()
    out.putalpha(mask)
    return out

def make_platforms(ground, out_dir, depth_px=135, height=72, cap=48):
    """Platforms: the top of the ground tile (path edge and one row of stones), scaled down to a slab."""
    slab = ground.crop((0, 0, ground.width, min(depth_px, ground.height)))
    k = height / float(slab.height)
    slab = slab.resize(size4(slab.width, slab.height, k), Image.LANCZOS)
    slab.save(os.path.join(out_dir, 'platform_mid.png'))
    # The left cap is the end of the tile (so it runs on into the tile's start); the right cap is its start.
    rounded_end(slab.crop((slab.width - cap, 0, slab.width, slab.height)), True, 10, 30).save(os.path.join(out_dir, 'platform_left.png'))
    rounded_end(slab.crop((0, 0, cap, slab.height)), False, 10, 30).save(os.path.join(out_dir, 'platform_right.png'))

if __name__ == '__main__':
    main()
