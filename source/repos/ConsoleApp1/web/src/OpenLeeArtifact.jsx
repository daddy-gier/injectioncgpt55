import React, { useState, useCallback, useMemo, memo } from 'react';
import { callClaude } from './api';

const MODELS = [
  { id: 'claude', label: 'Claude', color: '#d97706' },
  { id: 'chatgpt', label: 'ChatGPT', color: '#16a34a' },
  { id: 'mistral', label: 'Mistral', color: '#7c3aed' },
  { id: 'grok', label: 'Grok', color: '#0ea5e9' },
  { id: 'ludus', label: 'Ludus-AI', color: '#dc2626' },
  { id: 'openclaw', label: 'OpenClaw', color: '#6d28d9' },
];

const GPU_NODES = [
  { id: 'OZARK-01', cpu: 'Ryzen 5 7600X', gpu: 'RTX 4070', vram: 12 },
  { id: 'OZARK-02', cpu: 'Ryzen 7 5800X', gpu: 'RTX 3080', vram: 10 },
  { id: 'OZARK-03', cpu: 'Intel i9-13900K', gpu: 'RTX 4090', vram: 24 },
  { id: 'OZARK-04', cpu: 'Ryzen 9 5950X', gpu: 'RTX 3090', vram: 24 },
  { id: 'OZARK-05', cpu: 'Intel i7-12700K', gpu: 'GTX 1080 Ti', vram: 11 },
  { id: 'OZARK-06', cpu: 'Ryzen 5 5600X', gpu: 'RTX 3060', vram: 12 },
];

const DEMO = {
  chatgpt: 'ChatGPT: Integrated analysis of the query across neural pathways.',
  mistral: 'Mistral: Contextual synthesis completed with domain weighting.',
  grok: 'Grok: Real-time pattern matching applied. Output verified.',
  ludus: 'Ludus-AI: Game-dev specialization applied. UE5 context confirmed.',
  openclaw: 'OpenClaw: Multi-agent consensus reached. Delegating to muscles.',
};

const LoadingDots = memo(() => {
  const [frame, setFrame] = React.useState(0);
  React.useEffect(() => {
    const t = setInterval(() => setFrame(f => (f + 1) % 4), 400);
    return () => clearInterval(t);
  }, []);
  return <span>{'.'.repeat(frame)}&nbsp;</span>;
});

const GpuBar = memo(({ load }) => {
  const color = load > 80 ? '#dc2626' : load > 50 ? '#d97706' : '#16a34a';
  return (
    <div style={{ background: '#1a1a1c', borderRadius: 4, overflow: 'hidden', height: 6 }}>
      <div style={{ width: `${load}%`, height: '100%', background: color, transition: 'width .3s' }} />
    </div>
  );
});

