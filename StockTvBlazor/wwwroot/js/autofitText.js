(() => {
  /** @type {WeakMap<HTMLElement, {resize: ResizeObserver, mutation: MutationObserver}>} */
  const observers = new WeakMap();

  function fits(el) {
    // Use scroll sizes to detect overflow.
    return el.scrollWidth <= el.clientWidth && el.scrollHeight <= el.clientHeight;
  }

  function computeMaxPx(el) {
    // Upper bound based on available box.
    const h = Math.max(0, el.clientHeight);
    const w = Math.max(0, el.clientWidth);
    // Allow big text but keep it bounded.
    return Math.max(12, Math.floor(Math.min(h * 0.95, w * 0.60)));
  }

  function applyAutofit(el) {
    if (!(el instanceof HTMLElement)) return;
    if (!el.isConnected) return;

    const minPx = Number(el.dataset.autofitMin ?? "10");
    const maxPx = Number(el.dataset.autofitMax ?? computeMaxPx(el));

    const min = Number.isFinite(minPx) ? minPx : 10;
    const max = Number.isFinite(maxPx) ? maxPx : computeMaxPx(el);
    if (max <= 0 || el.clientWidth === 0 || el.clientHeight === 0) return;

    // Binary search the largest font-size that fits.
    let lo = Math.max(1, Math.floor(min));
    let hi = Math.max(lo, Math.floor(max));

    // If even the smallest doesn't fit, set to smallest.
    el.style.fontSize = `${lo}px`;
    if (!fits(el)) return;

    // If the biggest fits, take it.
    el.style.fontSize = `${hi}px`;
    if (fits(el)) return;

    // Search.
    while (lo + 1 < hi) {
      const mid = (lo + hi) >> 1;
      el.style.fontSize = `${mid}px`;
      if (fits(el)) lo = mid;
      else hi = mid;
    }
    el.style.fontSize = `${lo}px`;
  }

  function rafFit(el) {
    // Coalesce multiple triggers.
    if (el.dataset.autofitPending === "1") return;
    el.dataset.autofitPending = "1";
    requestAnimationFrame(() => {
      el.dataset.autofitPending = "0";
      applyAutofit(el);
    });
  }

  function observeContainer(containerSelector) {
    const container = document.querySelector(containerSelector);
    if (!container) return;

    const elements = container.querySelectorAll("[data-autofit]");
    elements.forEach((el) => {
      if (!(el instanceof HTMLElement)) return;
      if (observers.has(el)) {
        rafFit(el);
        return;
      }

      const resize = new ResizeObserver(() => rafFit(el));
      resize.observe(el);

      const mutation = new MutationObserver(() => rafFit(el));
      mutation.observe(el, {
        subtree: true,
        characterData: true,
        childList: true,
      });

      observers.set(el, { resize, mutation });
      rafFit(el);
    });
  }

  window.stockTvAutoFit = {
    observe: observeContainer,
    fit: (selector) => {
      document.querySelectorAll(selector).forEach((el) => {
        if (el instanceof HTMLElement) applyAutofit(el);
      });
    },
  };
})();

