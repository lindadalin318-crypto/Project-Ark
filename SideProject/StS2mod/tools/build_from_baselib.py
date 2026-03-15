"""
以 BaseLib.pck 为基础生成 Astrolabe.pck
BaseLib.pck 是由 Godot 4.5.1 正式导出的，格式完全正确。
我们只需要替换其中的 mod_manifest.json 内容。

BaseLib.pck 结构:
- offset 0-111:   PCK 头部（magic + 版本 + pack_flags=2 + file_base=112 + reserved + file_count=0）
- offset 112+:    数据段（直接包含所有文件内容，manifest 在最前面）

由于 pack_flags=2（加密索引），文件条目表被加密，直接嵌入。
游戏通过 Godot 内部机制访问 res://mod_manifest.json。
"""
import struct, os, shutil

BASELIB_PCK = r'D:\UnityProjects\Project-Ark\SideProject\StS2mod\tools\BaseLib.pck'
OUTPUT_PCK  = r'F:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Astrolabe\Astrolabe.pck'
OUTPUT_BACKUP = r'D:\UnityProjects\Project-Ark\SideProject\StS2mod\src\Astrolabe\bin\Debug\net9.0\Astrolabe.pck'

# Astrolabe 的 manifest（与 Astrolabe.json 保持一致）
OLD_MANIFEST_MARKER = b'"pck_name": "BaseLib"'
ASTROLABE_MANIFEST = b"""{
  "pck_name": "Astrolabe",
  "name": "Astrolabe - Decision Advisor",
  "author": "AstroTeam",
  "version": "0.1.0"
}"""

def main():
    data = bytearray(open(BASELIB_PCK, 'rb').read())
    print(f"BaseLib.pck size: {len(data)}")
    
    # 验证格式
    assert data[:4] == b'GDPC', "Not a PCK"
    pck_ver = struct.unpack_from('<I', data, 4)[0]
    major   = struct.unpack_from('<I', data, 8)[0]
    minor   = struct.unpack_from('<I', data, 12)[0]
    patch   = struct.unpack_from('<I', data, 16)[0]
    pack_flags = struct.unpack_from('<I', data, 20)[0]
    file_base  = struct.unpack_from('<Q', data, 24)[0]
    print(f"PCK v{pck_ver}, Godot {major}.{minor}.{patch}, pack_flags={pack_flags}, file_base={file_base}")
    
    # 找到 manifest 的起始位置（在数据段中）
    # manifest 在 file_base 之后，根据之前的分析在 offset 112 + 2 = 114（有两个前导字节 \x00\x00）
    # 实际内容从 {  开始
    brace_pos = data.index(b'{\r\n  "pck_name"', file_base)
    end_pos   = data.index(b'\n}', brace_pos) + 2  # 包含结束的 \n}
    old_manifest = data[brace_pos:end_pos]
    print(f"\n旧 manifest at {brace_pos}-{end_pos}:")
    print(old_manifest.decode('utf-8', 'replace'))
    
    # 替换：新旧大小不同时需要重新计算偏移
    # 为了简单起见，用空格填充到相同大小，或者截断+重建
    old_size = end_pos - brace_pos
    new_size = len(ASTROLABE_MANIFEST)
    size_diff = new_size - old_size
    print(f"\n旧大小={old_size}, 新大小={new_size}, 差值={size_diff}")
    
    if size_diff == 0:
        # 原地替换
        data[brace_pos:end_pos] = ASTROLABE_MANIFEST
    else:
        # 需要重建：把 manifest 前后的数据拼接
        new_data = (data[:brace_pos] 
                    + ASTROLABE_MANIFEST 
                    + data[end_pos:])
        data = bytearray(new_data)
        print(f"新 PCK 大小: {len(data)}")
    
    # 写出
    for path in [OUTPUT_PCK, OUTPUT_BACKUP]:
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, 'wb') as f:
            f.write(data)
        print(f"写入: {path}")
    
    # 验证
    out = open(OUTPUT_PCK, 'rb').read()
    idx = out.find(b'pck_name')
    if idx >= 0:
        print(f"\n验证 manifest: {out[idx-5:idx+60]!r}")
    else:
        print("ERROR: manifest not found in output!")

if __name__ == '__main__':
    main()
