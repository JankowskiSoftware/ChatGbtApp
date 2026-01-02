# Global Job Match Analyzer

A system for **mass-analyzing job offers worldwide** and ranking them against a candidate’s career strategy.  
It collects job postings from aggregators, downloads full job descriptions, and uses an LLM-driven pipeline to **evaluate fit, seniority, and compensation** — storing structured results in a local database for search, filtering, and iteration.

The goal is to reduce the time cost of job searching by turning a noisy stream of postings into a **high-signal shortlist**, while keeping LLM usage efficient and scalable.

---

## What this project does

- Scrapes / ingests job links from job aggregators
- Downloads job descriptions at scale (high parallelism)
- Evaluates each role against criteria such as:
  - distributed systems / microservices exposure
  - .NET backend relevance
  - seniority match (e.g., “senior” vs expectations)
  - salary estimation / compensation plausibility
- Stores structured evaluation results in **SQLite**
- Optimizes cost by using a **two-stage LLM workflow** (cheap filter → deeper analysis only when needed)

---

## Key idea: Cost-efficient LLM evaluation (Nano → Mini)

This project uses a staged approach to control cost and latency:

### Stage 0 — Fast pre-filter (non-LLM)
- Keyword matching / heuristics to remove obvious mismatches early
- Duplicate detection to avoid reprocessing the same posting
- Immutability approach: once a posting is stored, repeated runs don’t mutate previous results (new results create new records / versions rather than overwriting)

### Stage 1 — Lightweight LLM screening (GPT-5.2 nano)
A small prompt checks if the offer is worth deeper analysis (e.g., role type, seniority hints, core stack match).

### Stage 2 — Deep evaluation (GPT-5.2 mini)
Only triggered for postings that pass Stage 1. This step produces a richer structured analysis (fit scoring, reasoning, salary estimate, risks, and actionable notes).

---

## High-level workflow

1. **Collect links** from job boards / aggregators  
2. **Fetch job pages** and extract the job description  
3. **Pre-filter** (keywords + dedup)  
4. **Stage 1 LLM screen** (GPT-5.2 nano)  
5. If promising → **Stage 2 deep analysis** (GPT-5.2 mini)  
6. **Persist results** into SQLite for later review / querying  

---

## Tech stack

- **Language & Framework**: C# 10 (.NET 10.0)
- **LLM**: ChatGPT API (two-stage prompts: GPT-5.2 nano → GPT-5.2 mini)
- **Database**: SQLite (via Entity Framework Core)
- **Concurrency**: High-parallel fetching and processing with Polly (resilience/retry policies)
- **Scraping/Crawling**: Playwright for browser automation, HtmlAgilityPack for HTML parsing
- **Data Access**: Entity Framework Core with migrations
- **DI & Logging**: Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging
- **Data Mapping**: AutoMapper for DTO transformations
- **Data model**: Immutable-ish storage with dedup/versioning semantics

---

---

## Data stored (SQLite)

Typical records include:
- job source + canonical URL
- timestamps (seen, fetched, analyzed)
- raw extracted description (or normalized text)
- screening result (nano)
- deep evaluation result (mini)
- salary estimate and confidence
- tags (e.g., “distributed-systems”, “.net-backend”, “seniority-mismatch”)
- dedup hashes / fingerprints

