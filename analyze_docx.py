#!/usr/bin/env python3
from docx import Document
from docx.oxml import parse_xml
import sys

def deep_inspect(docx_path):
    doc = Document(docx_path)
    print(f"=== 深度检查: {docx_path} ===")
    
    for i, para in enumerate(doc.paragraphs[:200]):
        print(f"\n--- 段落 {i+1} ---")
        print(f"样式: {para.style.name}")
        
        if para.runs:
            for j, run in enumerate(para.runs):
                print(f"  运行 {j+1}:")
                print(f"    文本: {repr(run.text)}")
                print(f"    粗体: {run.bold}")
                print(f"    斜体: {run.italic}")
                print(f"    字体: {run.font.name}")
        
        if not para.runs:
            print(f"  XML: {para._element.xml[:500]}")

if __name__ == '__main__':
    deep_inspect('/Users/dada/Documents/GitHub/Project-Ark/Docs/GDD/示巴星 (鸣钟者)/示巴星 V2.docx')
