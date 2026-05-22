/**
 * WNS Push Client — Express router + public API.
 *
 * Mount in your Express app:
 *   import wnsRouter from './wns/index.js';
 *   app.use('/api/wns', wnsRouter);
 *
 * Endpoints:
 *   POST   /api/wns/profiles          upsert a WNS app profile
 *   GET    /api/wns/profiles          list profiles (secrets redacted)
 *   DELETE /api/wns/profiles/:id      remove a profile
 *   POST   /api/wns/profiles/import   import a WnsProfileBundle (JSON or binary proto)
 *
 *   POST   /api/wns/channels          register / refresh a channel URI
 *   GET    /api/wns/channels          list channels for a profile
 *   DELETE /api/wns/channels/:id      remove a channel
 *   POST   /api/wns/channels/prune    remove expired channels
 *
 *   POST   /api/wns/send              send a notification
 *   POST   /api/wns/broadcast         send to every channel in a profile
 */
import { Router } from 'express';
import { readFileSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';
import Ajv from 'ajv';
import addFormats from 'ajv-formats';

import { push }                                   from './wns-client.js';
import { upsertChannel, listChannels, removeChannel, pruneExpired, getChannel } from './channel-store.js';
import { upsertProfile, listProfiles, removeProfile, getProfile, importBundle }  from './profile-store.js';

const __dirname = dirname(fileURLToPath(import.meta.url));
const router    = Router();

// JSON Schema validator for import payloads
const ajv = new Ajv({ allErrors: true });
addFormats(ajv);
const importSchema = JSON.parse(
  readFileSync(join(__dirname, 'schemas', 'import-schema.json'), 'utf8')
);
const validateImport = ajv.compile(importSchema);

// ── Profiles ──────────────────────────────────────────────────────────────────

router.post('/profiles', (req, res) => {
  try {
    const profile = upsertProfile(req.body);
    res.status(201).json({ ok: true, profile });
  } catch (e) {
    res.status(400).json({ error: e.message });
  }
});

router.get('/profiles', (_req, res) => {
  res.json({ profiles: listProfiles() });
});

router.delete('/profiles/:id', (req, res) => {
  const removed = removeProfile(req.params.id);
  res.json({ ok: removed, message: removed ? 'Profile removed.' : 'Profile not found.' });
});

router.post('/profiles/import', (req, res) => {
  const bundle = req.body;
  const valid  = validateImport(bundle);
  if (!valid) {
    return res.status(400).json({ error: 'Invalid bundle schema', details: validateImport.errors });
  }
  const results = importBundle(bundle);
  const failed  = results.filter(r => !r.ok);
  res.status(failed.length > 0 ? 207 : 200).json({ results });
});

// ── Channels ──────────────────────────────────────────────────────────────────

router.post('/channels', (req, res) => {
  const { profile_id, channel_id, channel_uri, device_id, device_name, expires_at_ms, tags } = req.body;
  if (!profile_id) return res.status(400).json({ error: 'profile_id required' });
  if (!channel_id) return res.status(400).json({ error: 'channel_id required' });
  if (!channel_uri) return res.status(400).json({ error: 'channel_uri required' });
  if (!getProfile(profile_id)) return res.status(404).json({ error: 'Profile not found' });

  const ch = upsertChannel(profile_id, { channel_id, channel_uri, device_id, device_name, expires_at_ms, tags });
  res.status(201).json({ ok: true, channel: ch });
});

router.get('/channels', (req, res) => {
  const { profile_id } = req.query;
  if (!profile_id) return res.status(400).json({ error: 'profile_id query param required' });
  res.json({ channels: listChannels(profile_id) });
});

router.delete('/channels/:id', (req, res) => {
  const { profile_id } = req.query;
  if (!profile_id) return res.status(400).json({ error: 'profile_id query param required' });
  const removed = removeChannel(profile_id, req.params.id);
  res.json({ ok: removed, message: removed ? 'Channel removed.' : 'Channel not found.' });
});

router.post('/channels/prune', (_req, res) => {
  const count = pruneExpired();
  res.json({ ok: true, pruned: count });
});

// ── Send ──────────────────────────────────────────────────────────────────────

/**
 * POST /api/wns/send
 * Body: { profile_id, channel_id, type, payload, ttl?, tag?, group? }
 */
router.post('/send', async (req, res) => {
  const { profile_id, channel_id, type = 'toast', payload, ttl, tag, group } = req.body;
  if (!profile_id) return res.status(400).json({ error: 'profile_id required' });
  if (!channel_id) return res.status(400).json({ error: 'channel_id required' });
  if (!payload)    return res.status(400).json({ error: 'payload required' });

  const profile = getProfile(profile_id);
  if (!profile) return res.status(404).json({ error: 'Profile not found' });

  const channel = getChannel(profile_id, channel_id);
  if (!channel) return res.status(404).json({ error: 'Channel not found' });

  try {
    const result = await push({
      packageSid:   profile.package_sid,
      clientSecret: profile.client_secret,
      channelUri:   channel.channel_uri,
      type,
      payload,
      ttl,
      tag,
      group,
    });
    res.json({ ok: true, ...result });
  } catch (e) {
    const status = e.status ?? 500;
    res.status(status >= 400 && status < 600 ? status : 500)
       .json({ error: e.message, wns_error: e.wnsCode });
  }
});

/**
 * POST /api/wns/broadcast
 * Body: { profile_id, type, payload, ttl?, tag?, group? }
 * Sends to every channel registered under the profile.
 */
router.post('/broadcast', async (req, res) => {
  const { profile_id, type = 'toast', payload, ttl, tag, group } = req.body;
  if (!profile_id) return res.status(400).json({ error: 'profile_id required' });
  if (!payload)    return res.status(400).json({ error: 'payload required' });

  const profile  = getProfile(profile_id);
  if (!profile) return res.status(404).json({ error: 'Profile not found' });

  const channels = listChannels(profile_id);
  if (channels.length === 0) return res.json({ ok: true, sent: 0, results: [] });

  const results = await Promise.allSettled(
    channels.map(ch =>
      push({
        packageSid:   profile.package_sid,
        clientSecret: profile.client_secret,
        channelUri:   ch.channel_uri,
        type,
        payload,
        ttl,
        tag,
        group,
      }).then(r => ({ channel_id: ch.channel_id, ok: true, ...r }))
        .catch(e => ({ channel_id: ch.channel_id, ok: false, error: e.message }))
    )
  );

  const rows = results.map(r => r.value ?? r.reason);
  const sent = rows.filter(r => r.ok).length;
  res.json({ ok: true, sent, total: channels.length, results: rows });
});

export default router;
