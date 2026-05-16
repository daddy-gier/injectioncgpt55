import express from 'express';
import cors from 'cors';
import { config } from 'dotenv';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';

const __dirname = dirname(fileURLToPath(import.meta.url));
config({ path: join(__dirname, '.env') });

const app = express();
const PORT = process.env.PORT || 3001;
const ANTHROPIC_API_KEY = process.env.ANTHROPIC_API_KEY;

app.use(cors());
app.use(express.json());

if (!ANTHROPIC_API_KEY) {
  console.warn('[WARN] ANTHROPIC_API_KEY not set — /api/claude requests will fail');
}

async function callClaude(system, userPrompt, maxTokens = 1000) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 90000);
  try {
    const response = await fetch('https://api.anthropic.com/v1/messages', {
      method: 'POST',
      headers: {
        'x-api-key': ANTHROPIC_API_KEY,
        'anthropic-version': '2023-06-01',
        'content-type': 'application/json',
      },
      body: JSON.stringify({
        model: 'claude-sonnet-4-6',
        max_tokens: maxTokens,
        system,
        messages: [{ role: 'user', content: userPrompt }],
      }),
      signal: controller.signal,
    });
    if (!response.ok) {
      const err = await response.text();
      throw new Error(`Anthropic API error ${response.status}: ${err}`);
    }
    const data = await response.json();
    return data.content?.[0]?.text ?? '';
  } finally {
    clearTimeout(timeout);
  }
}

// ── Health ────────────────────────────────────────────────────────────────────

app.get('/api/health', (_req, res) => {
  res.json({ status: 'ok', claude: !!ANTHROPIC_API_KEY, ts: Date.now() });
});

// ── Claude proxy ──────────────────────────────────────────────────────────────

app.post('/api/claude', async (req, res) => {
  if (!ANTHROPIC_API_KEY) {
    return res.status(500).json({ error: 'ANTHROPIC_API_KEY not configured' });
  }
  const { prompt, max_tokens = 1000, system = 'You are CLAUDE NODE, part of a multi-AI system.' } = req.body;
  if (!prompt) return res.status(400).json({ error: 'prompt required' });
  try {
    const text = await callClaude(system, prompt, max_tokens);
    res.json({ text });
  } catch (err) {
    const aborted = err.name === 'AbortError';
    res.status(aborted ? 504 : 500).json({ error: aborted ? 'Request timed out' : err.message });
  }
});

// ── Game: NPC Dialogue ────────────────────────────────────────────────────────

app.post('/api/game/npc-dialogue', async (req, res) => {
  if (!ANTHROPIC_API_KEY) return res.status(500).json({ error: 'ANTHROPIC_API_KEY not configured' });
  const { characterName, personality, backstory, sceneContext, playerInput, conversationHistory = [] } = req.body;
  if (!characterName || !playerInput) return res.status(400).json({ error: 'characterName and playerInput required' });

  const system = `You are ${characterName}, an NPC in Nyghtshade Hollow prison.
Personality: ${personality || 'guarded, observant'}
Backstory: ${backstory || 'long-timer who knows the prison inside out'}
Stay in character. Respond naturally, 1-3 sentences. Never break the fourth wall.`;

  const history = conversationHistory.map(e => `${e.speaker}: ${e.text}`).join('\n');
  const userPrompt = history ? `${history}\nPlayer: ${playerInput}` : `Player: ${playerInput}`;

  try {
    const text = await callClaude(system, userPrompt, 300);
    res.json({ dialogue: text, character: characterName });
  } catch (err) {
    const aborted = err.name === 'AbortError';
    res.status(aborted ? 504 : 500).json({ error: aborted ? 'Request timed out' : err.message });
  }
});

// ── Game: Story Event ─────────────────────────────────────────────────────────

app.post('/api/game/story-event', async (req, res) => {
  if (!ANTHROPIC_API_KEY) return res.status(500).json({ error: 'ANTHROPIC_API_KEY not configured' });
  const { eventName, playerChoice, worldContext, storyFlags = [] } = req.body;
  if (!eventName || !playerChoice) return res.status(400).json({ error: 'eventName and playerChoice required' });

  const system = `You are the narrative engine for Nyghtshade Hollow, a gritty prison RPG.
Resolve story events and return a JSON object with:
{ outcome, narrative, flagsToSet, flagsToClear, suspicionDelta, influenceDelta, hint }
Keep narrative to 2-3 sentences. Be concise and dramatic.`;

  const userPrompt = `Event: ${eventName}
Player choice: ${playerChoice}
World context: ${worldContext || 'standard prison day'}
Active story flags: ${storyFlags.join(', ') || 'none'}`;

  try {
    const raw = await callClaude(system, userPrompt, 500);
    let result;
    try { result = JSON.parse(raw); } catch { result = { outcome: 'neutral', narrative: raw, flagsToSet: [], flagsToClear: [], suspicionDelta: 0, influenceDelta: 0 }; }
    res.json(result);
  } catch (err) {
    const aborted = err.name === 'AbortError';
    res.status(aborted ? 504 : 500).json({ error: aborted ? 'Request timed out' : err.message });
  }
});

