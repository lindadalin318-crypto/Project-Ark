"""
克隆 BetterSpire2.pck 的完整结构，只修改：
1. mod_manifest.json 内容 → Astrolabe 信息
2. Godot 版本号 4.3.0 → 4.5.1（让游戏知道这是 4.5 mod）

不改变 PCK 格式版本（保持 v2），因为游戏本体（Godot 4.5）能读 v2。
"""
import struct
import hashlib
import os
import shutil

TEMPLATE_PCK = r"F:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\BetterSpire2-2-1-62-1773426336\BetterSpire2.pck"
OUTPUT_PCK   = r"F:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Astrolabe\Astrolabe.pck"
OUTPUT_BACKUP = r"D:\UnityProjects\Project-Ark\SideProject\StS2mod\src\Astrolabe\bin\Debug\net9.0\Astrolabe.pck"

# Astrolabe 的 manifest 内容
MOD_MANIFEST = b"""{
  "pck_name": "Astrolabe",
  "name": "Astrolabe - Decision Advisor",
  "author": "AstroTeam",
  "version": "0.1.0"
}"""

# Astrolabe 的 project.godot 内容（作为 project.binary 的替代，用文本格式）
# Godot PCK 里 project.binary 是 project.godot 的二进制序列化版本
# 我们直接复用 BetterSpire2 的 project.binary，只改 manifest
# project.binary 里的项目名 "BetterSpire2" 不影响 mod 加载

def parse_pck_v2(data):
    """解析 PCK v2 格式"""
    pos = 0
    assert data[pos:pos+4] == b'GDPC'
    pos += 4
    
    pck_ver = struct.unpack_from('<I', data, pos)[0]; pos += 4
    major   = struct.unpack_from('<I', data, pos)[0]; pos += 4
    minor   = struct.unpack_from('<I', data, pos)[0]; pos += 4
    patch   = struct.unpack_from('<I', data, pos)[0]; pos += 4
    
    # BetterSpire2 头部 = 96 bytes total 到 file_count
    # offset 16-95 = 80 bytes 额外字段/保留
    # 直接跳到 file_count
    FILE_COUNT_OFFSET = 96
    pos = FILE_COUNT_OFFSET
    file_count = struct.unpack_from('<I', data, pos)[0]; pos += 4
    
    files = []
    for i in range(file_count):
        path_len = struct.unpack_from('<I', data, pos)[0]; pos += 4
        path = data[pos:pos+path_len].rstrip(b'\x00').decode('utf-8'); pos += path_len
        offset = struct.unpack_from('<Q', data, pos)[0]; pos += 8
        size   = struct.unpack_from('<Q', data, pos)[0]; pos += 8
        md5    = data[pos:pos+16]; pos += 16
        flags  = struct.unpack_from('<I', data, pos)[0]; pos += 4
        
        files.append({
            'path': path,
            'offset': offset,
            'size': size,
            'md5': md5,
            'flags': flags,
            'path_len': path_len,
            'data': data[offset:offset+size],
        })
    
    entries_end = pos
    return {
        'pck_ver': pck_ver, 'major': major, 'minor': minor, 'patch': patch,
        'header_prefix': data[:FILE_COUNT_OFFSET],  # 保留原始头部前缀（含保留字段）
        'files': files,
        'entries_end': entries_end,
    }

def build_pck_v2(header_prefix, files, major, minor, patch):
    """重建 PCK v2，允许修改 Godot 版本号"""
    # 修改 header_prefix 中的版本号
    # magic(4) + pck_ver(4) 之后是 major(4) + minor(4) + patch(4)
    header = bytearray(header_prefix)
    struct.pack_into('<I', header, 8,  major)
    struct.pack_into('<I', header, 12, minor)
    struct.pack_into('<I', header, 16, patch)
    
    # 计算文件条目表大小
    entry_table = bytearray()
    for f in files:
        path_bytes = f['path'].encode('utf-8')
        aligned_len = f['path_len']
        padded_path = path_bytes + b'\x00' * (aligned_len - len(path_bytes))
        
        entry_table += struct.pack('<I', aligned_len)
        entry_table += padded_path
        entry_table += struct.pack('<Q', 0)   # offset placeholder
        entry_table += struct.pack('<Q', f['size'])
        entry_table += f['md5']
        entry_table += struct.pack('<I', f['flags'])
    
    # 数据段起始偏移
    data_start = len(header) + 4 + len(entry_table)  # header + file_count(4) + entries
    
    # 分配真实偏移并重写条目表
    final_entries = bytearray()
    current_offset = data_start
    for f in files:
        path_bytes = f['path'].encode('utf-8')
        aligned_len = f['path_len']
        padded_path = path_bytes + b'\x00' * (aligned_len - len(path_bytes))
        
        final_entries += struct.pack('<I', aligned_len)
        final_entries += padded_path
        final_entries += struct.pack('<Q', current_offset)
        final_entries += struct.pack('<Q', f['size'])
        final_entries += f['md5']
        final_entries += struct.pack('<I', f['flags'])
        
        current_offset += f['size']
    
    # 组装最终 PCK
    result = bytes(header)
    result += struct.pack('<I', len(files))
    result += bytes(final_entries)
    for f in files:
        result += f['data']
    
    return result

def main():
    print("=== 读取 BetterSpire2.pck ===")
    with open(TEMPLATE_PCK, 'rb') as fp:
        template_data = fp.read()
    
    pck = parse_pck_v2(template_data)
    print(f"原始版本: Godot {pck['major']}.{pck['minor']}.{pck['patch']}, PCK v{pck['pck_ver']}")
    print(f"文件数: {len(pck['files'])}")
    for f in pck['files']:
        print(f"  {f['path']!r:60s} size={f['size']:5d}")
    
    print("\n=== 替换 mod_manifest.json ===")
    for f in pck['files']:
        if f['path'] == 'res://mod_manifest.json':
            f['data'] = MOD_MANIFEST
            f['size'] = len(MOD_MANIFEST)
            f['md5']  = hashlib.md5(MOD_MANIFEST).digest()
            print(f"  新内容: {f['data'].decode()}")
    
    # 目标版本：4.5.1（与游戏一致）
    TARGET_MAJOR, TARGET_MINOR, TARGET_PATCH = 4, 5, 1
    
    print(f"\n=== 构建 Astrolabe.pck (Godot {TARGET_MAJOR}.{TARGET_MINOR}.{TARGET_PATCH}, PCK v{pck['pck_ver']}) ===")
    new_pck = build_pck_v2(pck['header_prefix'], pck['files'], TARGET_MAJOR, TARGET_MINOR, TARGET_PATCH)
    print(f"PCK 大小: {len(new_pck)} bytes")
    
    # 验证版本号写入
    assert struct.unpack_from('<I', new_pck, 8)[0]  == TARGET_MAJOR
    assert struct.unpack_from('<I', new_pck, 12)[0] == TARGET_MINOR
    assert struct.unpack_from('<I', new_pck, 16)[0] == TARGET_PATCH
    print("版本号验证: OK")
    
    # 写出
    for path in [OUTPUT_PCK, OUTPUT_BACKUP]:
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, 'wb') as fp:
            fp.write(new_pck)
        print(f"写入: {path}")
    
    print("\n=== 验证 manifest ===")
    verify = parse_pck_v2(new_pck)
    for f in verify['files']:
        if 'manifest' in f['path']:
            content = f['data']
            print(f"  {f['path']}: {content.decode()}")

if __name__ == '__main__':
    main()
