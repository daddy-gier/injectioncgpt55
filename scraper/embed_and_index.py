"""
Reads chunked UE docs from data/ue_docs/, embeds them with the Anthropic API,
and stores them in a local SQLite vector store using sqlite-vec.

Usage:
    pip install anthropic sqlite-vec
    python embed_and_index.py

Then update backend/main.py to query this index at request time.
"""

import json
import os
import sqlite3
import struct
import anthropic

DOCS_DIR = os.path.join(os.path.dirname(__file__), "..", "data", "ue_docs")
DB_PATH = os.path.join(os.path.dirname(__file__), "..", "data", "ue_vectors.db")
EMBED_MODEL = "voyage-3"  # Anthropic's recommended embedding model
EMBED_DIM = 1024
BATCH_SIZE = 50


def serialize_vector(vec: list[float]) -> bytes:
    return struct.pack(f"{len(vec)}f", *vec)


def setup_db(conn: sqlite3.Connection):
    conn.execute("CREATE TABLE IF NOT EXISTS docs (id TEXT PRIMARY KEY, url TEXT, title TEXT, text TEXT)")
    conn.execute(
        f"CREATE VIRTUAL TABLE IF NOT EXISTS vss_docs USING vss0(embedding({EMBED_DIM}))"
    )


def load_chunks() -> list[dict]:
    chunks = []
    if not os.path.isdir(DOCS_DIR):
        print(f"No docs found at {DOCS_DIR}. Run fetch_ue_docs.py first.")
        return chunks
    for fname in sorted(os.listdir(DOCS_DIR)):
        if fname.endswith(".json"):
            with open(os.path.join(DOCS_DIR, fname)) as f:
                chunks.append(json.load(f))
    return chunks


def embed_batch(client: anthropic.Anthropic, texts: list[str]) -> list[list[float]]:
    # Anthropic embeddings via Voyage
    response = client.embeddings.create(
        model=EMBED_MODEL,
        input=texts,
    )
    return [r.embedding for r in response.data]


def main():
    client = anthropic.Anthropic(api_key=os.environ["ANTHROPIC_API_KEY"])
    chunks = load_chunks()
    if not chunks:
        return

    conn = sqlite3.connect(DB_PATH)
    try:
        conn.enable_load_extension(True)
        conn.load_extension("sqlite_vec")
    except Exception:
        print("sqlite-vec extension not available. Install: pip install sqlite-vec")
        return

    setup_db(conn)

    already = {row[0] for row in conn.execute("SELECT id FROM docs")}
    pending = [c for c in chunks if c["id"] not in already]
    print(f"{len(pending)} chunks to embed ({len(already)} already indexed)")

    for i in range(0, len(pending), BATCH_SIZE):
        batch = pending[i : i + BATCH_SIZE]
        texts = [f"{c['title']}\n\n{c['text']}" for c in batch]
        embeddings = embed_batch(client, texts)

        for chunk, emb in zip(batch, embeddings):
            conn.execute(
                "INSERT OR IGNORE INTO docs (id, url, title, text) VALUES (?,?,?,?)",
                (chunk["id"], chunk["url"], chunk["title"], chunk["text"]),
            )
            rowid = conn.execute("SELECT rowid FROM docs WHERE id=?", (chunk["id"],)).fetchone()[0]
            conn.execute(
                "INSERT INTO vss_docs (rowid, embedding) VALUES (?,?)",
                (rowid, serialize_vector(emb)),
            )

        conn.commit()
        print(f"  Embedded {min(i + BATCH_SIZE, len(pending))}/{len(pending)}")

    conn.close()
    print(f"Done. Index saved to {DB_PATH}")


if __name__ == "__main__":
    main()
