/**
 * WNS Push Client — server-side Node.js module.
 *
 * Handles OAuth2 token acquisition from Microsoft and sends toast/tile/badge/raw
 * notifications to registered WNS channel URIs.
 *
 * Microsoft WNS REST API reference:
 *   https://learn.microsoft.com/en-us/previous-versions/windows/apps/hh465435(v=win.10)
 */

// Token cache: profileId → { token, expiresAt }
const tokenCache = new Map();

// ── OAuth2 ────────────────────────────────────────────────────────────────────

/**
 * Fetch (or return a cached) WNS access token using client credentials.
 *
 * @param {string} packageSid  - ms-app://… SID from Partner Center
 * @param {string} clientSecret - OAuth2 secret from Partner Center
 * @returns {Promise<string>} Bearer token
 */
export async function getAccessToken(packageSid, clientSecret) {
  const cacheKey = `${packageSid}::${clientSecret}`;
  const cached = tokenCache.get(cacheKey);
  if (cached && Date.now() < cached.expiresAt - 30_000) {
    return cached.token;
  }

  const body = new URLSearchParams({
    grant_type:    'client_credentials',
    client_id:     packageSid,
    client_secret: clientSecret,
    scope:         'notify.windows.com',
  });

  const res = await fetch('https://login.microsoftonline.com/consumers/oauth2/v2.0/token', {
    method:  'POST',
    headers: { 'content-type': 'application/x-www-form-urlencoded' },
    body:    body.toString(),
    signal:  AbortSignal.timeout(15_000),
  });

  if (!res.ok) {
    const err = await res.text().catch(() => res.statusText);
    throw new Error(`WNS token fetch failed (${res.status}): ${err}`);
  }

  const data = await res.json();
  const token = data.access_token;
  const expiresIn = (data.expires_in ?? 3600) * 1000;
  tokenCache.set(cacheKey, { token, expiresAt: Date.now() + expiresIn });
  return token;
}

// ── XML Builders ──────────────────────────────────────────────────────────────

