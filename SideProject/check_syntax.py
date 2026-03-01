import re, subprocess, tempfile, os

with open('d:/Unity Projects/Project-Ark/SideProject/spinning-top.html', encoding='utf-8') as f:
    html = f.read()

scripts = re.findall(r'<script[^>]*>(.*?)</script>', html, re.DOTALL)
js = '\n'.join(scripts)

tmp_path = 'd:/Unity Projects/Project-Ark/SideProject/_tmp_check.js'
with open(tmp_path, 'w', encoding='utf-8') as f:
    f.write(js)

result = subprocess.run(['node', '--check', tmp_path], capture_output=True, text=True)
out = (result.stdout + result.stderr).strip()
if out:
    print("SYNTAX ERROR:")
    print(out[:1000])
else:
    print("SYNTAX OK - no errors")

os.unlink(tmp_path)
