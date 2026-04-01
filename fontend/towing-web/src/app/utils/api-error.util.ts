/** Extracts a readable message from Angular HttpClient / ASP.NET error payloads. */
export function parseApiError(err: unknown): string {
  const e = err as {
    error?: Record<string, unknown> | string;
    message?: string;
  };

  const raw = e?.error;
  if (typeof raw === 'string' && raw.trim()) {
    return raw;
  }

  if (raw && typeof raw === 'object') {
    const body = raw as Record<string, unknown>;
    if (typeof body['detail'] === 'string' && (body['detail'] as string).trim()) {
      return body['detail'] as string;
    }
    if (typeof body['message'] === 'string' && (body['message'] as string).trim()) {
      return body['message'] as string;
    }
    if (typeof body['title'] === 'string') {
      const m = typeof body['message'] === 'string' ? (body['message'] as string) : '';
      return m ? `${body['title']}: ${m}` : (body['title'] as string);
    }
    const errs = body['errors'];
    if (errs && typeof errs === 'object') {
      const parts = Object.values(errs as Record<string, unknown[]>)
        .flat()
        .map((x) => String(x));
      if (parts.length) {
        return parts.join(' ');
      }
    }
  }

  if (typeof e?.message === 'string' && e.message.trim()) {
    return e.message;
  }

  return 'Something went wrong.';
}
