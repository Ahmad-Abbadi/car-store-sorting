// QA only: time ordinary rendering first, then count one complete WebGL frame.
// No draw-call wrappers run during Unity's fifteen-second timing sample.
(() => {
  let calls=0, triangles=0, pending=null, originals=[];
  // Fix only the QA copy's render target, independent of browser window decorations.
  const style=document.createElement('style');
  style.textContent='#unity-canvas{width:1280px!important;height:682px!important}#unity-container{width:1280px!important;height:720px!important}#unity-footer{width:1280px!important}';
  document.head.appendChild(style);
  const frames=[],timestamps=[];
  let last=performance.now();
  function install() {
    for(const type of [WebGLRenderingContext,WebGL2RenderingContext])
    for(const name of ['drawElements','drawArrays','drawElementsInstanced','drawArraysInstanced']) {
      if(!Object.prototype.hasOwnProperty.call(type.prototype,name))continue;
      const original=type.prototype[name];originals.push({proto:type.prototype,name,original});
      type.prototype[name]=function(...args) {
        calls++;
        const count=args[name.startsWith('drawElements')?1:2];
        const instances=name.endsWith('Instanced')?args[name.startsWith('drawElements')?4:3]:1;
        if(args[0]===4)triangles+=count/3*instances;
        return original.apply(this,args);
      };
    }
  }
  function sample(now) {
    frames.push(now-last);timestamps.push(now);
    if(pending && calls>0) {
      for(const {proto,name,original} of originals)proto[name]=original;
      originals=[];
      Object.assign(pending,{drawCalls:calls,triangles,capturePending:false,captureFrames:1,instrumentationOutsideTiming:true});
      pending=null;
      const report=window.carRenderingBenchmark;
      if(report) {
        const element=document.getElementById('car-benchmark-result');
        if(element)element.textContent=JSON.stringify(report,null,2);
        if(location.hostname==='localhost'||location.hostname==='127.0.0.1')
          fetch('/car-benchmark-report',{method:'POST',body:JSON.stringify(report)});
      }
    }
    last=now;requestAnimationFrame(sample);
  }
  requestAnimationFrame(sample);
  window.carRenderCounters={report() {
    const cutoff=performance.now()-15000;
    const f=frames.filter((_,index)=>timestamps[index]>=cutoff),sorted=[...f].sort((a,b)=>a-b);
    const canvas=document.querySelector('canvas');
    const gl=canvas&&(canvas.getContext('webgl2')||canvas.getContext('webgl'));
    const info=gl&&gl.getExtension('WEBGL_debug_renderer_info');
    pending={capturePending:true,frames:f.length,averageFrameMs:f.reduce((a,b)=>a+b,0)/f.length,
      p95FrameMs:sorted[Math.min(sorted.length-1,Math.floor(sorted.length*.95))],userAgent:navigator.userAgent,
      renderer:info?gl.getParameter(info.UNMASKED_RENDERER_WEBGL):'unavailable'};
    calls=0;triangles=0;install();return pending;
  }};
})();
