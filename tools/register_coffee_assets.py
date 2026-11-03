"""Register 18 original photographic cutouts as native Unity coffee assets.

Copies the source PNG bytes without changing pixels. Creates serialized
ScriptableObject definitions, a Resources catalog, and stable Unity metadata.
Run from anywhere; --validate-only inspects the registered files without edits.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets" / "coffee_bags_realistic"
PROJECT = ROOT / "CloudCafeUnity" / "UnityProject"
ASSETS = PROJECT / "Assets"
REGISTRY = ASSETS / "Resources" / "CoffeeBags"
SCRIPTS = ASSETS / "Scripts" / "Coffee"
EDITOR = ASSETS / "Editor" / "CoffeeBagCatalogEditor.cs"


def identity(path: Path) -> str:
    return hashlib.md5(("cloud-cafe-coffee-registry-v1/" + path.relative_to(PROJECT).as_posix()).encode()).hexdigest()


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def scalar(value) -> str:
    return json.dumps(value, ensure_ascii=False)


def write(path: Path, value: str):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(value, encoding="utf-8", newline="\n")


def write_json(path: Path, value):
    write(path, json.dumps(value, ensure_ascii=False, indent=2) + "\n")


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def meta(path: Path, sprite=None):
    value = f"fileFormatVersion: 2\nguid: {identity(path)}\n"
    if path.is_dir():
        value += "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    elif sprite is not None:
        pivot = sprite["pivot"]
        value += f"""TextureImporter:
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
  spritePixelsToUnits: {sprite['pixels_per_unit']}
  textureType: 8
  textureShape: 1
  userData: Original photographic coffee bag PNG; bottom-centred pivot
  assetBundleName:
  assetBundleVariant:
"""
    elif path.suffix == ".cs":
        value += "MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    elif path.suffix == ".asset":
        value += "NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    else:
        value += "DefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    write(path.with_name(path.name + ".meta"), value)


def header(name: str, script_path: Path) -> str:
    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {identity(script_path)}, type: 3}}
  m_Name: {scalar(name)}
  m_EditorClassIdentifier:
"""


def create(products, source_manifest):
    imports = {item["id"]: item for item in source_manifest["sprite_imports"]}
    definition_script = SCRIPTS / "CoffeeBeanDefinition.cs"
    catalog_script = SCRIPTS / "CoffeeBeanCatalog.cs"
    for path in (definition_script, catalog_script, EDITOR):
        if not path.is_file():
            raise ValueError(f"Registry script is missing: {path}")
        meta(path)
    for folder in (SCRIPTS, REGISTRY, REGISTRY / "Sprites", REGISTRY / "Beans", REGISTRY / "Data"):
        folder.mkdir(parents=True, exist_ok=True)
        meta(folder)

    registered = []
    bean_paths = []
    for product in products:
        slug = product["slug"]
        sprite = imports[slug]
        source_image = SOURCE / "unity" / sprite["sprite"]
        destination = REGISTRY / "Sprites" / f"{slug}.png"
        if sha256(source_image) != sprite["sha256"]:
            raise ValueError(f"Original image differs from its manifest: {slug}")
        shutil.copy2(source_image, destination)
        meta(destination, sprite)
        variety, taste = product["variety"], product["taste"]
        fields = {
            "Id": slug, "Country": product["country"], "CountryKo": product["country_ko"],
            "Region": product["region"], "RegionKo": product["region_ko"],
            "Variety": variety["name"], "VarietyKo": variety["name_ko"],
            "Description": product["description_ko"], "Acidity": taste["acidity"],
            "Sweetness": taste["sweetness"], "Body": taste["body"],
            "Process": product["process_ko"], "Roast": product["roast_ko"],
            "NetWeightGrams": product["net_weight_g"], "SourceUrl": product["source_url"],
        }
        asset = REGISTRY / "Beans" / f"{slug}.asset"
        text = header(slug, definition_script)
        for name, value in fields.items():
            text += f"  {name}: {scalar(value)}\n"
        text += "  TastingNotes:\n" + "".join(f"  - {scalar(note)}\n" for note in product["tasting_notes_ko"])
        text += f"  BagSprite: {{fileID: 21300000, guid: {identity(destination)}, type: 3}}\n"
        write(asset, text)
        meta(asset)
        bean_paths.append(asset)
        registered.append({"id": slug, "definition": asset.relative_to(PROJECT).as_posix(),
                           "definition_guid": identity(asset), "sprite": destination.relative_to(PROJECT).as_posix(),
                           "sprite_guid": identity(destination), "sha256": sha256(destination),
                           "pixels_per_unit": sprite["pixels_per_unit"], "pivot": sprite["pivot"]})
    catalog = REGISTRY / "CoffeeBeanCatalog.asset"
    write(catalog, header("CoffeeBeanCatalog", catalog_script) + "  Beans:\n" + "".join(
        f"  - {{fileID: 11400000, guid: {identity(path)}, type: 2}}\n" for path in bean_paths))
    meta(catalog)
    write_json(REGISTRY / "Data" / "products.json", read_json(SOURCE / "catalog" / "products.json"))
    write_json(REGISTRY / "Data" / "sprite_manifest.json", source_manifest)
    write_json(REGISTRY / "Data" / "asset_registration.json", {
        "format_version": 1, "count": len(registered), "resource_path": "CoffeeBags/CoffeeBeanCatalog",
        "asset_type": "photorealistic fixed-view 2.5D sprites", "assets": registered})
    for path in (REGISTRY / "Data").iterdir():
        if path.is_file() and path.suffix != ".meta":
            meta(path)


