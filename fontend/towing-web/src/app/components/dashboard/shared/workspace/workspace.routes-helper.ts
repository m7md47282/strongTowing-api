import { Router } from '@angular/router';

/** Current staff dashboard base path: `/admin` or `/dispatcher`. */
export function workspaceShellPrefix(router: Router): string {
  const path = router.url.split('?')[0];
  if (path.startsWith('/dispatcher')) return '/dispatcher';
  return '/admin';
}
