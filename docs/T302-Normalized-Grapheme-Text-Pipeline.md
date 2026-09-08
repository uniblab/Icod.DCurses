# T302 — Normalized Grapheme / Text-Element Pipeline

**Project:** `Icod.DCurses`  
**Development line:** `0.3.0`  
**Development version:** `0.3.0-alpha.2`  
**Tranche:** T302 — normalized grapheme/text-element pipeline  
**Dependency baseline:** `Icod.Terminal 1.0.0`; `Icod.TermInfo 1.10.0`  
**Status:** Implementation complete; validation active

---

## 1. Purpose

T302 makes one Unicode normalization and text-element segmentation path authoritative for
string writes before the later width-data, editing, and viewport APIs depend on it.

The `0.2` implementation already used runtime text-element enumeration, but malformed
UTF-16 normalization occurred inside the per-element write path. That meant segmentation
and normalization were separate responsibilities and made it difficult to state a single
terminal-cell text contract.

T302 centralizes those responsibilities in `CursesUnicodeText`.

## 2. Authoritative pipeline

`CursesUnicodeText` now owns:

1. deterministic malformed UTF-16 replacement with U+FFFD;
2. segmentation of the resulting well-formed text using the runtime Unicode text-element
   contract;
3. ordered delivery of those normalized text elements to the window writer.

`CursesWindow.Write(string, CursesStyle)` now consumes
`CursesUnicodeText.EnumerateTextElements(...)` directly.

The old duplicate `CursesWindow.NormalizeMalformedUtf16(...)` routine has been removed.
`WriteTextElementCore(...)` therefore receives already-normalized text elements and passes
them unchanged to the configured `ICursesTextWidthProvider`.

## 3. Why normalization precedes segmentation

Malformed UTF-16 is not itself a Unicode scalar stream. The DCurses contract therefore
first maps each malformed code unit deterministically to U+FFFD and only then asks the
runtime Unicode segmentation algorithm to determine text-element boundaries.

For example, a malformed high surrogate followed by a combining acute accent becomes:

```text
U+FFFD U+0301
```

before segmentation. The replacement character and combining mark can therefore form one
normalized text element, and the width provider sees that complete element rather than two
pre-normalization fragments.

## 4. Regression coverage

Automated tests cover:

- malformed surrogate replacement before segmentation;
- a malformed surrogate followed by a combining mark through a real
  `CursesWindow.Write(...)` call;
- the exact text elements observed by an injected width provider;
- ordinary combining sequences;
- regional-indicator flags;
- keycap sequences;
- text-vs-emoji presentation selectors;
- a representative family emoji ZWJ sequence;
- leader/continuation representation for two-column clusters.

The window-level recording-width-provider test is the T302 ownership proof: segmentation
is not merely correct in an internal helper; the public string-write path uses it.

## 5. Control characters

The existing `CursesWindow` safety policy remains:

- tab, carriage return, and line feed retain their explicit window-control meanings;
- other C0/C1 terminal controls are rejected;
- normalization does not create a route for raw terminal escape/control injection.

## 6. Deferred to T303

T302 does not freeze the final East Asian width tables or Ambiguous-width behavior.

T303 owns:

- versioned Unicode width data;
- narrow/wide East Asian Ambiguous policy;
- completion of cluster-aware width measurement beyond the first alpha's emoji-sensitive
  sequence families.

## 7. Gate

T302 is complete when:

1. `CursesWindow.Write(string)` uses the centralized normalized segmentation path;
2. no duplicate malformed-UTF-16 normalization remains in `CursesWindow`;
3. the configured width provider receives normalized complete text elements;
4. existing control-character rejection remains intact;
5. Windows, Linux, and macOS Staging tests pass;
6. the canonical package/fresh-consumer validation accepts `0.3.0-alpha.2`.
