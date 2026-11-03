"""Checks the map layouts in Assets/Scripts/Data (MapDefs.cs and Maps/*.cs) without opening Unity.

    python Tools/check_maps.py            check every map
    python Tools/check_maps.py --print    also print the layouts

Markers: @ start, $ merchant, ^ waystone, & town return portal, % smithy, A-Z portals, a-z spawn points.
Checks: every ladder has a floor at both ends, every marker stands on a floor, no ladder cuts a platform,
every portal letter is linked and links are two-way, towns have @ $ ^ &, spawn letters are in the legend,
and every floor can really be reached by a slow hero (speed 5.3) using a copy of the game's movement rules
(World.Move): jumping, walking off edges, Down + Jump drop-through and ladders.
If you change jump, gravity or climb numbers in Tuning.cs, change them here too.
"""
import os, re, sys

G = -3.15            # Tuning.Ground
JUMP = 10.8          # Tuning.JumpSpeed
GRAVITY = -24.0      # Tuning.Gravity
CLIMB = 4.2          # Tuning.ClimbSpeed
SPEED = 5.3          # slowest hero
DT = 1 / 60.0

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATA = os.path.join(ROOT, 'Assets', 'Scripts', 'Data')   # MapDefs.cs plus Maps/<Region>.cs

# ------------------------------------------------------------------ read MapDefs.cs

def read_source():
    src = ''
    for folder, _, files in sorted(os.walk(DATA)):
        for f in sorted(files):
            if f.endswith('.cs'): src += open(os.path.join(folder, f), encoding='utf-8').read() + '\n'
    layouts = {name: text for name, text in re.findall(r'const string (\w+) = @"(.*?)";', src, re.S)}
    ids = {}
    m = re.search(r'public const int (Emberfields[^;]*);', src)
    for part in re.sub(r'//[^\n]*', '', m.group(1)).split(','):
        k, v = part.split('=')
        ids[k.strip()] = int(v)
    maps = {}
    for call in split_builds(src):
        args = re.match(r'\s*(\w+)\s*,\s*"([^"]*)"\s*,\s*"([^"]*)"\s*,\s*MapKind\.(\w+)\s*,\s*(\w+)', call)
        key, name, theme, kind, layout = args.groups()
        legend = {c: mon for c, mon in re.findall(r"MapLayout\.S\('(.)',\s*Monsters\.(\w+)\)", call)}
        links = {k: (t, tk) for k, t, tk in re.findall(r"MapLayout\.L\('(.)',\s*(\w+),\s*'(.)'\)", call)}
        maps[key] = dict(name=name, kind=kind.lower(), text=layouts[layout], legend=legend, links=links, id=ids[key])
    return maps

def split_builds(src):
    calls, i = [], 0
    while True:
        i = src.find('MapLayout.Build(', i)
        if i < 0: return calls
        j = i + len('MapLayout.Build('); depth = 1
        while depth:
            depth += {'(': 1, ')': -1}.get(src[j], 0); j += 1
        calls.append(src[i + len('MapLayout.Build('):j - 1]); i = j

# ------------------------------------------------------------------ parse a layout (same rules as MapLayout.Build)

def parse(text):
    lines = [l.strip() for l in text.split('\n') if l.strip()]
    rows, cols = len(lines), max(len(l) for l in lines)
    grid = [[(lines[rows - 1 - r][c] if c < len(lines[rows - 1 - r]) else '.') for c in range(cols)] for r in range(rows)]
    plats, ladders, marks = [], [], []
    for r in range(1, rows):
        c = 0
        while c < cols:
            if grid[r][c] != '=': c += 1; continue
            s = c
            while c < cols and grid[r][c] == '=': c += 1
            plats.append((s, c, G + r))
    for c in range(cols):
        r = 1
        while r < rows:
            if grid[r][c] != '|': r += 1; continue
            low = r
            while r < rows and grid[r][c] == '|': r += 1
            ladders.append((c + .5, G + low - 1, G + r))
    for r in range(1, rows):
        for c in range(cols):
            ch = grid[r][c]
            if ch in '.=#|': continue
            under = grid[r - 1][c]
            floor = r - 1
            while floor > 0 and grid[floor][c] != '=': floor -= 1
            marks.append((ch, c, r, c + .5, G + floor, under in '=#'))
    return dict(grid=grid, rows=rows, cols=cols, plats=plats, ladders=ladders, marks=marks, minX=.5, maxX=cols - .5)

# ------------------------------------------------------------------ movement (copy of World.Move, no knockback or dash)

def player(x, y): return dict(x=x, y=y, vy=0.0, grounded=True, climbing=False, floor=y, drop=0.0, dropFloor=0.0)

