# Specification PDF generator

Renders [`docs/SPECIFICATION.md`](../../docs/SPECIFICATION.md) to
[`docs/SPECIFICATION.pdf`](../../docs/SPECIFICATION.pdf).

The specification itself is the Markdown file — it is the source of truth and the
thing to edit. The PDF is a derived artifact, regenerated whenever the Markdown
changes.

## Prerequisites

- Node.js 20+
- Python 3 with `pymupdf` (`pip install pymupdf`)
- A Chrome or Chromium binary

`print.js` looks for Chrome at `/usr/local/bin/google-chrome`. Override with
`CHROME_PATH` if yours lives elsewhere.

## Usage

```bash
cd tools/spec-pdf
npm install
npm run build
```

## How it works

Three stages, because Chrome applies one set of page margins per print job and the
cover needs none:

1. **`build.js`** — parses the Markdown, lifts the title and metadata block onto a
   cover page, derives a two-column table of contents from the `##` headings, and
   writes `out/cover.html` and `out/body.html` with the print stylesheet inlined.
2. **`print.js`** — prints one of those files via headless Chrome. `cover` mode uses
   zero margins for a full-bleed page; `body` mode adds margins and a numbered footer.
3. **`merge.py`** — concatenates the two PDFs, sets document metadata, rebuilds the
   outline by heading point size, and re-attaches the internal links that the
   concatenation drops.

The cover page is intentionally unnumbered and excluded from the page count, so the
footer on the first body page reads "Page 1".