// ── Game: World State ─────────────────────────────────────────────────────────

const worldState = {
  currentChapter: 1,
  storyFlags: [],
  playerChoices: [],
  npcRelationships: {},
  suspicion: 0,
  influence: 0,
};

app.get('/api/game/world-state', (_req, res) => res.json(worldState));

app.post('/api/game/world-state', (req, res) => {
  Object.assign(worldState, req.body);
  res.json({ ok: true, worldState });
});

// ── Game: NPC Relationship ────────────────────────────────────────────────────

app.post('/api/game/npc-relationship', (req, res) => {
  const { npcId, tier } = req.body;
  if (!npcId || !tier) return res.status(400).json({ error: 'npcId and tier required' });
  worldState.npcRelationships[npcId] = tier;
  res.json({ ok: true, npcId, tier });
});

// ── OpenClaw muscles ──────────────────────────────────────────────────────────

const OLLAMA_URL = process.env.OLLAMA_URL || 'http://localhost:11434/v1';

async function callOllama(model, prompt, maxTokens = 1000) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 90000);
  try {
    const response = await fetch(`${OLLAMA_URL}/chat/completions`, {
      method: 'POST',
      headers: { 'content-type': 'application/json', authorization: 'Bearer ollama' },
      body: JSON.stringify({ model, messages: [{ role: 'user', content: prompt }], max_tokens: maxTokens }),
      signal: controller.signal,
    });
    if (!response.ok) throw new Error(`Ollama error ${response.status}`);
    const data = await response.json();
    return data.choices?.[0]?.message?.content ?? '';
  } finally {
    clearTimeout(timeout);
  }
}

app.get('/api/openclaw/status', async (_req, res) => {
  let ollamaOk = false;
  try {
    const r = await fetch(`${OLLAMA_URL}/models`, { signal: AbortSignal.timeout(3000) });
    ollamaOk = r.ok;
  } catch {}
  res.json({
    status: 'ok',
    brain: { provider: 'anthropic', model: 'claude-sonnet-4-6', available: !!ANTHROPIC_API_KEY },
    muscles: {
      CHARLIE: { model: 'qwen2.5-coder:14b', role: 'coder', available: ollamaOk },
      SCOUT: { model: 'qwen3:14b', role: 'researcher', available: ollamaOk },
      SPEEDY: { model: 'mistral:7b', role: 'fast_tasks', available: ollamaOk },
    },
    ollamaUrl: OLLAMA_URL,
  });
});

app.post('/api/openclaw/brain', async (req, res) => {
  if (!ANTHROPIC_API_KEY) return res.status(500).json({ error: 'ANTHROPIC_API_KEY not configured' });
  const { prompt, system = 'You are the Brain orchestrator for OpenClaw.' } = req.body;
  if (!prompt) return res.status(400).json({ error: 'prompt required' });
  try {
    const text = await callClaude(system, prompt);
    res.json({ text, muscle: 'brain', model: 'claude-sonnet-4-6' });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

app.post('/api/openclaw/charlie', async (req, res) => {
  const { prompt } = req.body;
  if (!prompt) return res.status(400).json({ error: 'prompt required' });
  try {
    const text = await callOllama('qwen2.5-coder:14b', prompt);
    res.json({ text, muscle: 'CHARLIE', model: 'qwen2.5-coder:14b' });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

app.post('/api/openclaw/scout', async (req, res) => {
  const { prompt } = req.body;
  if (!prompt) return res.status(400).json({ error: 'prompt required' });
  try {
    const text = await callOllama('qwen3:14b', prompt);
    res.json({ text, muscle: 'SCOUT', model: 'qwen3:14b' });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

app.post('/api/openclaw/speedy', async (req, res) => {
  const { prompt } = req.body;
  if (!prompt) return res.status(400).json({ error: 'prompt required' });
  try {
    const text = await callOllama('mistral:7b', prompt);
    res.json({ text, muscle: 'SPEEDY', model: 'mistral:7b' });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

// ── Start ─────────────────────────────────────────────────────────────────────

app.listen(PORT, () => {
  console.log(`[OPEN-LEE] server running on port ${PORT}`);
  console.log(`  GET  /api/health`);
  console.log(`  POST /api/claude`);
  console.log(`  POST /api/game/npc-dialogue`);
  console.log(`  POST /api/game/story-event`);
  console.log(`  GET  /api/game/world-state`);
  console.log(`  POST /api/game/world-state`);
  console.log(`  POST /api/game/npc-relationship`);
  console.log(`  GET  /api/openclaw/status`);
  console.log(`  POST /api/openclaw/brain`);
  console.log(`  POST /api/openclaw/charlie`);
  console.log(`  POST /api/openclaw/scout`);
  console.log(`  POST /api/openclaw/speedy`);
});
