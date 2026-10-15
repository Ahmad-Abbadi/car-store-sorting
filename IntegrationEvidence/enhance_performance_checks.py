from pathlib import Path
p=Path('Assets/TinyRiders/Gameplay/ThemedPerformanceProbe.cs');s=p.read_text(encoding='utf-8')
s=s.replace('private void Update(){', '''[UnityEngine.Scripting.Preserve]
  public void SamplePilePhysics(){
   if(!Debug.isDebugBuild)return;
   frames.Clear();sampleBegin=Time.realtimeSinceStartup;peakBytes=0;maxWorld=0;maxDynamic=0;reported=false;
   int count=0;foreach(var item in WorldItemBase.All){if(item==null||item.Body==null)continue;item.Body.isKinematic=false;item.Body.AddForce(Vector3.up*.6f,ForceMode.VelocityChange);ItemPhysicsCulling.Track(item);if(++count==24)break;}
  }
  private void Update(){''')
s=s.replace('loadedRendererComponents=FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length,', 'loadedRendererComponents=FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length,enabledRendererComponents=FindObjectsByType<Renderer>(FindObjectsSortMode.None).Count(r=>r.enabled&&r.gameObject.activeInHierarchy),')
s=s.replace('totalRecords,loadedRendererComponents;', 'totalRecords,loadedRendererComponents,enabledRendererComponents;')
p.write_text(s,encoding='utf-8')
p=Path('Assets/TinyRiders/Gameplay/Plugins/ThemedPerformance.jslib');s=p.read_text(encoding='utf-8')
s=s.replace("var p=document.createElement('pre');p.id='tiny-riders-metrics';", "var p=document.getElementById('tiny-riders-metrics')||document.createElement('pre');p.id='tiny-riders-metrics';")
p.write_text(s,encoding='utf-8')
p=Path('Assets/TinyRiders/Gameplay/Editor/WebHost/TinyRiders.host.js.txt');s=p.read_text(encoding='utf-8')
s=s.replace('document.body.appendChild(label);', '''document.body.appendChild(label);
      const physics = document.createElement('button');
      physics.id = 'tiny-riders-physics-sample';
      physics.textContent = 'Sample 24-body pile physics';
      physics.style.cssText = 'position:fixed;left:4px;bottom:4px;z-index:10001';
      physics.onclick = () => instance.SendMessage('Themed Continuous Collection','SamplePilePhysics');
      document.body.appendChild(physics);''')
p.write_text(s,encoding='utf-8')
