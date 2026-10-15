mergeInto(LibraryManager.library, {
 TinyRidersReport: function(ptr) {
  var report=JSON.parse(UTF8ToString(ptr));
  report.wasmHeapBytes=HEAPU8.length;
  window.tinyRidersMetrics=report;
  if(window.location.search.indexOf('qa=1')>=0){var p=document.getElementById('tiny-riders-metrics')||document.createElement('pre');p.id='tiny-riders-metrics';p.textContent=JSON.stringify(report,null,2);p.style.cssText='position:fixed;right:4px;bottom:4px;max-width:330px;font:12px monospace;background:#08121ee8;color:#fff;padding:10px;z-index:10000';document.body.appendChild(p);}
 }
});