/** Escape XML special characters. */
function esc(str) {
  return String(str ?? '')
    .replace(/&/g,  '&amp;')
    .replace(/</g,  '&lt;')
    .replace(/>/g,  '&gt;')
    .replace(/"/g,  '&quot;')
    .replace(/'/g,  '&apos;');
}

/**
 * Build WNS Toast XML payload.
 * @param {{ title, body, imageUri?, scenario?, actions?, launchArgs? }} p
 */
export function buildToastXml(p) {
  const scenario = p.scenario && p.scenario !== 'DEFAULT'
    ? ` scenario="${esc(p.scenario.toLowerCase())}"`
    : '';
  const launch   = p.launchArgs ? ` launch="${esc(p.launchArgs)}"` : '';
  const image    = p.imageUri
    ? `<image placement="appLogoOverride" src="${esc(p.imageUri)}"/>`
    : '';
  const actions  = (p.actions ?? [])
    .map(a => `<action content="${esc(a.content)}" arguments="${esc(a.arguments)}" activationType="${esc(a.activationType ?? 'foreground')}"/>`)
    .join('');
  const actionsEl = actions ? `<actions>${actions}</actions>` : '';

  return `<?xml version="1.0" encoding="utf-8"?>
<toast${scenario}${launch}>
  <visual>
    <binding template="ToastGeneric">
      <text>${esc(p.title)}</text>
      <text>${esc(p.body)}</text>
      ${image}
    </binding>
  </visual>
  ${actionsEl}
</toast>`;
}

/**
 * Build WNS Tile XML payload.
 * @param {{ text, imageUri?, branding? }} p
 */
export function buildTileXml(p) {
  const branding = p.branding ?? 'nameAndLogo';
  const image    = p.imageUri
    ? `<image src="${esc(p.imageUri)}"/>`
    : '';

  return `<?xml version="1.0" encoding="utf-8"?>
<tile>
  <visual branding="${esc(branding)}">
    <binding template="TileMedium">
      ${image}
      <text hint-style="body">${esc(p.text)}</text>
    </binding>
    <binding template="TileWide">
      ${image}
      <text hint-style="subtitle">${esc(p.text)}</text>
    </binding>
  </visual>
</tile>`;
}

/**
 * Build WNS Badge XML payload.
 * @param {{ value?: number, glyph?: string }} p - value 0 clears badge
 */
export function buildBadgeXml(p) {
  const val = p.glyph
    ? `value="${esc(p.glyph)}"`
    : `value="${Number(p.value ?? 0)}"`;
  return `<?xml version="1.0" encoding="utf-8"?>
<badge ${val}/>`;
}

// ── Send ──────────────────────────────────────────────────────────────────────

/**
 * WNS notification type → Content-Type + X-WNS-Type header values.
 */
const WNS_TYPE_MAP = {
  toast: { contentType: 'text/xml', wnsType: 'wns/toast' },
  tile:  { contentType: 'text/xml', wnsType: 'wns/tile'  },
  badge: { contentType: 'text/xml', wnsType: 'wns/badge' },
  raw:   { contentType: 'application/octet-stream', wnsType: 'wns/raw' },
};

/**
 * Send a notification to a WNS channel URI.
 *
 * @param {string} accessToken - Bearer token from getAccessToken()
 * @param {string} channelUri  - Channel URI from Windows device
 * @param {{ type: 'toast'|'tile'|'badge'|'raw', xml?: string, data?: Buffer, ttl?: number, tag?: string, group?: string }} notification
 * @returns {Promise<{ status: number, headers: object, messageId: string }>}
 */
export async function sendNotification(accessToken, channelUri, notification) {
  const { type = 'toast', xml, data, ttl = 1800, tag, group } = notification;
  const { contentType, wnsType } = WNS_TYPE_MAP[type] ?? WNS_TYPE_MAP.toast;

  const headers = {
    'Authorization':    `Bearer ${accessToken}`,
    'Content-Type':     contentType,
    'X-WNS-Type':       wnsType,
    'X-WNS-TTL':        String(ttl),
    'X-WNS-RequestForStatus': 'true',
  };
  if (tag)   headers['X-WNS-Tag']   = tag;
  if (group) headers['X-WNS-Group'] = group;

  const body = type === 'raw'
    ? (data instanceof Buffer ? data : Buffer.from(String(data ?? '')))
    : (xml ?? '');

  const res = await fetch(channelUri, {
    method:  'POST',
    headers,
    body,
    signal:  AbortSignal.timeout(20_000),
  });

  const responseHeaders = Object.fromEntries(res.headers.entries());
  const messageId = responseHeaders['x-wns-msg-id'] ?? '';

  if (!res.ok) {
    const text = await res.text().catch(() => '');
    const err  = new Error(`WNS send failed (${res.status}): ${text}`);
    err.status  = res.status;
    err.wnsCode = responseHeaders['x-wns-error'] ?? '';
    throw err;
  }

  return { status: res.status, headers: responseHeaders, messageId };
}

// ── Convenience helpers ───────────────────────────────────────────────────────

/**
 * End-to-end send: obtain token, build XML, send.
 *
 * @param {{ packageSid, clientSecret, channelUri, type, payload, ttl?, tag?, group? }} opts
 */
export async function push(opts) {
  const { packageSid, clientSecret, channelUri, type = 'toast', payload, ttl, tag, group } = opts;
  const token = await getAccessToken(packageSid, clientSecret);

  let xml;
  switch (type) {
    case 'toast': xml = buildToastXml(payload); break;
    case 'tile':  xml = buildTileXml(payload);  break;
    case 'badge': xml = buildBadgeXml(payload); break;
    case 'raw':   /* xml left undefined, pass raw data via payload.data */ break;
    default: throw new Error(`Unknown notification type: ${type}`);
  }

  return sendNotification(token, channelUri, {
    type,
    xml,
    data: type === 'raw' ? payload.data : undefined,
    ttl,
    tag,
    group,
  });
}

/** Clear the in-memory token cache (useful in tests). */
export function clearTokenCache() {
  tokenCache.clear();
}
