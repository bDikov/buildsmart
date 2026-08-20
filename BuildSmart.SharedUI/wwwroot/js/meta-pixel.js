// Meta Pixel (Facebook Pixel) Initializer & Event Helper
window.initializeMetaPixel = function (pixelId) {
    if (!pixelId || window.metaPixelInitialized) return;
    window.metaPixelInitialized = true;
    window.BuildSmart_MetaPixelId = pixelId;

    (function (f, b, e, v, n, t, s) {
        if (f.fbq) return; n = f.fbq = function () {
            n.callMethod ?
                n.callMethod.apply(n, arguments) : n.queue.push(arguments)
        };
        if (!f._fbq) f._fbq = n; n.push = n; n.loaded = !0; n.version = '2.0';
        n.queue = []; t = b.createElement(e); t.async = !0;
        t.src = v; s = b.getElementsByTagName(e)[0];
        s.parentNode.insertBefore(t, s)
    })(window, document, 'script', 'https://connect.facebook.net/en_US/fbevents.js');

    try {
        fbq('init', pixelId);
        fbq('track', 'PageView');
    } catch (e) {
        console.warn('[MetaPixel] Tracking error:', e);
    }
};

// Safe Meta Pixel Event Tracker Helper
window.trackMetaEvent = function (eventName, payload) {
    if (typeof fbq !== 'function') return;
    try {
        if (payload) {
            fbq('track', eventName, payload);
        } else {
            fbq('track', eventName);
        }
    } catch (e) {
        console.warn('[MetaPixel] Event track error:', e);
    }
};
