"""
Crawls Unreal Engine documentation pages and saves them as chunked text
for embedding into a vector database.

Usage:
    pip install requests beautifulsoup4 tiktoken
    python fetch_ue_docs.py
"""

import json
import os
import time
import hashlib
from urllib.parse import urljoin, urlparse
import requests
from bs4 import BeautifulSoup

OUTPUT_DIR = os.path.join(os.path.dirname(__file__), "..", "data", "ue_docs")
CHUNK_SIZE = 512  # tokens approx
DELAY = 1.0  # seconds between requests — be polite

SEED_URLS = [
    "https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-5-migration-guide",
    "https://dev.epicgames.com/documentation/en-us/unreal-engine/programming-and-scripting-in-unreal-engine",
    "https://dev.epicgames.com/documentation/en-us/unreal-engine/blueprints-visual-scripting-in-unreal-engine",
    "https://dev.epicgames.com/documentation/en-us/unreal-engine/rendering-and-graphics-in-unreal-engine",
    "https://dev.epicgames.com/documentation/en-us/unreal-engine/physics-in-unreal-engine",
    "https://dev.epicgames.com/documentation/en-us/unreal-engine/networking-and-multiplayer-in-unreal-engine",
    "https://dev.epicgames.com/documentation/en-us/unreal-engine/animation-system-in-unreal-engine",
    "https://dev.epicgames.com/documentation/en-us/unreal-engine/artificial-intelligence-in-unreal-engine",
    "https://dev.epicgames.com/documentation/en-us/unreal-engine/audio-in-unreal-engine",
    "https://dev.epicgames.com/documentation/en-us/unreal-engine/world-building-in-unreal-engine",
    "https://dev.epicgames.com/documentation/en-us/unreal-engine/gameplay-framework-in-unreal-engine",
    "https://dev.epicgames.com/documentation/en-us/unreal-engine/testing-and-optimizing-your-content",
]

ALLOWED_DOMAIN = "dev.epicgames.com"
MAX_PAGES = 2000


def chunk_text(text: str, max_chars: int = 2000) -> list[str]:
    paragraphs = [p.strip() for p in text.split("\n\n") if p.strip()]
    chunks, current = [], ""
    for para in paragraphs:
        if len(current) + len(para) > max_chars and current:
            chunks.append(current.strip())
            current = para
        else:
            current += "\n\n" + para
    if current.strip():
        chunks.append(current.strip())
    return chunks


def extract_text(soup: BeautifulSoup, url: str) -> tuple[str, str]:
    title_tag = soup.find("h1") or soup.find("title")
    title = title_tag.get_text(strip=True) if title_tag else url

    for tag in soup(["script", "style", "nav", "footer", "aside", "header"]):
        tag.decompose()

    main = soup.find("main") or soup.find("article") or soup.find("body")
    text = main.get_text(separator="\n", strip=True) if main else ""
    return title, text


def get_links(soup: BeautifulSoup, base_url: str) -> list[str]:
    links = []
    for a in soup.find_all("a", href=True):
        href = urljoin(base_url, a["href"])
        parsed = urlparse(href)
        if (
            parsed.netloc == ALLOWED_DOMAIN
            and "/documentation/en-us/unreal-engine/" in parsed.path
            and not parsed.fragment
        ):
            links.append(href.split("?")[0])
    return list(set(links))


def crawl():
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    visited = set()
    queue = list(SEED_URLS)
    doc_id = 0

    session = requests.Session()
    session.headers["User-Agent"] = "UEDocBot/1.0 (research scraper)"

    print(f"Starting crawl — output: {OUTPUT_DIR}")

    while queue and len(visited) < MAX_PAGES:
        url = queue.pop(0)
        if url in visited:
            continue
        visited.add(url)

        try:
            resp = session.get(url, timeout=15)
            if resp.status_code != 200:
                print(f"  SKIP {resp.status_code}: {url}")
                continue
        except Exception as e:
            print(f"  ERROR {url}: {e}")
            continue

        soup = BeautifulSoup(resp.text, "html.parser")
        title, text = extract_text(soup, url)

        if len(text) < 100:
            time.sleep(DELAY)
            continue

        chunks = chunk_text(text)
        for i, chunk in enumerate(chunks):
            record = {
                "id": f"ue_doc_{doc_id}",
                "url": url,
                "title": title,
                "chunk_index": i,
                "total_chunks": len(chunks),
                "text": chunk,
            }
            fname = os.path.join(OUTPUT_DIR, f"{doc_id:06d}.json")
            with open(fname, "w", encoding="utf-8") as f:
                json.dump(record, f, ensure_ascii=False, indent=2)
            doc_id += 1

        new_links = get_links(soup, url)
        queue.extend(l for l in new_links if l not in visited)

        print(f"[{len(visited)}/{MAX_PAGES}] {title[:60]} — {len(chunks)} chunks")
        time.sleep(DELAY)

    print(f"\nDone. {doc_id} chunks saved to {OUTPUT_DIR}")


if __name__ == "__main__":
    crawl()
