"""
从 BetterSpire2.pck 克隆一个 Astrolabe.pck
只替换 mod_manifest.json 的内容，其余文件（icon, project.binary等）保持不变
这确保 PCK 版本为 Godot 4.3 格式，与游戏兼容
"""
import struct
import hashlib
import os
import shutil

TEMPLATE_PCK = r"F:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\BetterSpire2-2-1-62-1773426336\BetterSpire2.pck"
OUTPUT_PCK = r"F:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Astrolabe\Astrolabe.pck"
OUTPUT_BACKUP = r"D:\UnityProjects\Project-Ark\SideProject\StS2mod\src\Astrolabe\bin\Debug\net9.0\Astrolabe.pck"

MOD_MANIFEST = b"""{
  "pck_name": "Astrolabe",
  "name": "Astrolabe - Decision Advisor",
  "author": "AstroTeam",
  "version": "0.1.0"
}"""

def parse_pck(data):
    """解析 PCK 二进制，返回结构体"""
    pos = 0
    assert data[pos:pos+4] == b'GDPC', "Not a PCK file"
    pos += 4
    
    pck_ver = struct.unpack_from('<I', data, pos)[0]; pos += 4
    major   = struct.unpack_from('<I', data, pos)[0]; pos += 4
    minor   = struct.unpack_from('<I', data, pos)[0]; pos += 4
    patch   = struct.unpack_from('<I', data, pos)[0]; pos += 4
    
    # 找第一个文件路径来定位 file_count 的偏移
    # 已知：BetterSpire2 file_count 在 offset 96, 头部 20+76=96 bytes
    # 根据 Godot 源码 PCK v2: header = magic(4)+pck_ver(4)+major(4)+minor(4)+patch(4)+reserved(16*4=64)+file_count(4)
    # = 4+4+4+4+4+64+4 = 88，但实测是96...
    # 对比 v2 vs v1: v2 在 file_count 前多了一个 uint64 (数据段偏移 + 对齐填充)
    # offset 20-23: extra_field_1 = 0
    # offset 24-27: extra_field_2 = 608 (数据段偏移？)
    # offset 28-91: reserved (64 bytes, all zeros)
    # offset 92-95: more reserved?
    # offset 96: file_count
    
    # 直接用偏移96
    file_count_pos = 96
    
    # 保存头部的完整原始字节（offset 0 到 file_count 之前）
    header_raw = data[:file_count_pos]
    
    pos = file_count_pos
    file_count = struct.unpack_from('<I', data, pos)[0]; pos += 4
    
    files = []
    for i in range(file_count):
        path_len = struct.unpack_from('<I', data, pos)[0]; pos += 4
        path = data[pos:pos+path_len].rstrip(b'\x00').decode('utf-8')
        pos += path_len
        
        offset = struct.unpack_from('<Q', data, pos)[0]; pos += 8
        size   = struct.unpack_from('<Q', data, pos)[0]; pos += 8
        md5    = data[pos:pos+16]; pos += 16
        flags  = struct.unpack_from('<I', data, pos)[0]; pos += 4
        
        file_data = data[offset:offset+size]
        files.append({
            'path': path,
            'offset': offset,
            'size': size,
            'md5': md5,
            'flags': flags,
            'path_len': path_len,
            'data': file_data,
        })
    
    entries_end = pos  # 文件条目表结束，数据段开始
    
    return {
        'header_raw': header_raw,
        'pck_ver': pck_ver,
        'major': major, 'minor': minor, 'patch': patch,
        'file_count_pos': file_count_pos,
        'files': files,
        'entries_end': entries_end,
        'raw': data,
    }

def build_pck(header_raw, files):
    """
    重建 PCK。
    - 保持 header_raw 不变（包含 PCK版本、Godot版本、保留字段）
    - 重新计算文件条目表和数据段偏移
    """
    # 计算文件条目表大小
    entry_table_size = 0
    for f in files:
        path_bytes = f['path'].encode('utf-8')
        # path_len必须与原始path_len一致（对齐规则相同）
        aligned_len = f['path_len']
        # 4(path_len) + aligned_len + 8(offset) + 8(size) + 16(md5) + 4(flags)
        entry_table_size += 4 + aligned_len + 8 + 8 + 16 + 4
    
    # 数据段起始 = header(96) + file_count(4) + entry_table
    data_start = len(header_raw) + 4 + entry_table_size
    
    # 分配每个文件的新偏移
    current_offset = data_start
    for f in files:
        f['new_offset'] = current_offset
        current_offset += f['size']
    
    # 组装 PCK
    result = bytearray(header_raw)
    result += struct.pack('<I', len(files))  # file_count
    
    for f in files:
        path_bytes = f['path'].encode('utf-8')
        aligned_len = f['path_len']
        padded_path = path_bytes + b'\x00' * (aligned_len - len(path_bytes))
        
        result += struct.pack('<I', aligned_len)
        result += padded_path
        result += struct.pack('<Q', f['new_offset'])
        result += struct.pack('<Q', f['size'])
        result += f['md5']
        result += struct.pack('<I', f['flags'])
    
    # 数据段
    for f in files:
        result += f['data']
    
    return bytes(result)

def main():
    print("=== 读取 BetterSpire2.pck ===")
    with open(TEMPLATE_PCK, 'rb') as fp:
        template_data = fp.read()
    
    pck = parse_pck(template_data)
    print(f"Godot: {pck['major']}.{pck['minor']}.{pck['patch']}, PCK v{pck['pck_ver']}")
    print(f"Files: {len(pck['files'])}")
    for f in pck['files']:
        print(f"  {f['path']!r:60s} offset={f['offset']:5d} size={f['size']:5d}")
    
    print("\n=== 替换 mod_manifest.json ===")
    for f in pck['files']:
        if f['path'] == 'res://mod_manifest.json':
            print(f"  旧内容: {f['data'][:80]!r}")
            f['data'] = MOD_MANIFEST
            f['size'] = len(MOD_MANIFEST)
            f['md5'] = hashlib.md5(MOD_MANIFEST).digest()
            print(f"  新内容: {f['data']!r}")
            print(f"  新大小: {f['size']} bytes")
    
    print("\n=== 构建 Astrolabe.pck ===")
    new_pck = build_pck(pck['header_raw'], pck['files'])
    print(f"PCK 大小: {len(new_pck)} bytes (原始: {len(template_data)} bytes)")
    
    # 写出
    os.makedirs(os.path.dirname(OUTPUT_PCK), exist_ok=True)
    with open(OUTPUT_PCK, 'wb') as fp:
        fp.write(new_pck)
    print(f"写入: {OUTPUT_PCK}")
    
    os.makedirs(os.path.dirname(OUTPUT_BACKUP), exist_ok=True)
    shutil.copy2(OUTPUT_PCK, OUTPUT_BACKUP)
    print(f"备份: {OUTPUT_BACKUP}")
    
    print("\n=== 验证 ===")
    with open(OUTPUT_PCK, 'rb') as fp:
        verify_data = fp.read()
    verify = parse_pck(verify_data)
    for f in verify['files']:
        marker = " <<<" if f['path'] == 'res://mod_manifest.json' else ""
        print(f"  {f['path']!r:60s} offset={f['new_offset'] if 'new_offset' in f else f['offset']:5d} size={f['size']:5d}{marker}")
        if f['path'] == 'res://mod_manifest.json':
            print(f"    内容: {f['data']!r}")

if __name__ == '__main__':
    main()
