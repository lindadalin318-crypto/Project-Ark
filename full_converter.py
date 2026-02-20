#!/usr/bin/env python3
import zipfile
import re
import sys
import os

def extract_text_from_xml(xml_content):
    text_parts = []
    
    pattern = r'<w:t[^>]*>([^<]*)</w:t>'
    matches = re.findall(pattern, xml_content)
    
    return ''.join(matches)

def docx_to_markdown_via_xml(docx_path, md_path):
    with zipfile.ZipFile(docx_path, 'r') as zf:
        with zf.open('word/document.xml') as f:
            xml_content = f.read().decode('utf-8')
    
    paragraphs = []
    
    p_pattern = r'<w:p[^>]*>(.*?)</w:p>'
    p_matches = re.findall(p_pattern, xml_content, re.DOTALL)
    
    for p_match in p_matches:
        style = ''
        style_match = re.search(r'<w:pStyle[^>]*w:val="([^"]+)"', p_match)
        if style_match:
            style = style_match.group(1)
        
        t_pattern = r'<w:t[^>]*>([^<]*)</w:t>'
        t_matches = re.findall(t_pattern, p_match)
        text = ''.join(t_matches).strip()
        
        paragraphs.append({
            'style': style,
            'text': text
        })
    
    md_lines = []
    for para in paragraphs:
        text = para['text']
        style = para['style']
        
        if not text:
            md_lines.append('')
            continue
        
        if style == 'Heading1':
            md_lines.append(f'# {text}')
        elif style == 'Heading2':
            md_lines.append(f'## {text}')
        elif style == 'Heading3':
            md_lines.append(f'### {text}')
        elif style == 'Heading4':
            md_lines.append(f'#### {text}')
        elif style == 'Heading5':
            md_lines.append(f'##### {text}')
        elif style == 'Heading6':
            md_lines.append(f'###### {text}')
        else:
            md_lines.append(text)
    
    md_content = '\n'.join(md_lines)
    
    with open(md_path, 'w', encoding='utf-8') as f:
        f.write(md_content)
    
    print(f'转换完成: {md_path}')

if __name__ == '__main__':
    files_to_convert = [
        ('/Users/dada/Documents/GitHub/Project-Ark/Docs/GDD/示巴星 (鸣钟者)/示巴星 V1.docx',
         '/Users/dada/Documents/GitHub/Project-Ark/Docs/GDD/示巴星 (鸣钟者)/示巴星 V1.md'),
        ('/Users/dada/Documents/GitHub/Project-Ark/Docs/GDD/示巴星 (鸣钟者)/示巴星 V2.docx',
         '/Users/dada/Documents/GitHub/Project-Ark/Docs/GDD/示巴星 (鸣钟者)/示巴星 V2.md'),
    ]
    
    for docx_path, md_path in files_to_convert:
        if os.path.exists(docx_path):
            docx_to_markdown_via_xml(docx_path, md_path)
        else:
            print(f'文件不存在: {docx_path}')
