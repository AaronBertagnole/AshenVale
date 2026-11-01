"""Makes a new theme's tiles by recolouring another theme's (for example autumn leaves into spring moss).

    python Tools/recolor_theme.py <from_theme> <to_theme> <hue_shift_degrees> [amount] [min_saturation]

Only warm, saturated pixels (reds, oranges, golds: leaves) are shifted, by up to hue_shift_degrees;
grey stone and dark shadows stay as they are. amount (0..1, default 1) blends between old and new colours.
"""
import colorsys, os, sys
from PIL import Image

def recolor(im, shift, amount, min_sat):
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
            deg = h * 360.0
            warm = deg < 65 or deg > 335                       # red .. gold
            if not warm or s < min_sat:
                continue
            weight = min(1.0, (s - min_sat) / 0.25) * amount    # gentle near the cut-off
            nh = ((deg + shift * weight) % 360.0) / 360.0
            ns = s * (1.0 - 0.25 * weight)                       # moss is a little less vivid than autumn leaves
            nr, ng, nb = colorsys.hsv_to_rgb(nh, ns, v * (1.0 - 0.12 * weight))
            px[x, y] = (int(nr * 255), int(ng * 255), int(nb * 255), a)
    return im

def main():
    src, dst, shift = sys.argv[1], sys.argv[2], float(sys.argv[3])
    amount = float(sys.argv[4]) if len(sys.argv) > 4 else 1.0
    min_sat = float(sys.argv[5]) if len(sys.argv) > 5 else 0.3
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    themes = os.path.join(root, 'Assets', 'Resources', 'Art', 'Themes')
    os.makedirs(os.path.join(themes, dst), exist_ok=True)
    for name in sorted(os.listdir(os.path.join(themes, src))):
        if not name.endswith('.png'):
            continue
        im = Image.open(os.path.join(themes, src, name)).convert('RGBA')
        recolor(im, shift, amount, min_sat).save(os.path.join(themes, dst, name))
        print(dst + '/' + name, im.size)

if __name__ == '__main__':
    main()
