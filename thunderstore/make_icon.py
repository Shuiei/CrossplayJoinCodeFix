# Draws thunderstore/icon.png (256x256): a server rack with a green "back online" check inside
# a retry arrow. python3 thunderstore/make_icon.py   (needs Pillow)
import math, os
from PIL import Image, ImageDraw

S = 4  # draw at 4x, then shrink for smooth edges
W = 256 * S
img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
d = ImageDraw.Draw(img)
d.rounded_rectangle((8 * S, 8 * S, 248 * S, 248 * S), radius=40 * S, fill=(24, 32, 48, 255), outline=(70, 90, 120, 255), width=4 * S)
# Server rack: three units with status lights.
for i, y in enumerate((56, 100, 144)):
    d.rounded_rectangle((44 * S, y * S, 150 * S, (y + 34) * S), radius=6 * S, fill=(52, 64, 86, 255), outline=(96, 116, 146, 255), width=3 * S)
    d.ellipse((58 * S, (y + 12) * S, 68 * S, (y + 22) * S), fill=(90, 220, 120, 255) if i < 2 else (240, 180, 60, 255))
    for x in range(84, 140, 12):
        d.rectangle((x * S, (y + 14) * S, (x + 6) * S, (y + 20) * S), fill=(120, 140, 170, 255))
# Retry arrow around a check mark, bottom right.
cx, cy, r = 176 * S, 172 * S, 50 * S
d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(24, 32, 48, 255))
d.arc((cx - r + 6 * S, cy - r + 6 * S, cx + r - 6 * S, cy + r - 6 * S), start=-60, end=250, fill=(110, 190, 255, 255), width=10 * S)
a = math.radians(-60)
tip = (cx + (r - 11 * S) * math.cos(a), cy + (r - 11 * S) * math.sin(a))
d.polygon([(tip[0] + 18 * S, tip[1] - 4 * S), (tip[0] - 10 * S, tip[1] - 16 * S), (tip[0] - 2 * S, tip[1] + 16 * S)], fill=(110, 190, 255, 255))
d.line([(cx - 20 * S, cy + 2 * S), (cx - 4 * S, cy + 18 * S), (cx + 24 * S, cy - 16 * S)], fill=(90, 220, 120, 255), width=11 * S, joint="curve")
img.resize((256, 256), Image.LANCZOS).save(os.path.join(os.path.dirname(os.path.abspath(__file__)), "icon.png"))
