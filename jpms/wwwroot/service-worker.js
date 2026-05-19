// Development service worker — does not cache anything.
// In development, Blazor's hot reload + browser refresh is faster without caching.
// The production service worker (service-worker.published.js) takes over when you `dotnet publish`.

self.addEventListener('fetch', () => { });