def get_guid(path: Path):
    match = re.search(r"^guid: ([0-9a-f]{32})$", path.with_name(path.name + ".meta").read_text(encoding="utf-8"), re.M)
    if match is None:
        raise ValueError(f"Missing/invalid meta GUID: {path}")
    return match[1]


def validate(products, source_manifest):
    imports = {item["id"]: item for item in source_manifest["sprite_imports"]}
    expected = {p["slug"] for p in products}
    if len(products) != 18 or len(expected) != 18 or set(imports) != expected:
        raise ValueError("Products and sprite import records must contain the same 18 identifiers")
    if {p.stem for p in (REGISTRY / "Sprites").glob("*.png")} != expected:
        raise ValueError("Registered sprite identities/count differ from source")
    if {p.stem for p in (REGISTRY / "Beans").glob("*.asset")} != expected:
        raise ValueError("Registered definition identities/count differ from source")
    catalog = REGISTRY / "CoffeeBeanCatalog.asset"
    catalog_text = catalog.read_text(encoding="utf-8")
    definition_script_guid = get_guid(SCRIPTS / "CoffeeBeanDefinition.cs")
    catalog_script_guid = get_guid(SCRIPTS / "CoffeeBeanCatalog.cs")
    if f"guid: {catalog_script_guid}, type: 3" not in catalog_text:
        raise ValueError("Catalog ScriptableObject script reference is invalid")
    all_guids = []
    for product in products:
        slug = product["slug"]
        image = REGISTRY / "Sprites" / f"{slug}.png"
        definition = REGISTRY / "Beans" / f"{slug}.asset"
        image_guid, asset_guid = get_guid(image), get_guid(definition)
        all_guids.extend((image_guid, asset_guid))
        if sha256(image) != imports[slug]["sha256"]:
            raise ValueError(f"Registered PNG pixels/bytes differ from the original: {slug}")
        text = definition.read_text(encoding="utf-8")
        for reference in (f"guid: {definition_script_guid}, type: 3", f"guid: {image_guid}, type: 3"):
            if reference not in text:
                raise ValueError(f"Invalid script/sprite reference: {slug}")
        if f"guid: {asset_guid}, type: 2" not in catalog_text:
            raise ValueError(f"Definition is not included in the Resources catalog: {slug}")
        for name, expected_value in {"Id": slug, "Country": product["country"], "Region": product["region"],
                                     "Variety": product["variety"]["name"], "Description": product["description_ko"],
                                     "Acidity": product["taste"]["acidity"], "Sweetness": product["taste"]["sweetness"],
                                     "Body": product["taste"]["body"]}.items():
            match = re.search(rf"^  {name}: (.*)$", text, re.M)
            if match is None or json.loads(match[1]) != expected_value:
                raise ValueError(f"Definition metadata differs from source: {slug}.{name}")
        image_meta = image.with_name(image.name + ".meta").read_text(encoding="utf-8")
        ppu = re.search(r"^  spritePixelsToUnits: (.*)$", image_meta, re.M)
        if ppu is None or abs(float(ppu[1]) - imports[slug]["pixels_per_unit"]) > .01:
            raise ValueError(f"Sprite scale differs from source: {slug}")
        if "  spriteMode: 1" not in image_meta or "  alphaIsTransparency: 1" not in image_meta:
            raise ValueError(f"PNG is not configured as a transparent Single Sprite: {slug}")
    if len(all_guids) != len(set(all_guids)):
        raise ValueError("Registered Sprite/definition GUIDs collide")
    for path in [SCRIPTS, REGISTRY, *SCRIPTS.rglob("*"), *REGISTRY.rglob("*"), EDITOR]:
        if path.suffix != ".meta" and not path.with_name(path.name + ".meta").is_file():
            raise ValueError(f"Unity asset has no metadata: {path}")
    return {"passed": True, "count": len(products), "sprites": 18, "definitions": 18,
            "catalog_reference_count": len(re.findall(r"^  - \{fileID: 11400000, guid:", catalog_text, re.M)),
            "original_png_hashes_match": True, "script_and_sprite_guids_resolve": True,
            "resource_path": "CoffeeBags/CoffeeBeanCatalog", "unity_editor_executed": False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--validate-only", action="store_true")
    args = parser.parse_args()
    products = read_json(SOURCE / "catalog" / "products.json")["products"]
    manifest = read_json(SOURCE / "catalog" / "sprite_manifest.json")
    if not args.validate_only:
        create(products, manifest)
    report = validate(products, manifest)
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
