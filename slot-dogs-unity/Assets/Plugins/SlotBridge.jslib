mergeInto(LibraryManager.library, {
  JS_PostSessionData: function (jsonPtr) {
    try {
      var json = UTF8ToString(jsonPtr);
      var data = JSON.parse(json);
      var target = (window.parent && window.parent !== window) ? window.parent : window;
      target.postMessage(data, '*');
    } catch (e) {
      console.error('[SlotBridge] postMessage falhou:', e);
    }
  }
});
