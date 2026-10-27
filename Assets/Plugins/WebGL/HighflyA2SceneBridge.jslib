mergeInto(LibraryManager.library, {
  HF_A2_NotifyScene: function (scenePtr) {
    if (typeof window !== 'undefined' && typeof window.HF_A2_setScene === 'function') {
      window.HF_A2_setScene(UTF8ToString(scenePtr));
    }
  }
});
