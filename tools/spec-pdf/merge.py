import sys

import pymupdf

cover_path, body_path, out_path = sys.argv[1:4]

# Heading levels are identified by their rendered point size, which the stylesheet
# fixes at 15pt for "Contents", 14pt for h2, and 11pt for h3.
LEVELS = {15.0: 1, 14.0: 1, 11.0: 2}


def headings(path, page_offset):
    found = []
    doc = pymupdf.open(path)
    for index in range(doc.page_count):
        for block in doc[index].get_text("dict")["blocks"]:
            for line in block.get("lines", []):
                text = "".join(s["text"] for s in line.get("spans", [])).strip()
                size = round(line["spans"][0]["size"], 1) if line.get("spans") else 0
                if text and size in LEVELS:
                    found.append([LEVELS[size], text, index + 1 + page_offset])
    doc.close()
    return found


doc = pymupdf.open()
doc.insert_file(cover_path)
doc.insert_file(body_path)

# insert_file drops GoTo annotations, so re-attach the body's internal links,
# shifted by the single unnumbered cover page.
body = pymupdf.open(body_path)
carried = 0
for index in range(body.page_count):
    for link in body[index].get_links():
        # Chrome emits in-document anchors as named destinations; normalise both
        # those and plain GoTo links to resolved page references.
        if link["kind"] in (pymupdf.LINK_GOTO, pymupdf.LINK_NAMED) and link.get("page", -1) >= 0:
            doc[index + 1].insert_link(
                {
                    "kind": pymupdf.LINK_GOTO,
                    "from": link["from"],
                    "page": link["page"] + 1,
                    "to": link.get("to", pymupdf.Point(0, 0)),
                }
            )
            carried += 1
body.close()

doc.set_metadata(
    {
        "title": "Service Management Platform - Software Specification",
        "author": "Service Management Platform",
        "subject": "Functional and technical specification (v1.0, through Phase 4)",
        "keywords": "specification, service management, ASP.NET Core, Angular, Flutter, PostgreSQL",
        "creator": "Service Management Platform docs pipeline",
    }
)

toc = headings(body_path, page_offset=1)
# A level-2 entry cannot open the outline, so promote any that appear first.
if toc and toc[0][0] != 1:
    toc[0][0] = 1
doc.set_toc(toc)

doc.save(out_path, garbage=4, deflate=True, clean=True)
print(f"wrote {out_path}: {doc.page_count} pages, {len(toc)} bookmarks, {carried} links")
for level, title, page in toc:
    print(f"  p{page:>3}  {'  ' * (level - 1)}{title}")