export default function OpenLeeArtifact() {
  const [tab, setTab] = useState('oracle');
  const [prompt, setPrompt] = useState('');
  const [running, setRunning] = useState(false);
  const [states, setStates] = useState(() => Object.fromEntries(MODELS.map(m => [m.id, 'idle'])));
  const [responses, setResponses] = useState(() => Object.fromEntries(MODELS.map(m => [m.id, ''])));
  const [synthesis, setSynthesis] = useState('');
  const [queryLog, setQueryLog] = useState([]);
  const [gpuLoads, setGpuLoads] = useState(() => GPU_NODES.map(() => Math.floor(Math.random() * 40 + 20)));

  React.useEffect(() => {
    const t = setInterval(() => setGpuLoads(ls => ls.map(l => Math.max(5, Math.min(95, l + (Math.random() * 10 - 5))))), 2000);
    return () => clearInterval(t);
  }, []);

  const doneCount = useMemo(() => MODELS.filter(m => states[m.id] === 'done' || states[m.id] === 'error').length, [states]);
  const progress = useMemo(() => Math.round((doneCount / MODELS.length) * 100), [doneCount]);

  const runQuery = useCallback(async () => {
    if (!prompt.trim() || running) return;
    setRunning(true);
    setSynthesis('');
    setStates(Object.fromEntries(MODELS.map(m => [m.id, 'loading'])));
    setResponses(Object.fromEntries(MODELS.map(m => [m.id, ''])));

    const q = prompt.trim();
    setQueryLog(prev => [{ ts: new Date().toLocaleTimeString(), q }, ...prev].slice(0, 10));

    const modelPromises = MODELS.map(async ({ id }) => {
      try {
        let text;
        if (id === 'claude') {
          text = await callClaude(q, 90000);
        } else {
          await new Promise(r => setTimeout(r, 800 + Math.random() * 1200));
          text = DEMO[id];
        }
        setStates(s => ({ ...s, [id]: 'done' }));
        setResponses(r => ({ ...r, [id]: text }));
      } catch {
        setStates(s => ({ ...s, [id]: 'error' }));
        setResponses(r => ({ ...r, [id]: 'Error reaching model.' }));
      }
    });

    await Promise.allSettled(modelPromises);

    try {
      const synthPrompt = `Synthesize these AI responses into one unified answer:\n\nUser question: ${q}\n\n${MODELS.map(m => `${m.label}: ${responses[m.id] || DEMO[m.id] || ''}`).join('\n\n')}`;
      const synth = await callClaude(synthPrompt, 90000, { system: 'You are the synthesis engine. Combine the responses into one clear, concise answer.' });
      setSynthesis(synth);
    } catch {
      setSynthesis('Synthesis unavailable.');
    }

    setRunning(false);
    setPrompt('');
  }, [prompt, running, responses]);

  const handleKey = useCallback((e) => {
    if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') runQuery();
  }, [runQuery]);

  const styles = {
    root: { background: '#050507', color: '#c9c3b5', fontFamily: 'JetBrains Mono, monospace', minHeight: '100vh', padding: 24 },
    header: { fontSize: 28, fontWeight: 700, color: '#8b0a1a', letterSpacing: '0.1em', marginBottom: 4 },
    sub: { fontSize: 11, color: '#44444a', letterSpacing: '0.18em', textTransform: 'uppercase', marginBottom: 24 },
    tabs: { display: 'flex', gap: 8, marginBottom: 24 },
    tab: (active) => ({ background: active ? '#8b0a1a' : '#1a1a1c', color: active ? '#e8e2d4' : '#44444a', border: 'none', padding: '6px 16px', cursor: 'pointer', borderRadius: 4, fontSize: 12, letterSpacing: '0.1em', textTransform: 'uppercase' }),
    input: { width: '100%', background: '#111114', border: '1px solid #2a2a2d', color: '#c9c3b5', padding: '12px 16px', fontSize: 14, borderRadius: 4, resize: 'vertical', minHeight: 80, fontFamily: 'inherit', boxSizing: 'border-box' },
    btn: (disabled) => ({ background: disabled ? '#2a2a2d' : '#8b0a1a', color: disabled ? '#44444a' : '#e8e2d4', border: 'none', padding: '10px 24px', cursor: disabled ? 'not-allowed' : 'pointer', borderRadius: 4, fontSize: 12, letterSpacing: '0.1em', textTransform: 'uppercase', marginTop: 8 }),
    progressBar: { background: '#1a1a1c', borderRadius: 4, overflow: 'hidden', height: 4, marginBottom: 24 },
    progressFill: { height: '100%', background: '#8b0a1a', transition: 'width .3s', width: `${progress}%` },
    grid: { display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 12, marginBottom: 24 },
    card: (state, color) => ({ background: '#0a0a0b', border: `1px solid ${state === 'done' ? color : state === 'error' ? '#dc2626' : '#2a2a2d'}`, borderRadius: 6, padding: 12 }),
    cardLabel: (color) => ({ fontSize: 10, color, letterSpacing: '0.18em', textTransform: 'uppercase', marginBottom: 8 }),
    cardText: { fontSize: 12, color: '#c9c3b5', lineHeight: 1.5, maxHeight: 80, overflow: 'hidden', textOverflow: 'ellipsis' },
    synthesis: { background: '#0a0a0b', border: '1px solid #4a0610', borderRadius: 6, padding: 16, marginBottom: 24 },
    synthLabel: { fontSize: 10, color: '#8b0a1a', letterSpacing: '0.18em', textTransform: 'uppercase', marginBottom: 8 },
    synthText: { fontSize: 14, color: '#e8e2d4', lineHeight: 1.6 },
    nodeGrid: { display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 12 },
    node: { background: '#0a0a0b', border: '1px solid #2a2a2d', borderRadius: 6, padding: 12 },
    nodeId: { fontSize: 10, color: '#8b0a1a', letterSpacing: '0.18em', marginBottom: 4 },
    nodeMeta: { fontSize: 11, color: '#44444a', marginBottom: 4 },
    logItem: { background: '#0a0a0b', border: '1px solid #1a1a1c', borderRadius: 4, padding: 8, marginBottom: 6 },
    logTs: { fontSize: 10, color: '#44444a', marginBottom: 2 },
    logQ: { fontSize: 12, color: '#c9c3b5' },
  };

  return (
    <div style={styles.root}>
      <div style={styles.header}>OPEN-LEE</div>
      <div style={styles.sub}>Multi-AI Consensus Engine · Nyghtshade Hollow</div>
      <div style={styles.tabs}>
        {['oracle', 'buddy', 'log'].map(t => (
          <button key={t} style={styles.tab(tab === t)} onClick={() => setTab(t)}>
            {t === 'oracle' ? 'Oracle Engine' : t === 'buddy' ? 'Buddy System' : 'Query Log'}
          </button>
        ))}
      </div>

      {tab === 'oracle' && (
        <>
          <textarea
            style={styles.input}
            value={prompt}
            onChange={e => setPrompt(e.target.value)}
            onKeyDown={handleKey}
            placeholder="Enter query… (Ctrl+Enter to run)"
          />
          <button style={styles.btn(!prompt.trim() || running)} onClick={runQuery} disabled={!prompt.trim() || running}>
            {running ? 'Running…' : 'Run Query'}
          </button>
          {running && <div style={{ ...styles.progressBar, marginTop: 16 }}><div style={styles.progressFill} /></div>}
          <div style={styles.grid}>
            {MODELS.map(m => (
              <div key={m.id} style={styles.card(states[m.id], m.color)}>
                <div style={styles.cardLabel(m.color)}>{m.label}</div>
                <div style={styles.cardText}>
                  {states[m.id] === 'loading' ? <LoadingDots /> : responses[m.id] || <span style={{ color: '#44444a' }}>Idle</span>}
                </div>
              </div>
            ))}
          </div>
          {synthesis && (
            <div style={styles.synthesis}>
              <div style={styles.synthLabel}>Synthesis</div>
              <div style={styles.synthText}>{synthesis}</div>
            </div>
          )}
        </>
      )}

      {tab === 'buddy' && (
        <div style={styles.nodeGrid}>
          {GPU_NODES.map((node, i) => (
            <div key={node.id} style={styles.node}>
              <div style={styles.nodeId}>{node.id}</div>
              <div style={styles.nodeMeta}>{node.cpu}</div>
              <div style={styles.nodeMeta}>{node.gpu} · {node.vram}GB VRAM</div>
              <GpuBar load={Math.round(gpuLoads[i])} />
              <div style={{ ...styles.nodeMeta, marginTop: 4 }}>{Math.round(gpuLoads[i])}% GPU load</div>
            </div>
          ))}
        </div>
      )}

      {tab === 'log' && (
        <>
          {queryLog.length === 0 && <div style={{ color: '#44444a', fontSize: 12 }}>No queries yet.</div>}
          {queryLog.map((entry, i) => (
            <div key={i} style={styles.logItem}>
              <div style={styles.logTs}>{entry.ts}</div>
              <div style={styles.logQ}>{entry.q}</div>
            </div>
          ))}
        </>
      )}
    </div>
  );
}
