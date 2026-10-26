const fs = require('fs');
const MarkdownIt = require('markdown-it');
const anchor = require('markdown-it-anchor');

const srcPath = process.argv[2];
const outPath = process.argv[3];

fs.mkdirSync(outPath, { recursive: true });

// GitHub-compatible slugs so in-document links written as #15-out-of-scope resolve.
const slugify = (s) =>
  s
    .trim()
    .toLowerCase()
    .replace(/[^\w\s-]/g, '')
    .replace(/\s+/g, '-');

const md = new MarkdownIt({ html: true, linkify: false, typographer: true }).use(anchor, {
  slugify,
  level: [2, 3],
});

const raw = fs.readFileSync(srcPath, 'utf8');

// The document's own H1 and the metadata block become the cover page instead of body flow.
const lines = raw.split('\n');
const hrIndex = lines.findIndex((l, i) => i > 0 && l.trim() === '---');
const frontLines = lines.slice(0, hrIndex);
const bodyLines = lines.slice(hrIndex + 1);

const title = frontLines.find((l) => l.startsWith('# ')).slice(2).trim();
const meta = frontLines
  .filter((l) => l.startsWith('**'))
  .map((l) => {
    const m = l.match(/^\*\*(.+?):\*\*\s*(.+?)\s*$/);
    return m ? { label: m[1], value: m[2] } : null;
  })
  .filter(Boolean);

// Requirement ids must not break across lines at their hyphens.
const bodyHtml = md
  .render(bodyLines.join('\n'))
  .replace(/\b((?:FR-[A-Z]+|NFR)-\d+(?:…\d+)?)\b/g, '<span class="nobr">$1</span>');

// Build a table of contents from the H2 headings.
const toc = [...bodyHtml.matchAll(/<h2[^>]*>(.*?)<\/h2>/g)].map((m) =>
  m[1].replace(/<[^>]+>/g, ''),
);

const metaRows = meta
  .map((m) => `<tr><th>${m.label}</th><td>${m.value}</td></tr>`)
  .join('');

const tocHtml = toc
  .map((t) => {
    const parts = t.match(/^(\d+|Appendix [A-Z])[.\s—]*\s*(.*)$/);
    const num = parts ? parts[1] : '';
    const text = parts ? parts[2].replace(/^[—\s]+/, '') : t;
    return `<li><span class="toc-num">${num}</span><span class="toc-text">${text}</span></li>`;
  })
  .join('');

