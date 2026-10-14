import json,math
from surface_audit import P,D,data,hits
Z={'C001':-.43,'C002':-.30,'C003':-.58,'C004':-.28,'C005':-.13,'C007':-.65,'C008':-.35,'C009':-.48,'C010':-.90,'C011':-.40,'C012':-.60,'C014':-.62,'C015':-.72}
profiles=[]
for e in data:
    c=e['baseCarId'];z=Z[c];h=hits(e,0,z)[0];assert 'Glass' not in h['material']
    dz=(hits(e,0,z+.08)[0]['y']-hits(e,0,z-.08)[0]['y'])/.16
    dx=(hits(e,.08,z)[0]['y']-hits(e,-.08,z)[0]['y'])/.16
    samples=[{'x':x,'z':zz,'surface':hits(e,x,zz)[0]} for x in [-.15,0,.15] for zz in [z-.15,z,z+.15]]
    profiles.append({'profileId':c+'-R03-MOUNT','baseCarId':c,'buildIdentity':'existing themed V01 prefab build','anchorRenderer':h['renderer'],'anchorMaterial':h['material'],'mountDescription':'Solid rear deck behind the cockpit, ahead of the fixed wing' if c=='C010' else 'Measured painted roof surface, behind forward lights and windshield','localPosition':[0,h['y']-.022,z],'localRotation':[-math.degrees(math.atan(dz)),0,math.degrees(math.atan(dx))],'uniformScalePolicy':{'targetMaxFootprintFractionOfCarLength':.26,'maxWidthFraction':.65,'maxAddedAccessoryHeightFraction':.42},'sourceBoundsMin':e['boundsMin'],'sourceBoundsMax':e['boundsMax'],'contactSamples':samples,'fitStatus':'surface_measured_pending_render_review','evidence':'baseline/base_geometry.json and baseline/surface_samples.json','optionalPartOverrides':[]})
(D/'fitting_profiles.json').write_text(json.dumps(profiles,indent=2));print('Measured',len(profiles),'base-specific mounting profiles')
