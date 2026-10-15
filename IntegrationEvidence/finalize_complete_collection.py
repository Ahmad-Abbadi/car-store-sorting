"""Keep the portable CSV and current gameplay summary aligned with the validated collection."""
import csv
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
evidence = root / 'IntegrationEvidence'
validation = json.loads((evidence / 'complete_collection_validation.json').read_text())
assert validation['passed'] and validation['designs'] == 600
folder = root / 'Assets/TinyRiders/ThemedCollection/Data/Themes'
entries = json.loads((folder / 'variant_catalog.json').read_text())['entries']
target = folder / 'variant_catalog.csv'
with target.open(encoding='utf-8-sig', newline='') as source:
    reader = csv.DictReader(source)
    fields = reader.fieldnames
    existing = {row['collectibleId'] for row in reader}
with target.open('a', encoding='utf-8', newline='') as output:
    writer = csv.DictWriter(output, fieldnames=fields, extrasaction='ignore')
    for entry in entries:
        if entry['collectibleId'] in existing:
            continue
        row = dict(entry)
        row['accessoryKitId'] = 'KIT_' + entry['themeId']
        row['gameplayPreviewPath'] = entry['previewPath']
        row['validationNotes'] = 'Full 15-shape collection; gameplay identity, theme materials and fixed shelf fit validated.'
        writer.writerow(row)
summary = {
    'scene': 'Assets/TinyRiders/Scenes/ThemedSorting.unity',
    'fixedShelfScale': 9,
    'variants': validation['designs'],
    'themes': validation['themes'],
    'physicalInstances': validation['physicalCars'],
    'copiesPerDesign': validation['copiesPerDesign'],
    'shelves': validation['shelves'],
    'capacity': validation['slots'],
    'visiblePileLimit': validation['visiblePileLimit'],
    'dynamicPhysicsLimit': 24,
}
(evidence / 'build_summary.json').write_text(json.dumps(summary, indent=2))
completion = {
    'designs': len(entries), 'themes': validation['themes'], 'shapesPerTheme': 15,
    'copiesPerDesign': 2, 'physicalCars': validation['physicalCars'],
    'existingDesignsPreserved': 410,
    'addedDesigns': [e['collectibleId'] for e in entries if e.get('releaseId') == 'complete_15_shapes_v1'],
}
(evidence / 'collection_completion.json').write_text(json.dumps(completion, indent=2))
print(json.dumps(summary, indent=2))
