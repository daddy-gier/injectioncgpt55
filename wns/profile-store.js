/**
 * In-memory WNS profile registry with JSON persistence.
 * Stores app credentials (packageSid + clientSecret) keyed by profileId.
 * Persists to wns-profiles.json — secrets are stored in plaintext; protect
 * this file with filesystem permissions in production.
 */
import { readFileSync, writeFileSync, existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';
import { createHash } from 'crypto';

const __dirname = dirname(fileURLToPath(import.meta.url));
const STORE_PATH = join(__dirname, 'wns-profiles.json');

function load() {
  if (existsSync(STORE_PATH)) {
    try { return JSON.parse(readFileSync(STORE_PATH, 'utf8')); } catch { return {}; }
  }
  return {};
}

let store = load();

function persist() {
  if (process.env.NODE_ENV === 'test') return;
  try { writeFileSync(STORE_PATH, JSON.stringify(store, null, 2), 'utf8'); } catch { }
}

export function upsertProfile(profile) {
  if (!profile.profile_id) throw new Error('profile_id required');
  if (!profile.package_sid) throw new Error('package_sid required');
  if (!profile.client_secret) throw new Error('client_secret required');
  store[profile.profile_id] = {
    ...profile,
    created_at_ms: store[profile.profile_id]?.created_at_ms ?? Date.now(),
  };
  persist();
  return safeProfile(store[profile.profile_id]);
}

export function getProfile(profileId) {
  return store[profileId] ?? null;
}

export function listProfiles() {
  return Object.values(store).map(safeProfile);
}

export function removeProfile(profileId) {
  if (!store[profileId]) return false;
  delete store[profileId];
  persist();
  return true;
}

/** Returns profile without exposing client_secret — replaces with a hash hint. */
function safeProfile(p) {
  const { client_secret, ...rest } = p;
  return {
    ...rest,
    client_secret_hint: client_secret
      ? createHash('sha256').update(client_secret).digest('hex').slice(0, 8) + '…'
      : null,
  };
}

export function importBundle(bundle) {
  const results = [];
  for (const p of bundle.profiles ?? []) {
    try {
      results.push({ profile_id: p.profile_id, ok: true, profile: upsertProfile(p) });
    } catch (e) {
      results.push({ profile_id: p.profile_id, ok: false, error: e.message });
    }
  }
  return results;
}

export function resetStore() { store = {}; }
