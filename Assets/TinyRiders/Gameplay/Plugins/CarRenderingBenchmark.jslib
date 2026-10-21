mergeInto(LibraryManager.library, {
  CarBatchReport: function(ptr) {
    var report = JSON.parse(UTF8ToString(ptr));
    report.wasmHeapBytes = HEAPU8.length;
    if (window.carRenderCounters) report.webgl = window.carRenderCounters.report();
    window.carRenderingBenchmark = report;
    var result = document.createElement('pre');
    result.id = 'car-benchmark-result';
    result.textContent = JSON.stringify(report, null, 2);
    result.style.cssText = 'position:fixed;right:0;top:0;max-width:340px;background:#112e;color:white;font:11px monospace;z-index:10000';
    document.body.appendChild(result);
    console.log('CAR_RENDER_BENCHMARK ' + JSON.stringify(report));
    if (location.hostname === '127.0.0.1' || location.hostname === 'localhost') fetch('/car-benchmark-report', {method:'POST',body:JSON.stringify(report)});
  }
});
