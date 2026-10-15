window.configureTinyRidersHost = function(canvas, config) {
  const started = performance.now();
  config.autoSyncPersistentDataPath = true;
  config.matchWebGLToCanvasSize = true;
  config.devicePixelRatio = 1;
  const query = new URLSearchParams(location.search);
  const fresh = query.get('fresh');
  if (fresh) for (const key of ['dataUrl','frameworkUrl','codeUrl'])
    config[key] += '?qaFresh=' + encodeURIComponent(fresh);
  const nativeLock = canvas.requestPointerLock && canvas.requestPointerLock.bind(canvas);
  if (nativeLock) {
    const explain = error => {
      console.warn('Mouse capture unavailable in this browser host:', error && error.name);
      document.documentElement.dataset.mouseCaptureFallback = 'true';
    };
    const attempt = (options, retry) => {
      try {
        const request = nativeLock(options);
        if (request && request.catch) return request.catch(error => {
          if (retry) return attempt(undefined, false);
          explain(error);
        });
        return request;
      } catch (error) {
        if (retry) return attempt(undefined, false);
        explain(error);
      }
    };
    // Browser denial is handled normally; no browser permission or safety setting is bypassed.
    canvas.requestPointerLock = options => attempt(options, !!(options && options.unadjustedMovement));
  }
  return function(instance) {
    const elapsed = performance.now() - started;
    document.documentElement.dataset.unityReadyMs = elapsed.toFixed(1);
    console.log('TINYRIDERS_LOADER_TO_READY_MS ' + elapsed.toFixed(1));
    if (query.get('qa') === '1') {
      const label = document.createElement('pre');
      label.id = 'tiny-riders-load-time';
      label.textContent = 'Loader to ready: ' + (elapsed / 1000).toFixed(2) + ' s';
      label.style.cssText = 'position:fixed;left:4px;bottom:40px;margin:0;padding:5px;background:#08121ee8;color:#fff;font:12px monospace;z-index:10000';
      document.body.appendChild(label);
      const physics = document.createElement('button');
      physics.id = 'tiny-riders-physics-sample';
      physics.textContent = 'Reload with 24-body physics sample';
      physics.style.cssText = 'position:fixed;left:4px;bottom:4px;z-index:10001';
      physics.onclick = () => {const url = new URL(location.href);url.searchParams.set('physics','1');location.href = url.toString();};
      document.body.appendChild(physics);
      const current = document.createElement('button');
      current.id = 'tiny-riders-current-sample';
      current.textContent = 'Sample this view (24 bodies)';
      current.style.cssText = 'position:fixed;left:280px;bottom:4px;z-index:10001';
      current.onclick = () => instance.SendMessage('Compact Theme Collection','SamplePilePhysics');
      document.body.appendChild(current);
    }
  };
};
