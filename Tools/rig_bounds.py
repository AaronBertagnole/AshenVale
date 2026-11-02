import sys, numpy as np
from PIL import Image
art=sys.argv[1]
names=["Head","Torso","Pelvis","NearUpperArm","NearForearm","FarUpperArm","FarForearm","NearThigh","NearShin","FarThigh","FarShin","Weapon"]
def pivot(cell, ranger):
    if cell==0: return (.72,.27) if ranger else (.52,.08)
    if cell==1: return (.60,.06)
    if cell==2: return (.50,.80)
    if cell==3: return (.65,.93)
    if cell==5: return (.58,.93)
    if cell in (4,6): return (.42,.94)
    if cell in (8,10): return (.46,.95)
    if cell==11: return (.69,.46) if ranger else (.50,.36)
    return (.50,.95)
def bounds(a, cell, cols=4, rows=3):
    H,W=a.shape
    col,row=cell%cols,cell//cols
    x0=round(col*W/cols); x1=round((col+1)*W/cols)
    y0=H-round((row+1)*H/rows); y1=H-round(row*H/rows)   # unity bottom-up rows
    sub=a[H-y1:H-y0, x0:x1]  # image rows (top-down) matching unity y0..y1
    ys,xs=np.where(sub>64)
    left=x0+xs.min(); right=x0+xs.max()
    # unity y: image row r -> unity y = H-1-r
    top=H-1-(H-y1+ys.min()); bottom=H-1-(H-y1+ys.max())
    left=max(x0,left-2); right=min(x1-1,right+2); bottom=max(y0,bottom-2); top=min(y1-1,top+2)
    return (left,bottom,right-left+1,top-bottom+1)
def sole(a, rect, pv, n=10):
    H=a.shape[0]; x,y,w,h=rect; px,py=pv[0]*w,pv[1]*h
    pts=[]
    for i in range(n):
        xx=int((i+.5)*w/n)
        for yy in range(h):
            r=H-1-(y+yy)
            if a[r,x+xx]>64:
                pts.append(((xx+.5-px)/h,(yy+.5-py)/h)); break
    return pts
out=[]
for key,ranger in (("ranger",True),("warrior",False)):
    a=np.array(Image.open(f"{art}/{'Ranger' if ranger else 'Warrior'}_Parts.png").convert("RGBA"))[:,:,3]
    print(f"            // {key}: part rects in atlas pixels (x, y from the bottom, width, height), pivot (0..1)")
    for i,n in enumerate(names):
        b=bounds(a,i); pv=pivot(i,ranger)
        print(f"            new RigPart({b[0]}, {b[1]}, {b[2]}, {b[3]}, {pv[0]:.2f}f, {pv[1]:.2f}f), // {n}")
    for cell,label in ((8,"nearSole"),(10,"farSole")):
        pts=sole(a,bounds(a,cell),pivot(cell,ranger))
        print(f"            // {label}: "+", ".join(f"{p[0]:.3f}f, {p[1]:.3f}f" for p in pts))
    if not ranger:
        c=np.array(Image.open(f"{art}/Warrior_Cape.png").convert("RGBA"))[:,:,3]
        b=bounds(c,0,1,1); print(f"            // cape: new RigPart({b[0]}, {b[1]}, {b[2]}, {b[3]}, .82f, .95f)")
