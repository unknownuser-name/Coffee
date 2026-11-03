"""Package unmodified imagegen coffee-bag cutouts for Unity 2.5D scenes.

Requires Pillow and numpy. No image pixels are retouched, erased or repainted.
Only the preview contact sheet resizes and composites copies of the originals.
"""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import hashlib
import io
import json
from pathlib import Path, PurePosixPath
import re
import shutil
import tarfile

import numpy as np
from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
UNITY_ROOT = ROOT / "unity"
ASSET_ROOT = UNITY_ROOT / "Assets" / "CoffeeBagsRealistic"
SPRITES = ASSET_ROOT / "Sprites"
SOURCE = ROOT.parent / "coffee_bags" / "catalog" / "products.v2.json"
PACKAGE = ROOT / "CoffeeBags_Realistic_Unity.unitypackage"


def write_json(path: Path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def guid(relative_path: str) -> str:
    return hashlib.md5(("coffee-bags-realistic-v1/" + relative_path).encode()).hexdigest()


def inspect_image(path: Path, product: dict) -> dict:
    with Image.open(path) as source:
        if source.format != "PNG" or source.mode != "RGBA":
            raise ValueError("Expected a true-alpha RGBA PNG; no background conversion is performed")
        source.load()
        width, height = source.size
        if min(width, height) < 512:
            raise ValueError("Image is smaller than 512 pixels on one edge")
        alpha = np.asarray(source.getchannel("A"))
        if int(alpha.min()) != 0 or int(alpha.max()) < 240:
            raise ValueError("Image needs fully transparent pixels and a substantially opaque product")
        corner = min(12, width // 20, height // 20)
        # Built-in generation may leave one 8-bit alpha step in a few otherwise
        # clear pixels. Preserve the original image and report that precision.
        corner_alpha_max = max(int(a.max()) for a in (alpha[:corner, :corner], alpha[:corner, -corner:],
                                  alpha[-corner:, :corner], alpha[-corner:, -corner:]))
        if corner_alpha_max > 1:
            raise ValueError("Image corners exceed the 1/255 alpha precision tolerance")
        occupied = float((alpha > 8).mean())
        if not .04 < occupied < .95:
            raise ValueError(f"Implausible foreground coverage: {occupied:.3f}")
        yy, xx = np.where(alpha > 8)
        bbox = [int(xx.min()), int(yy.min()), int(xx.max()) + 1, int(yy.max()) + 1]
        if bbox[0] < 2 or bbox[1] < 2 or bbox[2] > width - 2 or bbox[3] > height - 2:
            raise ValueError("Foreground touches the canvas edge; product may be cropped")
    dimensions = product.get("dimensions_m", [.135, .230, .070])
    world_height = float(dimensions[1]) if isinstance(dimensions, list) and len(dimensions) >= 2 else .230
    if not .1 <= world_height <= .5:
        world_height = .230
    pixels_per_unit = round((bbox[3] - bbox[1]) / world_height, 4)
    # Unity pivots are normalized from the lower-left; place it at the visible
    # bag's bottom centre without cropping or altering the delivered PNG.
    pivot = [round((bbox[0] + bbox[2]) / (2 * width), 8), round((height - bbox[3]) / height, 8)]
    return {
        "id": product["slug"], "sprite": f"Assets/CoffeeBagsRealistic/Sprites/{path.name}",
        "size_px": [width, height], "foreground_bbox_px": bbox,
        "occupied_fraction": round(occupied, 6), "has_true_alpha": True,
        "alpha_min": int(alpha.min()), "alpha_max": int(alpha.max()),
        "corners_clear_within_1_255": True, "corner_alpha_max": corner_alpha_max,
        "alpha_quantization_tolerance": 1, "pixels_per_unit": pixels_per_unit,
        "pivot": pivot, "nominal_height_m": world_height, "sha256": sha256(path),
    }


def write_meta(path: Path, sprite_settings: dict[str, dict]):
    relative = path.relative_to(UNITY_ROOT).as_posix()
    identity = guid(relative)
    if path.is_dir():
        text = f"fileFormatVersion: 2\nguid: {identity}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    elif path.parent == SPRITES and path.suffix.lower() == ".png":
        record = sprite_settings[path.stem]
        pivot = record["pivot"]
        text = f"""fileFormatVersion: 2
guid: {identity}
TextureImporter:
  externalObjects: {{}}
  serializedVersion: 12
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  streamingMipmaps: 0
  textureFormat: 1
  maxTextureSize: 4096
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  textureCompression: 0
  alphaSource: 1
  alphaIsTransparency: 1
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 9
  spritePivot: {{x: {pivot[0]}, y: {pivot[1]}}}
  spritePixelsToUnits: {record['pixels_per_unit']}
  textureType: 8
  textureShape: 1
  userData: Realistic coffee cutout; imagegen original pixels preserved
  assetBundleName: 
  assetBundleVariant: 
"""
    elif path.suffix == ".cs":
        text = f"fileFormatVersion: 2\nguid: {identity}\nMonoImporter:\n  externalObjects: {{}}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {{instanceID: 0}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    else:
        text = f"fileFormatVersion: 2\nguid: {identity}\n"
    path.with_name(path.name + ".meta").write_text(text, encoding="utf-8")


def font(size, bold=False, korean=False):
    filename = "malgunbd.ttf" if korean and bold else "malgun.ttf" if korean else "arialbd.ttf" if bold else "arial.ttf"
    return ImageFont.truetype(str(Path("C:/Windows/Fonts") / filename), size)


def preview(products: list[dict], available: dict[str, dict], partial=False) -> Path:
    # This is a presentation sheet. The source PNG files are never rewritten.
    priority = ["yirgacheffe_kurume", "caturra", "kilimanjaro_bourbon"]
    ordered = sorted(products, key=lambda p: priority.index(p["slug"]) if p["slug"] in priority else 3 + products.index(p))
    columns, cell_width, cell_height = 6, 420, 574
    rows = (len(ordered) + columns - 1) // columns
    sheet = Image.new("RGB", (columns * cell_width + 64, rows * cell_height + 190), "#eae5dc")
    draw = ImageDraw.Draw(sheet)
    draw.text((44, 30), "COFFEE / REALISTIC ORIGIN COLLECTION", font=font(46, True), fill="#39352f")
    draw.text((47, 96), f"{len(available)} / {len(products)} ORIGIN EDITIONS  |  TRUE-ALPHA 2.5D SPRITES", font=font(23), fill="#7d7365")
    for index, product in enumerate(ordered):
        x, y = 32 + index % columns * cell_width, 152 + index // columns * cell_height
        draw.rounded_rectangle((x, y, x + cell_width - 12, y + cell_height - 14), radius=18, fill="#f9f7f1")
        slug = product["slug"]
        if slug in available:
            with Image.open(SPRITES / f"{slug}.png") as source:
                cutout = source.copy()
            cutout.thumbnail((388, 440), Image.Resampling.LANCZOS)
            sheet.paste(cutout, (x + (cell_width - 12 - cutout.width) // 2, y + 10 + (440 - cutout.height) // 2), cutout)
        else:
            draw.text((x + 115, y + 210), "GENERATING", font=font(20), fill="#b1a594")
        name = product["region"].upper()
        name_size = 25
        while draw.textlength(name, font=font(name_size, True)) > cell_width - 46 and name_size > 15:
            name_size -= 1
        draw.text((x + 20, y + 451), name, font=font(name_size, True), fill="#403a31")
        draw.text((x + 20, y + 489), product["country"].upper(), font=font(17), fill="#807466")
        label = product.get("origin_label_ko", product.get("region_ko", ""))
        draw.text((x + 20, y + 522), label, font=font(18, korean=True), fill="#897b68")
    draw.text((46, sheet.height - 28), "PHOTOREALISTIC RASTER CUTOUTS / ORIGINAL GENERATED PIXELS / UNITY SPRITE IMPORT SETTINGS INCLUDED", font=font(15), fill="#887d6e")
    output = ROOT / ("preview_partial.png" if partial else "preview_realistic_18.png")
    sheet.save(output, optimize=True)
    return output


def make_package(sprite_settings):
    for path in [ASSET_ROOT, *sorted(ASSET_ROOT.rglob("*"))]:
        if path.suffix != ".meta":
            write_meta(path, sprite_settings)
    source_paths = [p for p in [ASSET_ROOT, *sorted(ASSET_ROOT.rglob("*"))] if p.suffix != ".meta"]
    with tarfile.open(PACKAGE, "w:gz", compresslevel=6) as archive:
        for path in source_paths:
            relative = path.relative_to(UNITY_ROOT).as_posix()
            identity = guid(relative)
            if path.is_file():
                archive.add(path, arcname=f"{identity}/asset", recursive=False)
            archive.add(path.with_name(path.name + ".meta"), arcname=f"{identity}/asset.meta", recursive=False)
            pathname = relative.encode("utf-8")
            entry = tarfile.TarInfo(f"{identity}/pathname")
            entry.size = len(pathname)
            archive.addfile(entry, io.BytesIO(pathname))
    return source_paths


def validate_package(source_paths):
    expected = {p.relative_to(UNITY_ROOT).as_posix(): p for p in source_paths}
    seen = set()
    file_count = 0
    with tarfile.open(PACKAGE, "r:gz") as archive:
        members = {m.name: m for m in archive.getmembers()}
        if len(members) != len(archive.getmembers()):
            raise ValueError("Duplicate package members")
        for member in archive.getmembers():
            if not member.name.endswith("/pathname"):
                continue
            handle = archive.extractfile(member)
            assert handle is not None
            relative = handle.read().decode("utf-8")
            pure = PurePosixPath(relative)
            if pure.is_absolute() or ".." in pure.parts or relative not in expected or relative in seen:
                raise ValueError(f"Unexpected or duplicate Unity pathname: {relative}")
            seen.add(relative)
            identity = member.name.split("/")[0]
            if identity != guid(relative):
                raise ValueError("Package GUID differs from stable pathname GUID")
            original = expected[relative]
            for suffix, source in [("asset.meta", original.with_name(original.name + ".meta"))] + ([("asset", original)] if original.is_file() else []):
                handle = archive.extractfile(f"{identity}/{suffix}")
                if handle is None or hashlib.sha256(handle.read()).hexdigest() != sha256(source):
                    raise ValueError("Packaged bytes differ from source file")
            file_count += original.is_file()
    if seen != set(expected):
        raise ValueError("Unity package is missing source assets")
    return {"files": file_count, "entries": len(seen), "source_hashes_match": True, "sha256": sha256(PACKAGE)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--products", type=Path, default=SOURCE)
    parser.add_argument("--allow-partial", action="store_true", help="Write a preview of present PNGs; never package a partial set")
    parser.add_argument("--validate-only", action="store_true", help="Inspect source PNGs without writing Unity imports or a package")
    parser.add_argument("--reuse-preview", action="store_true", help="Keep the existing gallery when repackaging unchanged images")
    args = parser.parse_args()
    source = json.loads(args.products.read_text(encoding="utf-8-sig"))
    products = source["products"]
    identities = [p["slug"] for p in products]
    if len(products) != 18 or len(set(identities)) != 18 or not all(re.fullmatch(r"[a-z0-9_]+", slug) for slug in identities):
        raise ValueError("Expected 18 unique, safe product identifiers")
    records, errors, missing = {}, [], []
    for product in products:
        path = SPRITES / f"{product['slug']}.png"
        if not path.is_file():
            missing.append(product["slug"])
            continue
        try:
            records[product["slug"]] = inspect_image(path, product)
        except Exception as error:
            errors.append(f"{product['slug']}: {error}")
    extra = sorted(p.stem for p in SPRITES.glob("*.png") if p.stem not in identities)
    if extra:
        errors.append("Unexpected sprite identifiers: " + ", ".join(extra))
    report = {"checked_at_utc": datetime.now(timezone.utc).isoformat(), "expected_count": 18,
              "checked_count": len(records), "missing": missing, "errors": errors,
              "images": list(records.values()), "unity_editor_executed": False,
              "limitations": ["Unity Editor import and execution have not been run.",
                              "Photorealistic raster cutouts; no new 3D meshes are included.",
                              "Printed text requires visual review of each generated image."],
              "passed": not missing and not errors}
    if args.allow_partial and records:
        report["preview"] = str(preview(products, records, partial=not report["passed"]))
    if report["passed"] and not args.validate_only:
        catalog = ROOT / "catalog"
        write_json(catalog / "products.json", source)
        write_json(ASSET_ROOT / "Data" / "products.json", source)
        manifest = {"format_version": 1, "asset_type": "photorealistic 2.5D raster sprites",
                    "generation_mode": "built-in image_gen", "sprite_count": 18,
                    "sprite_imports": list(records.values()),
                    "taste_basis": source.get("profile_scale", {}).get("basis", "Illustrative game profiles")}
        write_json(catalog / "sprite_manifest.json", manifest)
        write_json(ASSET_ROOT / "Data" / "sprite_manifest.json", manifest)
        if (ROOT / "README_KO.md").exists():
            shutil.copy2(ROOT / "README_KO.md", ASSET_ROOT / "README_KO.md")
        preview_path = ROOT / "preview_realistic_18.png"
        if not args.reuse_preview or not preview_path.is_file():
            preview_path = preview(products, records)
        report["preview"] = str(preview_path)
        paths = make_package(records)
        report["unitypackage"] = validate_package(paths)
        report["package"] = str(PACKAGE)
        # Prove preview generation and packaging did not change original pixels.
        for record in records.values():
            if sha256(UNITY_ROOT / record["sprite"]) != record["sha256"]:
                raise ValueError("A source image changed during packaging")
    write_json(ROOT / "validation.json", report)
    print(json.dumps({k: report.get(k) for k in ("passed", "expected_count", "checked_count", "missing", "errors", "preview", "package")}, ensure_ascii=False, indent=2))
    return 0 if report["passed"] or (args.allow_partial and not errors) else 1


if __name__ == "__main__":
    raise SystemExit(main())
