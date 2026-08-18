import sys
from pathlib import Path

pdf_path = Path(r"e:\Work\Cursor\Gravedigger2026\Gravedigger2026\Gravedigger2026\Assets\AllIn1SpriteShader\Documentation.pdf")
out_path = Path(r"e:\Work\Cursor\Gravedigger2026\Gravedigger2026\.scratch\tools\allin1_docs.txt")

def extract():
    errors = []
    try:
        import pypdf
        reader = pypdf.PdfReader(str(pdf_path))
        texts = []
        for i, page in enumerate(reader.pages):
            texts.append(f"\n\n===== PAGE {i+1} =====\n")
            texts.append(page.extract_text() or "")
        out_path.write_text("".join(texts), encoding="utf-8")
        print(f"pypdf pages={len(reader.pages)} chars={out_path.stat().st_size}")
        return
    except Exception as e:
        errors.append(f"pypdf: {e}")

    try:
        import PyPDF2
        reader = PyPDF2.PdfReader(str(pdf_path))
        texts = []
        for i, page in enumerate(reader.pages):
            texts.append(f"\n\n===== PAGE {i+1} =====\n")
            texts.append(page.extract_text() or "")
        out_path.write_text("".join(texts), encoding="utf-8")
        print(f"PyPDF2 pages={len(reader.pages)} chars={out_path.stat().st_size}")
        return
    except Exception as e:
        errors.append(f"PyPDF2: {e}")

    try:
        import fitz
        doc = fitz.open(str(pdf_path))
        texts = []
        for i, page in enumerate(doc):
            texts.append(f"\n\n===== PAGE {i+1} =====\n")
            texts.append(page.get_text() or "")
        out_path.write_text("".join(texts), encoding="utf-8")
        print(f"fitz pages={len(doc)} chars={out_path.stat().st_size}")
        return
    except Exception as e:
        errors.append(f"fitz: {e}")

    try:
        from pdfminer.high_level import extract_text
        text = extract_text(str(pdf_path))
        out_path.write_text(text, encoding="utf-8")
        print(f"pdfminer chars={out_path.stat().st_size}")
        return
    except Exception as e:
        errors.append(f"pdfminer: {e}")

    print("FAILED")
    for err in errors:
        print(err)
    sys.exit(1)

if __name__ == "__main__":
    print("pdf exists", pdf_path.exists(), pdf_path.stat().st_size if pdf_path.exists() else 0)
    extract()
