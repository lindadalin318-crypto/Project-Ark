#!/usr/bin/env python3
import zipfile
import sys
import os

def extract_and_inspect(docx_path):
    with zipfile.ZipFile(docx_path, 'r') as zf:
        print(f"=== 解压文件列表:")
        for name in zf.namelist():
            print(f"  {name}")
        
        print("\n=== 检查 document.xml ===")
        if 'word/document.xml' in zf.namelist():
            with zf.open('word/document.xml') as f:
                content = f.read().decode('utf-8')
                print(content[:5000])

if __name__ == '__main__':
    extract_and_inspect('/Users/dada/Documents/GitHub/Project-Ark/Docs/GDD/示巴星 (鸣钟者)/示巴星 V2.docx')
