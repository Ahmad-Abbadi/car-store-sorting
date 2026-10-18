"""Build a minimal Unity import package from the user's existing Asset Store cache."""
import os
import io
import re
import tarfile
from pathlib import Path

source = Path(os.environ['APPDATA']) / 'Unity/Asset Store-5.x/Synty Studios/3D ModelsEnvironmentsSci-Fi/POLYGON - Sci-Fi City Pack - Art by Synty.unitypackage'
output = Path('IntegrationEvidence/UpgradeStation/Factory/SelectedMechanicalParts.unitypackage')
output.parent.mkdir(parents=True, exist_ok=True)
with tarfile.open(source, 'r:gz') as archive:
    members = {m.name: m for m in archive.getmembers()}
    paths = {name.split('/')[0]: archive.extractfile(m).read().decode().splitlines()[0]
             for name, m in members.items() if name.endswith('/pathname')}
    selected = {guid for guid, path in paths.items() if path.endswith((
        'SM_Prop_Medical_Machine_01.prefab', 'SM_Prop_Robotic_Part_08.prefab'))}
    pending = list(selected)
    while pending:
        guid = pending.pop()
        for suffix in ('asset', 'asset.meta'):
            member = members.get(guid + '/' + suffix)
            if not member:
                continue
            data = archive.extractfile(member).read()
            for ref in re.findall(rb'guid: ([0-9a-f]{32})', data):
                ref = ref.decode()
                if ref in paths and ref not in selected:
                    if paths[ref].endswith(('.cs', '.unity', '.dll')):
                        raise RuntimeError('Unexpected executable/demo dependency: ' + paths[ref])
                    selected.add(ref)
                    pending.append(ref)
    with tarfile.open(output, 'w:gz', format=tarfile.GNU_FORMAT) as dest:
        for name, member in members.items():
            if name.split('/')[0] in selected:
                if name.endswith('/pathname'):
                    data = paths[name.split('/')[0]].encode('utf-8')
                    member.size = len(data)
                    dest.addfile(member, io.BytesIO(data))
                else:
                    dest.addfile(member, archive.extractfile(member) if member.isfile() else None)
    Path('IntegrationEvidence/UpgradeStation/Factory/import-manifest.txt').write_text('\n'.join(sorted(paths[g] for g in selected)))
    print('\n'.join(sorted(paths[g] for g in selected)))
    print('Package:', output, 'Bytes:', output.stat().st_size)
