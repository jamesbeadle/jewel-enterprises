// Manual boot, for resilience Blazor doesn't provide on its own. Safari in particular can drop
// the download of dotnet.native.wasm (the one multi-megabyte boot file) with a bare
// "TypeError: Load failed", and the stock loader just gives up — leaving the jewel pulsing forever
// over a blank page. Two defences:
//
//   1. loadBootResource retries each failed download, backing off briefly and switching to
//      cache: 'reload' plus a cache-busting query so the retry cannot be served whatever
//      truncated or stale response broke the first attempt.
//   2. If boot still fails, the boot screen swaps its "Loading" line for the "couldn't load /
//      try again" prompt held in index.html's #boot-failed template (jpmsBoot.fail).
//
// Loaded after _framework/blazor.webassembly.js (autostart="false"), so `Blazor` exists here.
(function () {
    const RETRY_LIMIT = 2;
    const RETRY_BACKOFF_MS = 500;

    const fetchWithRetry = (defaultUri, integrity) => {
        const attempt = (n) =>
            fetch(n === 0 ? defaultUri : defaultUri + '?retry=' + n, {
                cache: n === 0 ? 'no-cache' : 'reload',
                integrity: integrity || undefined
            }).then((response) => {
                if (!response.ok) throw new Error(response.status + ' ' + defaultUri);
                return response;
            }).catch((error) => {
                if (n >= RETRY_LIMIT) throw error;
                return new Promise((resolve) => setTimeout(resolve, RETRY_BACKOFF_MS * (n + 1)))
                    .then(() => attempt(n + 1));
            });
        return attempt(0);
    };

    Blazor.start({
        loadBootResource: (type, name, defaultUri, integrity) =>
            type === 'dotnetjs' ? defaultUri : fetchWithRetry(defaultUri, integrity)
    }).catch((error) => {
        console.error('JPMS failed to boot', error);
        window.jpmsBoot.fail();
    });
})();
