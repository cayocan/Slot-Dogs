// Lightweight service worker that prefers network (no-cache) and falls back to cache if offline.
self.addEventListener('install', (event) => {
  self.skipWaiting();
});
self.addEventListener('activate', (event) => {
  event.waitUntil(self.clients.claim());
});
self.addEventListener('fetch', (event) => {
  const req = event.request;
  // Only handle GET requests to avoid interfering with form posts, etc.
  if (req.method !== 'GET') return;

  // Use network-first with cache: 'no-store' to avoid HTTP cache.
  event.respondWith(
    fetch(req, { cache: 'no-store', credentials: 'same-origin' })
      .catch(() => caches.match(req))
  );
});
