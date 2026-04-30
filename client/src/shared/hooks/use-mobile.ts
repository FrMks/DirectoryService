import * as React from "react";

const MOBILE_BREAKPOINT = 768;
const MOBILE_MEDIA_QUERY = `(max-width: ${MOBILE_BREAKPOINT - 1}px)`;

export function useIsMobile() {
  return React.useSyncExternalStore(
    subscribeToMobileChanges,
    getIsMobile,
    getServerSnapshot,
  );
}

function subscribeToMobileChanges(onStoreChange: () => void) {
  const mql = window.matchMedia(MOBILE_MEDIA_QUERY);

  mql.addEventListener("change", onStoreChange);

  return () => mql.removeEventListener("change", onStoreChange);
}

function getIsMobile() {
  return window.matchMedia(MOBILE_MEDIA_QUERY).matches;
}

function getServerSnapshot() {
  return false;
}