def move(p, d, axis, vert, jump):
    p['drop'] = max(0.0, p['drop'] - DT)
    near = None
    for (x, b, t) in d['ladders']:
        if abs(p['x'] - x) < .68 and b - .35 <= p['y'] <= t + .45: near = (x, b, t); break
    if not p['climbing'] and near:
        if (vert > .1 and p['y'] < near[2] - .05) or (vert < -.1 and p['y'] > near[1] + .05):
            p.update(climbing=True, grounded=False, vy=0.0, x=near[0])
    if p['climbing'] and (not near or jump):
        p['climbing'] = False
        if jump: p.update(vy=JUMP, grounded=False)
    if not p['climbing'] and p['grounded'] and jump:
        if vert < -.1:
            if p['floor'] > G + .05: p.update(drop=.25, dropFloor=p['floor'], grounded=False, vy=0.0)
        else: p.update(vy=JUMP, grounded=False)
    p['x'] = min(max(p['x'] + (0 if p['climbing'] else axis * SPEED * DT), d['minX']), d['maxX'])
    if p['climbing']:
        p['y'] = min(max(p['y'] + vert * CLIMB * DT, near[1]), near[2])
        if p['y'] >= near[2] - .04 and vert > 0: p.update(climbing=False, grounded=True, floor=near[2], y=near[2])
        elif p['y'] <= near[1] + .04 and vert < 0: p.update(climbing=False, grounded=True, floor=near[1], y=near[1])
        return
    prev = p['y']
    if not p['grounded']: p['vy'] += GRAVITY * DT; p['y'] += p['vy'] * DT
    if p['vy'] <= 0:
        landing = G
        for (l, r, t) in d['plats']:
            if p['drop'] > 0 and abs(t - p['dropFloor']) < .05: continue
            if l + .16 <= p['x'] <= r - .16 and prev >= t - .03 and p['y'] <= t and t > landing: landing = t
        if p['y'] <= landing and prev >= landing - .03: p.update(y=landing, vy=0.0, grounded=True, floor=landing)
    if p['grounded']:
        ok = abs(p['floor'] - G) < .01 or any(abs(p['floor'] - t) < .05 and l + .16 <= p['x'] <= r - .16 for (l, r, t) in d['plats'])
        if not ok: p.update(grounded=False, vy=0.0)

def floors(d): return [('ground', 0, d['cols'], G)] + [('platform cols %d-%d row %d' % (l, r - 1, round(t - G)), l, r, t) for (l, r, t) in d['plats']]

def floor_of(d, p):
    if not p['grounded']: return None
    for i, (_, l, r, t) in enumerate(floors(d)):
        if abs(p['floor'] - t) < .05 and (i == 0 or l - .2 <= p['x'] <= r + .2): return i
    return None

def unreachable(d):
    S = floors(d); seen = {0}; todo = [0]
    def fly(p, axis, vert, jump):
        move(p, d, axis, vert, jump)
        for _ in range(240):
            if p['grounded'] or p['climbing']: break
            move(p, d, axis, 0, False)
        return floor_of(d, p)
    while todo:
        i = todo.pop(); _, l, r, t = S[i]; found = set()
        for k in range(int(max(l, d['minX']) * 2) + 1, int(min(r, d['maxX']) * 2)):
            x = k / 2
            for axis in (-1, 0, 1):
                found.add(fly(player(x, t), axis, 0, True))
                found.add(fly(player(x, t), axis, -1, True))
            for axis in (-1, 1):
                p = player(x, t)
                for _ in range(200):
                    move(p, d, axis, 0, False)
                    if not p['grounded']: break
                found.add(fly(p, axis, 0, False))
        for (lx, b, top) in d['ladders']:
            for (y0, v) in ((b, 1), (top, -1)):
                if abs(y0 - t) < .05 and l <= lx <= r:
                    p = player(lx, t)
                    for _ in range(400): move(p, d, 0, v, False)
                    found.add(floor_of(d, p))
        for j in found:
            if j is not None and j not in seen: seen.add(j); todo.append(j)
    return [S[i][0] for i in range(len(S)) if i not in seen]

# ------------------------------------------------------------------ checks

def check(key, m, maps):
    d = parse(m['text']); problems = []
    grid = d['grid']
    S = floors(d)
    for (x, b, t) in d['ladders']:
        for y, end in ((b, 'bottom'), (t, 'top')):
            if not any(abs(sy - y) < .01 and l <= x <= r for (_, l, r, sy) in S):
                problems.append('ladder at col %d has no floor at its %s' % (int(x), end))
    for r in range(1, d['rows']):
        for c in range(1, d['cols'] - 1):
            if grid[r][c] == '|' and grid[r][c - 1] == '=' and grid[r][c + 1] == '=':
                problems.append('ladder at col %d cuts the platform on row %d' % (c, r))
    letters = set()
    for (ch, c, r, x, y, standing) in d['marks']:
        if not standing: problems.append('%s at col %d row %d is not standing on a floor' % (ch, c, r))
        if ch.islower() and ch not in m['legend']: problems.append('spawn letter %s has no monster in the legend' % ch)
        if ch.isupper():
            letters.add(ch)
            if ch not in m['links']: problems.append('portal %s is not linked' % ch)
    for k, (target, tk) in m['links'].items():
        if k not in letters: problems.append('link %s has no portal in the layout' % k)
        back = maps.get(target, {}).get('links', {}).get(tk)
        if back != (key, k): problems.append('portal %s -> %s %s does not lead back' % (k, target, tk))
    if m['kind'] == 'town':
        for need, what in (('@', 'start'), ('$', 'merchant'), ('^', 'waystone'), ('&', 'return portal')):
            if not any(mk[0] == need for mk in d['marks']): problems.append('town has no %s (%s)' % (what, need))
    problems += ['cannot reach ' + f for f in unreachable(d)]
    return d, problems

def main():
    maps = read_source(); ok = True
    for key, m in sorted(maps.items(), key=lambda kv: kv[1]['id']):
        d, problems = check(key, m, maps)
        spawns = sum(1 for mk in d['marks'] if mk[0].islower())
        print('%-12s %3dx%-3d %-4s spawns %2d  %s' % (m['name'], d['cols'], d['rows'], m['kind'], spawns, 'OK' if not problems else 'PROBLEMS'))
        for p in problems: print('    - ' + p)
        ok = ok and not problems
        if '--print' in sys.argv: print('\n'.join(l.strip() for l in m['text'].split('\n') if l.strip()) + '\n')
    sys.exit(0 if ok else 1)

if __name__ == '__main__':
    main()
