from PIL import Image, ImageDraw

SIZE = 1024
BLUE = (10, 132, 255, 255)
WHITE = (255, 255, 255, 255)
BG_TOP = (44, 44, 46)
BG_BOTTOM = (20, 20, 22)

SS = 4
S = SIZE * SS

img = Image.new("RGBA", (S, S), (0, 0, 0, 0))

grad = Image.new("RGB", (1, S))
for y in range(S):
    t = y / (S - 1)
    grad.putpixel((0, 0 if False else 0), BG_TOP)
for y in range(S):
    t = y / (S - 1)
    r = int(BG_TOP[0] + (BG_BOTTOM[0] - BG_TOP[0]) * t)
    g = int(BG_TOP[1] + (BG_BOTTOM[1] - BG_TOP[1]) * t)
    b = int(BG_TOP[2] + (BG_BOTTOM[2] - BG_TOP[2]) * t)
    grad.putpixel((0, y), (r, g, b))
grad = grad.resize((S, S))

mask = Image.new("L", (S, S), 0)
md = ImageDraw.Draw(mask)
margin = int(0.08 * S)
radius = int(0.225 * S)
md.rounded_rectangle([margin, margin, S - margin, S - margin], radius=radius, fill=255)
img.paste(grad, (0, 0), mask)

draw = ImageDraw.Draw(img)

cx = S / 2
cy = S / 2
scale = S / 1024 * 30

def t(x, y):
    return (cx + (x - 12) * scale, cy + (y - 12) * scale)

def cubic(p0, p1, p2, p3, n=48):
    pts = []
    for i in range(n + 1):
        u = i / n
        mu = 1 - u
        x = mu**3 * p0[0] + 3 * mu**2 * u * p1[0] + 3 * mu * u**2 * p2[0] + u**3 * p3[0]
        y = mu**3 * p0[1] + 3 * mu**2 * u * p1[1] + 3 * mu * u**2 * p2[1] + u**3 * p3[1]
        pts.append((x, y))
    return pts

shield = []
shield.append(t(12, 1))
shield.append(t(3, 5))
shield.append(t(3, 11))
shield += [t(px, py) for px, py in cubic((3, 11), (3, 16.55), (6.84, 21.74), (12, 23))]
shield += [t(px, py) for px, py in cubic((12, 23), (17.16, 21.74), (21, 16.55), (21, 11))]
shield.append(t(21, 5))
shield.append(t(12, 1))
draw.polygon(shield, fill=BLUE)

def rr(x0, y0, x1, y1, rad, fill):
    a = t(x0, y0)
    b = t(x1, y1)
    draw.rounded_rectangle([a[0], a[1], b[0], b[1]], radius=rad * scale, fill=fill)

shk_out_l, shk_out_r = 9.3, 14.7
shk_top = 6.6
shk_bot = 13.0
rr(shk_out_l, shk_top, shk_out_r, shk_bot, (shk_out_r - shk_out_l) / 2, WHITE)

inset = 1.15
rr(shk_out_l + inset, shk_top + inset, shk_out_r - inset, shk_bot + 2,
   (shk_out_r - shk_out_l - 2 * inset) / 2, BLUE)

rr(8.2, 10.6, 15.8, 17.2, 1.4, WHITE)

kc = t(12, 13.2)
kr = 1.05 * scale
draw.ellipse([kc[0] - kr, kc[1] - kr, kc[0] + kr, kc[1] + kr], fill=BLUE)
kb0 = t(11.55, 13.2)
kb1 = t(12.45, 15.6)
draw.rounded_rectangle([kb0[0], kb0[1], kb1[0], kb1[1]], radius=0.35 * scale, fill=BLUE)

img = img.resize((SIZE, SIZE), Image.LANCZOS)
img.save("build/icon_1024.png")
print("wrote build/icon_1024.png")
