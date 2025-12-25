# Architecture Overview

## Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         JobsCrawler                              │
│  (Orchestrates parallel processing of multiple jobs)             │
│                                                                   │
│  - Parses URL list                                               │
│  - Manages parallelism (max 4 concurrent)                        │
│  - Tracks overall progress                                       │
└───────────────────────┬─────────────────────────────────────────┘
                        │
                        │ uses
                        ▼
┌─────────────────────────────────────────────────────────────────┐
│                       IJobProcessor                              │
│                     (Interface)                                  │
└───────────────────────┬─────────────────────────────────────────┘
                        │
                        │ implemented by
                        ▼
┌─────────────────────────────────────────────────────────────────┐
│                       JobProcessor                               │
│   (Processes single job: extract → analyze → parse → store)     │
│                                                                   │
│  1. Check if duplicate                                           │
│  2. Extract content via IPageContentExtractor                    │
│  3. Send to OpenAI for analysis                                  │
│  4. Parse AI response                                            │
│  5. Store in database                                            │
└───────────────────────┬─────────────────────────────────────────┘
                        │
                        │ uses
                        ▼
┌─────────────────────────────────────────────────────────────────┐
│                  IPageContentExtractor                           │
│                     (Interface)                                  │
│                                                                   │
│  + ExtractAsync(url) → PageExtractionResult                      │
│  + CanHandle(url) → bool                                         │
└───────────────────────┬─────────────────────────────────────────┘
                        │
         ┌──────────────┴──────────────┬──────────────┐
         │                              │              │
         ▼                              ▼              ▼
┌──────────────────┐      ┌──────────────────┐  ┌──────────────┐
│   Chromium       │      │   LinkedIn       │  │    Indeed    │
│ PageExtractor    │      │ PageExtractor    │  │PageExtractor │
│                  │      │                  │  │              │
│ - Uses Playwright│      │ - LinkedIn auth  │  │ - Simple HTTP│
│ - Handles login  │      │ - Custom selec.  │  │ - No auth    │
│ - loopcv.pro     │      │ - linkedin.com   │  │ - indeed.com │
└──────────────────┘      └──────────────────┘  └──────────────┘
```

## Data Flow

```
URLs (comma-separated string)
    │
    ▼
JobsCrawler.CrawlJobsAsync()
    │
    ├─ Parse URLs
    │
    ├─ Initialize progress tracking
    │
    └─ Parallel.ForEachAsync (max 4 parallel)
           │
           ▼
       JobProcessor.ProcessJobAsync(url)
           │
           ├─ Check duplicate (JobStorage)
           │      │
           │      └─ If duplicate → Skip
           │
           ├─ Extract content (IPageContentExtractor)
           │      │
           │      ├─ Select appropriate extractor (CanHandle)
           │      │
           │      ├─ Fetch page content
           │      │
           │      └─ Handle authentication if needed
           │
           ├─ AI Analysis (OpenAiApi)
           │      │
           │      └─ Send job description + prompt
           │
           ├─ Parse Response (GptKeyValueParser)
           │      │
           │      └─ Extract structured data
           │
           └─ Store Result (JobStorage)
                  │
                  └─ Save to database
```

## Class Responsibilities

### **JobsCrawler**
```
Responsibility: Orchestration & Parallelism
Input:  Comma-separated URLs
Output: None (side effects: DB updates)
Uses:   IJobProcessor, JobProcessingProgress
```

### **JobProcessor** 
```
Responsibility: Single job workflow
Input:  Single URL
Output: JobProcessingResult
Uses:   IPageContentExtractor, JobStorage, 
        OpenAiApi, GptKeyValueParser, Prompt
```

### **ChromiumPageExtractor**
```
Responsibility: Browser-based content extraction
Input:  Single URL
Output: PageExtractionResult
Uses:   Chromium (Playwright wrapper)
```

### **PageExtractorFactory**
```
Responsibility: Select appropriate extractor
Input:  URL
Output: IPageContentExtractor instance
Pattern: Chain of Responsibility
```

## Extension Points

### Adding a New Website Parser

```csharp
// 1. Create extractor
public class GlassdoorExtractor : IPageContentExtractor
{
    public bool CanHandle(string url) 
        => url.Contains("glassdoor.com");
    
    public async Task<PageExtractionResult> ExtractAsync(string url)
    {
        // Your extraction logic
    }
}

// 2. Register in DI
services.AddSingleton<IPageContentExtractor, GlassdoorExtractor>();

// 3. Done! System automatically uses it
```

### Using Multiple Extractors

```csharp
services.AddSingleton<IPageContentExtractor>(provider => 
{
    var extractors = new List<IPageContentExtractor>
    {
        new ChromiumPageExtractor(...),
        new LinkedInExtractor(...),
        new IndeedExtractor(...)
    };
    
    // Factory pattern or composite pattern
    return new CompositeExtractor(extractors);
});
```

## Error Handling Strategy

```
JobsCrawler
    └─ Catches: Any exceptions
    └─ Logs: Critical errors
    └─ Continues: Processing other URLs

JobProcessor
    └─ Catches: All exceptions
    └─ Returns: JobProcessingResult with error details
    └─ Never throws

IPageContentExtractor
    └─ Catches: Extraction errors
    └─ Returns: PageExtractionResult.Failed(message)
    └─ Never throws
```

## Thread Safety

- **JobsCrawler**: Safe (uses Parallel.ForEachAsync)
- **JobProcessor**: Safe (stateless, per-URL instance)
- **JobStorage**: Thread-safe (uses lock)
- **ChromiumPageExtractor**: Safe (each extraction is isolated)

## Performance Characteristics

| Operation | Time | Parallelism |
|-----------|------|-------------|
| URL Parsing | <1ms | Sequential |
| Duplicate Check | ~5ms | Parallel (DB) |
| Page Extraction | 2-5s | Parallel (max 4) |
| AI Analysis | 3-10s | Parallel (max 4) |
| Parsing | <10ms | Parallel |
| Storage | ~50ms | Parallel (locked) |
| **Total per job** | **5-15s** | **4 concurrent** |