const styles = `<style>
  @page { size: A4; margin: 20mm 18mm 20mm 18mm; }
  @page :first { margin: 0; }

  :root {
    --ink: #16202c;
    --muted: #5b6b7c;
    --rule: #d8e0e8;
    --accent: #1f5f8b;
    --accent-soft: #eef4f9;
    --code-bg: #f5f7f9;
  }

  * { box-sizing: border-box; }

  html { -webkit-print-color-adjust: exact; print-color-adjust: exact; }

  body {
    margin: 0;
    font-family: "Source Sans Pro", "Segoe UI", "Helvetica Neue", Arial, sans-serif;
    font-size: 9.6pt;
    line-height: 1.55;
    color: var(--ink);
  }

  /* ---------- cover ---------- */
  .cover {
    height: 297mm;
    padding: 46mm 22mm 20mm 22mm;
    page-break-after: always;
    display: flex;
    flex-direction: column;
    background: linear-gradient(170deg, #11314a 0%, #1f5f8b 58%, #2b7fb0 100%);
    color: #fff;
  }
  .cover .eyebrow {
    font-size: 9pt;
    letter-spacing: 0.22em;
    text-transform: uppercase;
    color: #a9cde6;
    margin-bottom: 14mm;
  }
  .cover h1 {
    font-size: 30pt;
    line-height: 1.16;
    font-weight: 600;
    margin: 0 0 7mm 0;
    letter-spacing: -0.015em;
    max-width: 135mm;
  }
  .cover .rule { width: 26mm; height: 3px; background: #7fc3e8; margin-bottom: 7mm; }
  .cover .tagline {
    font-size: 11.5pt;
    color: #d5e9f5;
    font-weight: 300;
    max-width: 118mm;
    line-height: 1.5;
  }
  .cover .spacer { flex: 1; }
  .cover table { border-collapse: collapse; font-size: 9.5pt; }
  .cover table th,
  .cover table td { border: 0; background: none; vertical-align: top; }
  .cover table th {
    text-align: left;
    font-weight: 400;
    color: #a9cde6;
    padding: 2.2mm 12mm 2.2mm 0;
    white-space: nowrap;
  }
  .cover table td { padding: 2.2mm 0; color: #fff; }

  /* ---------- contents ---------- */
  .contents { page-break-after: always; padding-top: 2mm; }
  .contents h2 {
    font-size: 15pt; color: var(--accent); margin: 0 0 6mm 0;
    padding: 0; border: 0;
  }
  .contents ol { list-style: none; margin: 0; padding: 0; column-count: 2; column-gap: 12mm; }
  .contents li {
    display: flex; gap: 4mm; padding: 1.5mm 0;
    border-bottom: 1px solid #eef1f4;
    break-inside: avoid;
  }
  .toc-num { color: var(--accent); font-weight: 600; min-width: 12mm; font-variant-numeric: tabular-nums; }
  .toc-text { color: var(--ink); }

  /* ---------- body ---------- */
  h2 {
    font-size: 14pt;
    font-weight: 600;
    color: var(--accent);
    margin: 10mm 0 3.5mm 0;
    padding-bottom: 2mm;
    border-bottom: 2px solid var(--rule);
    page-break-after: avoid;
    letter-spacing: -0.01em;
  }
  h2:first-child { margin-top: 0; }
  h3 {
    font-size: 11pt; font-weight: 600; color: var(--ink);
    margin: 6.5mm 0 2.5mm 0; page-break-after: avoid;
  }
  h4 {
    font-size: 9.8pt; font-weight: 600; color: var(--muted);
    text-transform: uppercase; letter-spacing: 0.07em;
    margin: 5mm 0 2mm 0; page-break-after: avoid;
  }

  p { margin: 0 0 3mm 0; orphans: 2; widows: 2; }

  ul, ol { margin: 0 0 3.5mm 0; padding-left: 5.5mm; }
  li { margin-bottom: 1.3mm; }
  li > p { margin-bottom: 1.5mm; }

  strong { font-weight: 600; }

  a { color: var(--accent); text-decoration: none; border-bottom: 1px solid #b7d2e3; }
  h2 a, h3 a { color: inherit; border: 0; }

  blockquote {
    margin: 0 0 4mm 0;
    padding: 2.5mm 0 2.5mm 5mm;
    border-left: 3px solid var(--accent);
    background: var(--accent-soft);
    color: var(--ink);
  }
  blockquote p:last-child { margin-bottom: 0; }

  .nobr { white-space: nowrap; }

  code {
    font-family: "DejaVu Sans Mono", "Consolas", monospace;
    font-size: 8.4pt;
    background: var(--code-bg);
    padding: 0.3mm 0.5mm;
    border-radius: 2px;
    color: #0f3a57;
  }
  pre {
    background: var(--code-bg);
    border: 1px solid var(--rule);
    border-left: 3px solid var(--accent);
    border-radius: 3px;
    padding: 3mm 4mm;
    margin: 0 0 4mm 0;
    overflow: hidden;
    page-break-inside: avoid;
  }
  pre code {
    background: none; padding: 0; font-size: 8pt; line-height: 1.45;
    color: var(--ink); white-space: pre-wrap; word-break: break-word;
  }

  main table {
    width: 100%;
    border-collapse: collapse;
    margin: 0 0 4.5mm 0;
    font-size: 8.6pt;
    page-break-inside: auto;
  }
  main thead { display: table-header-group; }
  main tr { page-break-inside: avoid; }
  main th {
    text-align: left;
    background: var(--accent);
    color: #fff;
    font-weight: 600;
    padding: 1.9mm 2.4mm;
    border: 1px solid var(--accent);
    line-height: 1.35;
  }
  main td {
    padding: 1.7mm 2.4mm;
    border: 1px solid var(--rule);
    vertical-align: top;
    line-height: 1.45;
  }
  main tbody tr:nth-child(even) td { background: #f8fafb; }
  main td code, main th code { font-size: 7.9pt; }

  hr { border: 0; border-top: 1px solid var(--rule); margin: 7mm 0; }

  h2 + table, h3 + table, h2 + pre, h3 + pre { page-break-before: avoid; }
</style>`;

const page = (bodyContent) => `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>${title}</title>
${styles}
</head>
<body>${bodyContent}</body>
</html>`;

const coverHtml = page(`
  <section class="cover">
    <div class="eyebrow">Technical Specification</div>
    <h1>${title.replace(/ — /, '<br>')}</h1>
    <div class="rule"></div>
    <div class="tagline">Functional and technical specification for a category-agnostic
    service management and fulfillment platform.</div>
    <div class="spacer"></div>
    <table>${metaRows}</table>
  </section>`);

const docHtml = page(`
  <section class="contents">
    <h2>Contents</h2>
    <ol>${tocHtml}</ol>
  </section>
  <main>${bodyHtml}</main>`);

fs.writeFileSync(`${outPath}/cover.html`, coverHtml);
fs.writeFileSync(`${outPath}/body.html`, docHtml);
console.log(`wrote cover.html + body.html to ${outPath}; ${toc.length} sections`);
