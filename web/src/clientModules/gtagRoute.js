// SPA route-change page_view for the deferred Google Analytics loader
// (replaces the client module of @docusaurus/plugin-google-gtag).
// window.gtag is a queueing stub defined in <head>, so this is safe to call
// before gtag.js has actually been downloaded.
export function onRouteDidUpdate({location, previousLocation}) {
  if (
    previousLocation &&
    (location.pathname !== previousLocation.pathname ||
      location.search !== previousLocation.search ||
      location.hash !== previousLocation.hash)
  ) {
    // Wait one tick so react-helmet-async has updated document.title
    setTimeout(() => {
      if (typeof window.gtag === 'function') {
        window.gtag('set', 'page_path', location.pathname + location.search + location.hash);
        window.gtag('event', 'page_view');
      }
    });
  }
}
