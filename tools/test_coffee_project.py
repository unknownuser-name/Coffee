"""Independently validate the game's native Unity coffee-bean registration.

Run test_coffee_project.ps1 for model execution, syntax and this asset check.
This script only reads source PNGs, JSON, YAML and Unity metadata.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import sys

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'CloudCafeUnity/UnityProject/Assets'
BAGS = ASSETS / 'Resources/CoffeeBags'
SOURCE = ROOT / 'assets/coffee_bags_realistic/catalog/products.json'
ORIGINALS = ROOT / 'assets/coffee_bags_realistic/unity/Assets/CoffeeBagsRealistic/Sprites'


def require(condition, message):
    if not condition:
        raise ValueError(message)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read_json(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def meta_guid(path):
    meta = path.with_name(path.name + '.meta')
    require(meta.is_file(), f'Missing .meta for {path.relative_to(ROOT).as_posix()}')
    match = re.search(r'^guid:\s*([0-9a-f]{32})\s*$', meta.read_text(encoding='utf-8-sig'), re.M)
    require(match is not None, f'Invalid GUID in {meta.relative_to(ROOT).as_posix()}')
    return match[1]


def yaml_field(text, field):
    match = re.search(r'^\s+' + re.escape(field) + r':\s*(.*?)\s*$', text, re.M)
    require(match is not None, f'Missing serialized field {field}')
    raw = match[1]
    if raw.startswith('"'):
        return json.loads(raw)
    if raw.startswith("'") and raw.endswith("'"):
        return raw[1:-1].replace("''", "'")
    return raw


def validate():
    source = read_json(SOURCE)
    products = source['products']
    require(len(products) == 18 and len({p['slug'] for p in products}) == 18, 'Expected 18 unique source products')
    require(read_json(BAGS / 'Data/products.json') == source, 'Game product metadata differs from authored source')
    catalog = BAGS / 'CoffeeBeanCatalog.asset'
    require(catalog.is_file(), 'CoffeeBeanCatalog.asset is missing')
    catalog_text = catalog.read_text(encoding='utf-8-sig')
    catalog_guid = meta_guid(catalog)
    definition_script = ASSETS / 'Scripts/Coffee/CoffeeBeanDefinition.cs'
    catalog_script = ASSETS / 'Scripts/Coffee/CoffeeBeanCatalog.cs'
    definition_script_guid = meta_guid(definition_script)
    catalog_script_guid = meta_guid(catalog_script)
    require(re.search(r'm_Script:\s*\{[^}]*guid:\s*' + catalog_script_guid, catalog_text), 'Catalog references the wrong script GUID')
    require(len(list((BAGS / 'Beans').glob('*.asset'))) == 18, 'Expected exactly 18 native bean assets')
    require(len(list((BAGS / 'Sprites').glob('*.png'))) == 18, 'Expected exactly 18 registered PNG sprites')
    rows = []
    expected_guids = []
    regions = set()
    for product in products:
        slug = product['slug']
        png = BAGS / f'Sprites/{slug}.png'
        bean = BAGS / f'Beans/{slug}.asset'
        require(png.is_file() and bean.is_file(), f'{slug}: sprite or native bean asset missing')
        require(digest(png) == digest(ORIGINALS / f'{slug}.png'), f'{slug}: game sprite differs from generated original')
        with Image.open(png) as image:
            require(image.format == 'PNG' and image.mode == 'RGBA', f'{slug}: sprite is not a real RGBA PNG')
            require(image.size == (1024, 1536), f'{slug}: unexpected source size')
            alpha = np.asarray(image.getchannel('A'))
            require(int(alpha.min()) == 0 and int(alpha.max()) >= 240, f'{slug}: alpha channel missing usable clear/opaque pixels')
            require(max(int(a.max()) for a in [alpha[:12,:12], alpha[:12,-12:], alpha[-12:,:12], alpha[-12:,-12:]]) <= 1,
                    f'{slug}: corners exceed native 1/255 alpha precision')
        sprite_guid = meta_guid(png)
        bean_guid = meta_guid(bean)
        expected_guids.append(bean_guid)
        text = bean.read_text(encoding='utf-8-sig')
        require(re.search(r'm_Script:\s*\{[^}]*guid:\s*' + definition_script_guid, text), f'{slug}: wrong bean-definition script GUID')
        require(yaml_field(text, 'Id') == slug, f'{slug}: serialized product ID mismatch')
        for field, source_key in [('Country','country'), ('CountryKo','country_ko'), ('Region','region'), ('RegionKo','region_ko')]:
            require(yaml_field(text, field) == product[source_key], f'{slug}: {field} differs from source')
        require(yaml_field(text, 'Variety') == product['variety']['name'], f'{slug}: cultivar mismatch')
        require(yaml_field(text, 'VarietyKo') == product['variety']['name_ko'], f'{slug}: Korean cultivar mismatch')
        require(yaml_field(text, 'Description') == product['description_ko'], f'{slug}: explanation differs from source')
        require(yaml_field(text, 'Process') == product['process_ko'], f'{slug}: processing differs from source')
        require(yaml_field(text, 'Roast') == product['roast_ko'], f'{slug}: roast differs from source')
        require(int(yaml_field(text, 'NetWeightGrams')) == 250, f'{slug}: net weight differs from source')
        notes_block = re.search(r'^  TastingNotes:\s*\r?\n((?:  -[^\r\n]*(?:\r?\n|$))+)', text, re.M)
        require(notes_block is not None, f'{slug}: tasting notes are missing')
        notes = [json.loads(line[4:]) for line in notes_block[1].splitlines()]
        require(notes == product['tasting_notes_ko'], f'{slug}: tasting notes differ from source')
        for field, source_key in [('Acidity','acidity'), ('Sweetness','sweetness'), ('Body','body')]:
            require(int(yaml_field(text, field)) == product['taste'][source_key], f'{slug}: {field} mismatch')
        require(re.search(r'BagSprite:\s*\{fileID:\s*21300000,\s*guid:\s*' + sprite_guid + r',\s*type:\s*3\}', text), f'{slug}: missing/wrong native Sprite reference')
        require(re.search(r'\b' + bean_guid + r'\b', catalog_text), f'{slug}: bean not included in catalog')
        meta = png.with_name(png.name + '.meta').read_text(encoding='utf-8-sig')
        for key, value in [('textureType',8),('spriteMode',1),('alphaIsTransparency',1)]:
            require(re.search(r'^\s+' + key + r':\s*' + str(value) + r'\s*$', meta, re.M), f'{slug}: {key} importer setting missing')
        ppu = float(re.search(r'^\s+spritePixelsToUnits:\s*([\d.]+)', meta, re.M)[1])
        require(ppu > 0, f'{slug}: invalid pixels per unit')
        rows.append({'id': slug, 'native_asset_linked': True, 'original_pixels_preserved': True, 'sprite_guid': sprite_guid, 'bean_guid': bean_guid})
        regions.add(product['region'].casefold())
    require(len(set(expected_guids)) == 18, 'Bean asset GUIDs collide')
    registered_guids = re.findall(r'\{fileID:\s*11400000,\s*guid:\s*([0-9a-f]{32}),\s*type:\s*2\}', catalog_text)
    require(len(registered_guids) == 18 and set(registered_guids) == set(expected_guids), 'Catalog must reference each native bean exactly once')
    require({'yirgacheffe','antioquia','kilimanjaro'} <= regions, 'Required example origins missing')
    # Scan the entire imported Unity project, catching accidental duplicate GUIDs
    # rather than validating only the newly authored folder in isolation.
    seen = {}
    for path in ASSETS.rglob('*.meta'):
        match = re.search(r'^guid:\s*([0-9a-f]{32})\s*$', path.read_text(encoding='utf-8-sig'), re.M)
        require(match is not None, f'Malformed metadata: {path.relative_to(ROOT).as_posix()}')
        require(match[1] not in seen, f'Duplicate Unity GUID: {path.relative_to(ROOT).as_posix()} and {seen.get(match[1])}')
        seen[match[1]] = path.relative_to(ROOT).as_posix()
    return {'passed': True, 'registered_count': 18, 'catalog_guid': catalog_guid, 'project_meta_count': len(seen),
            'native_sprite_and_catalog_references_verified': True, 'source_png_sha256_match': True,
            'required_origins_present': ['Yirgacheffe','Antioquia','Kilimanjaro'], 'beans': rows, 'errors': []}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--assets-only', action='store_true')
    parser.parse_args()
    try:
        result = validate()
    except Exception as error:
        result = {'passed': False, 'errors': [str(error)]}
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0 if result['passed'] else 1


if __name__ == '__main__':
    sys.exit(main())
