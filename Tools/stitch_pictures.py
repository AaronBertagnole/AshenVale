"""Joins two street pictures side by side: the right part of `west` overlaps the left part of `east`.
A seam is cut through the overlap where the two pictures look most alike, then the pictures are blended
across it band by band (coarse colour over a wide strip, fine detail over a narrow one).

    python Tools/stitch_pictures.py west.png east.png overlap out.png [crop_top]

overlap: how many pixels of the two pictures lie on top of each other (100-250 works for street scenes).
crop_top: rows to cut off the top, e.g. 1 to turn a 725 px tall picture into 724 (Unity compresses
pictures whose sides are multiples of 4). Pick the overlap so the joined width is a multiple of 4 too.
Needs opencv-python and numpy.
"""
import sys
import numpy as np
import cv2

def seam_path(cost):
    """Lowest-cost top-to-bottom path through cost (h x w); returns a column per row."""
    h, w = cost.shape
    acc = cost.copy()
    back = np.zeros((h, w), np.int32)
    for y in range(1, h):
        prev = acc[y - 1]
        left = np.r_[np.inf, prev[:-1]]
        right = np.r_[prev[1:], np.inf]
        stack = np.vstack([left, prev, right])
        choice = stack.argmin(0)
        acc[y] += stack[choice, np.arange(w)]
        back[y] = np.arange(w) + choice - 1
    path = np.zeros(h, np.int32)
    path[-1] = int(acc[-1].argmin())
    for y in range(h - 1, 0, -1):
        path[y - 1] = back[y, path[y]]
    return path

def pyramid_blend(a, b, mask, levels):
    """Blend float images a, b (h x w x 3) with mask (h x w, 1 = a) using Laplacian pyramids."""
    ga, gb, gm = [a], [b], [mask]
    for _ in range(levels):
        ga.append(cv2.pyrDown(ga[-1])); gb.append(cv2.pyrDown(gb[-1])); gm.append(cv2.pyrDown(gm[-1]))
    out = None
    for i in range(levels, -1, -1):
        if i == levels:
            la, lb = ga[i], gb[i]
        else:
            size = (ga[i].shape[1], ga[i].shape[0])
            la = ga[i] - cv2.pyrUp(ga[i + 1], dstsize=size)
            lb = gb[i] - cv2.pyrUp(gb[i + 1], dstsize=size)
        m = gm[i][..., None]
        band = la * m + lb * (1 - m)
        if out is None:
            out = band
        else:
            out = cv2.pyrUp(out, dstsize=(band.shape[1], band.shape[0])) + band
    return out

def main():
    west_path, east_path, ov, out_path = sys.argv[1], sys.argv[2], int(sys.argv[3]), sys.argv[4]
    crop_top = int(sys.argv[5]) if len(sys.argv) > 5 else 0
    west = cv2.imread(west_path, cv2.IMREAD_COLOR)[crop_top:].astype(np.float32)
    east = cv2.imread(east_path, cv2.IMREAD_COLOR)[crop_top:].astype(np.float32)
    h, W = west.shape[:2]
    E = east.shape[1]
    total = W + E - ov

    # Seam through the overlap: colour difference plus difference of edges, slightly smoothed.
    A, B = west[:, W - ov:], east[:, :ov]
    diff = np.abs(A - B).sum(2)
    ga = cv2.Laplacian(cv2.cvtColor(A, cv2.COLOR_BGR2GRAY), cv2.CV_32F)
    gb = cv2.Laplacian(cv2.cvtColor(B, cv2.COLOR_BGR2GRAY), cv2.CV_32F)
    cost = cv2.GaussianBlur(diff + 2 * np.abs(ga - gb), (0, 0), 2)
    # keep the seam off the very edges of the overlap so the blend has room on both sides
    margin = min(24, ov // 6)
    cost[:, :margin] += 1e6; cost[:, ov - margin:] += 1e6
    path = seam_path(cost.astype(np.float64))
    seam = path + (W - ov)                    # output column where east takes over, per row

    # Blend inside a window around the overlap; outside it each picture is copied as it is.
    pad = 384
    x0, x1 = max(0, W - ov - pad), min(total, W + pad)
    def window(img, offset):
        # img placed at `offset` in the output; mirror it past its own edges to fill the window
        cols = np.arange(x0, x1) - offset
        n = img.shape[1]
        cols = np.abs(cols)
        cols = np.where(cols >= n, 2 * (n - 1) - cols, cols)
        return img[:, np.clip(cols, 0, n - 1)]
    wa, wb = window(west, 0), window(east, W - ov)
    xs = np.arange(x0, x1)[None, :]
    mask = (xs < seam[:, None]).astype(np.float32)
    blended = pyramid_blend(wa, wb, mask, 6)

    out = np.zeros((h, total, 3), np.float32)
    out[:, :W] = west
    out[:, W - ov:] = np.where((np.arange(W - ov, total)[None, :, None] < seam[:, None, None]), out[:, W - ov:], east)
    out[:, x0:x1] = blended
    cv2.imwrite(out_path, np.clip(out + .5, 0, 255).astype(np.uint8))
    print('size', total, 'x', h, '; seam columns', seam.min(), '-', seam.max())

if __name__ == '__main__':
    main()
