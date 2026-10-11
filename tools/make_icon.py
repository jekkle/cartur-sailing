"""Draws the 256x256 Thunderstore icon: a longship's square sail over waves.

Thunderstore rejects anything that is not exactly 256x256 PNG, and there is no art asset to crop
from, so the icon is drawn - same approach and background as the other Cartur icons.

    python tools/make_icon.py
"""
import os

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(os.path.dirname(HERE), "package", "icon.png")

SIZE = 256
SCALE = 4

BACKGROUND = (38, 34, 30, 255)
SAIL = (226, 214, 188, 255)
STRIPE = (168, 52, 40, 255)
SAIL_EDGE = (96, 82, 62, 255)
WOOD = (138, 96, 58, 255)
WOOD_EDGE = (74, 48, 26, 255)
WAVE = (70, 116, 150, 255)
WAVE_EDGE = (34, 60, 84, 255)

s = SCALE
image = Image.new("RGBA", (SIZE * s, SIZE * s), BACKGROUND)
draw = ImageDraw.Draw(image)


def poly(points, fill, edge, width=5):
    draw.polygon([(x * s, y * s) for x, y in points], fill=fill, outline=edge, width=width * s)


# Mast and yard.
draw.line([(128 * s, 24 * s), (128 * s, 196 * s)], fill=WOOD_EDGE, width=14 * s)
draw.line([(128 * s, 24 * s), (128 * s, 196 * s)], fill=WOOD, width=8 * s)
draw.line([(50 * s, 44 * s), (206 * s, 44 * s)], fill=WOOD_EDGE, width=12 * s)
draw.line([(50 * s, 44 * s), (206 * s, 44 * s)], fill=WOOD, width=6 * s)

# The sail, bellied by the wind, with two red stripes.
sail = [(56, 50), (200, 50), (210, 160), (128, 172), (46, 160)]
poly(sail, SAIL, SAIL_EDGE)
poly([(84, 52), (108, 52), (106, 168), (80, 164)], STRIPE, STRIPE, 1)
poly([(148, 52), (172, 52), (176, 164), (150, 168)], STRIPE, STRIPE, 1)
poly(sail, None, SAIL_EDGE)

# Hull line and waves.
poly([(30, 188), (226, 188), (204, 214), (52, 214)], WOOD, WOOD_EDGE)
poly([(0, 220), (40, 208), (84, 222), (128, 208), (172, 222), (216, 208), (256, 220),
      (256, 256), (0, 256)], WAVE, WAVE_EDGE)

image = image.resize((SIZE, SIZE), Image.LANCZOS)
image.save(OUT)
print("wrote", OUT, image.size)
