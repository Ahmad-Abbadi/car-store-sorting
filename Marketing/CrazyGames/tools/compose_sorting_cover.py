"""Compose one cover identity from locally rendered, unchanged Unity game art."""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'SourceRender'

def linear(rgb):
    return np.where(rgb <= .04045, rgb / 12.92, ((rgb + .055) / 1.055) ** 2.4)

def srgb(rgb):
    return np.where(rgb <= .0031308, 12.92 * rgb, 1.055 * np.maximum(rgb, 0) ** (1 / 2.4) - .055)

def extract(name):
    black = linear(np.asarray(Image.open(SOURCE / f'{name}-black.png').convert('RGB'), dtype=np.float32) / 255)
    white = linear(np.asarray(Image.open(SOURCE / f'{name}-white.png').convert('RGB'), dtype=np.float32) / 255)
    alpha = np.clip(1 - np.median(white - black, axis=2), 0, 1)
    alpha[alpha < .015] = 0
    color = srgb(np.clip(black / np.maximum(alpha[:, :, None], .001), 0, 1))
    result = Image.fromarray(np.uint8(np.clip(np.dstack((color, alpha)), 0, 1) * 255), 'RGBA')
    bbox = result.getchannel('A').getbbox()
    if not bbox:
        raise ValueError(f'Empty Unity render: {name}')
    result = result.crop(bbox)
    result.save(SOURCE / f'{name}-cutout.png')
    return result

layers = {name: extract(name) for name in ['pile', 'shelves', 'hero', 'gold']}
# Typeset the actual title in the game's existing rounded UI font and brand colors.
# This avoids clipping the tray illustration attached to the source logo.
font_path = ROOT.parents[1] / 'Assets/00_Project/04_ِArt/UI/Fonts/Fredoka-SemiBold.ttf'
title = Image.new('RGBA', (1400, 800))
pen = ImageDraw.Draw(title)
y = 35
for word, colors in [('TOY CAR', ((255, 241, 110), (255, 176, 13))), ('SORT', ((119, 245, 251), (0, 175, 218)))]:
    size = 250
    while ImageFont.truetype(str(font_path), size).getlength(word) < 1260:
        size += 1
    font = ImageFont.truetype(str(font_path), size - 1)
    box = pen.textbbox((0, 0), word, font=font)
    x = (1400 - (box[2] - box[0])) // 2 - box[0]
    at = (x, y - box[1])
    pen.text((at[0], at[1] + 13), word, font=font, fill='#11344c', stroke_width=28, stroke_fill='#11344c')
    pen.text(at, word, font=font, fill='#11344c', stroke_width=25, stroke_fill='#fff9db')
    pen.text(at, word, font=font, fill='#11344c', stroke_width=18, stroke_fill='#102e48')
    mask = Image.new('L', title.size)
    ImageDraw.Draw(mask).text(at, word, font=font, fill=255)
    gradient = np.zeros((title.height, title.width, 3), np.uint8)
    for row in range(title.height):
        mix = min(1, max(0, (row - y) / max(1, box[3] - box[1])))
        gradient[row, :] = np.array(colors[0]) * (1 - mix) + np.array(colors[1]) * mix
    title.paste(Image.fromarray(gradient), (0, 0), mask)
    y += box[3] - box[1] + 30
title = title.crop(title.getchannel('A').getbbox())
layers['title'] = title

def backdrop(w, h):
    y, x = np.mgrid[0:h, 0:w]
    t = (y / h)[:, :, None]
    top = np.array([249, 245, 224.])
    bottom = np.array([167, 211, 205.])
    rgb = top * (1 - t) + bottom * t
    light = np.exp(-(((x / w - .47) / .7) ** 2 + ((y / h - .40) / .6) ** 2))[:, :, None]
    rgb = np.clip(rgb + light * np.array([6., 4., -2.]), 0, 255)
    return Image.fromarray(np.uint8(rgb), 'RGB').convert('RGBA')

def paste(canvas, name, x, y, width, shadow=True):
    source = layers[name]
    item = source.resize((width, round(source.height * width / source.width)), Image.Resampling.LANCZOS)
    max_height = canvas.height - y - 22
    if item.height > max_height:
        item = item.resize((round(item.width * max_height / item.height), max_height), Image.Resampling.LANCZOS)
    if shadow:
        shade = Image.new('RGBA', canvas.size)
        shape = Image.new('RGBA', item.size, (10, 38, 46, 0))
        shape.putalpha(item.getchannel('A').point(lambda a: round(a * .20)))
        shade.alpha_composite(shape, (x + round(width * .01), y + round(width * .024)))
        shade = shade.filter(ImageFilter.GaussianBlur(max(4, width * .014)))
        canvas.alpha_composite(shade)
    canvas.alpha_composite(item, (x, y))
    return (x, y, x + item.width, y + item.height)

layouts = {
    'Landscape_1920x1080': ((1920, 1080), [
        ('shelves', 890, 130, 970), ('gold', 1630, 865, 210),
        ('pile', 32, 470, 1120), ('hero', 755, 620, 650), ('title', 68, 25, 745)]),
    'Portrait_800x1200': ((800, 1200), [
        ('shelves', 95, 370, 660), ('gold', 628, 747, 130),
        ('pile', 20, 785, 730), ('hero', 345, 895, 425), ('title', 36, 25, 728)]),
    'Square_800x800': ((800, 800), [
        ('shelves', 245, 280, 535), ('gold', 695, 666, 85),
        ('pile', 14, 475, 675), ('hero', 350, 550, 350), ('title', 80, 16, 640)]),
}

validation = {}
for name, (size, placement) in layouts.items():
    canvas = backdrop(*size)
    boxes = {}
    for item, x, y, width in placement:
        boxes[item] = paste(canvas, item, x, y, width, shadow=item != 'title')
    output = ROOT / f'Cover_{name}.png'
    canvas.convert('RGB').save(output, compress_level=9)
    thumb = canvas.convert('RGB')
    thumb.thumbnail((360, 300), Image.Resampling.LANCZOS)
    thumb.save(SOURCE / f'Preview_{name}.png')
    validation[name] = {'size': list(Image.open(output).size), 'element_bounds': boxes}

# A compact, unlabelled contact sheet for reduced-size visual inspection.
thumbs = [Image.open(SOURCE / f'Preview_{name}.png').convert('RGB') for name in layouts]
sheet = Image.new('RGB', (sum(i.width for i in thumbs) + 48, max(i.height for i in thumbs) + 32), '#e6eae4')
x = 12
for im in thumbs:
    sheet.paste(im, (x, 16))
    x += im.width + 12
sheet.save(SOURCE / 'Thumbnail_Check.png')
(SOURCE / 'dimensions.json').write_text(json.dumps(validation, indent=2), encoding='utf-8')
print(json.dumps({name: v['size'] for name, v in validation.items()}))
