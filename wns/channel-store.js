/**
 * In-memory channel registry with JSON persistence.
 * Persists to wns-channels.json in the same directory when NODE_ENV !== 'test'.
 */
import { readFileSync, writeFileSync, existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const STORE_PATH = join(__dirname, 'wns-channels.json');

function loadChannels() {
  if (existsSync(STORE_PATH)) {
    try {
      return JSON.parse(readFileSync(STORE_PATH, 'utf8'));
    } catch {
      return {};
    }
  }
  return {};
}

// Map<profileId, Map<channelId, channel>>
let store = loadChannels();

function persist() {
  if (process.env.NODE_ENV === 'test') return;
  try {
    writeFileSync(STORE_PATH, JSON.stringify(store, null, 2), 'utf8');
  } catch { /* non-fatal */ }
}

export function upsertChannel(profileId, channel) {
  if (!store[profileId]) store[profileId] = {};
  store[profileId][channel.channel_id] = {
    ...channel,
    registered_at_ms: channel.registered_at_ms ?? Date.now(),
  };
  persist();
  return store[profileId][channel.channel_id];
}

export function getChannel(profileId, channelId) {
  return store[profileId]?.[channelId] ?? null;
}

export function listChannels(profileId) {
  return Object.values(store[profileId] ?? {});
}

export function removeChannel(profileId, channelId) {
  if (!store[profileId]?.[channelId]) return false;
  delete store[profileId][channelId];
  if (Object.keys(store[profileId]).length === 0) delete store[profileId];
  persist();
  return true;
}

export function pruneExpired() {
  const now = Date.now();
  let removed = 0;
  for (const profileId of Object.keys(store)) {
    for (const [id, ch] of Object.entries(store[profileId])) {
      if (ch.expires_at_ms && ch.expires_at_ms < now) {
        delete store[profileId][id];
        removed++;
      }
    }
    if (Object.keys(store[profileId]).length === 0) delete store[profileId];
  }
  if (removed > 0) persist();
  return removed;
}

export function resetStore() {
  store = {};
}
