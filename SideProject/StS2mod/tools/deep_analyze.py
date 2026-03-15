"""
深度分析 BetterSpire2.pck 的 mod_manifest.json 内容
之前发现它的数据不是明文JSON，可能被加密或压缩
需要了解真实内容才能正确构建我们的 PCK
"""
import struct, zlib

def parse_v2(data):
    FILE_COUNT_OFFSET = 96
    file_count = struct.unpack_from('<I', data, FILE_COUNT_OFFSET)[0]
    pos = FILE_COUNT_OFFSET + 4
    
    files = []
    for i in range(file_count):
        path_len = struct.unpack_from('<I', data, pos)[0]; pos += 4
        path = data[pos:pos+path_len].rstrip(b'\x00').decode('utf-8'); pos += path_len
        offset = struct.unpack_from('<Q', data, pos)[0]; pos += 8
        size   = struct.unpack_from('<Q', data, pos)[0]; pos += 8
        md5    = data[pos:pos+16]; pos += 16
        flags  = struct.unpack_from('<I', data, pos)[0]; pos += 4
        files.append({'path':path,'offset':offset,'size':size,'flags':flags,'md5':md5})
    return files

data = open(r"F:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\BetterSpire2-2-1-62-1773426336\BetterSpire2.pck",'rb').read()
files = parse_v2(data)

for f in files:
    raw = data[f['offset']:f['offset']+f['size']]
    print(f"\n{'='*60}")
    print(f"Path: {f['path']}")
    print(f"Offset: {f['offset']}, Size: {f['size']}, Flags: {f['flags']} (0x{f['flags']:08X})")
    print(f"MD5: {f['md5'].hex()}")
    
    # 尝试解析
    print(f"Raw hex (first 32): {raw[:32].hex()}")
    
    # 检查是否是 zstd (magic: 0xFD2FB528)
    if len(raw) >= 4:
        magic4 = struct.unpack_from('<I', raw, 0)[0]
        print(f"Magic: 0x{magic4:08X}")
        if magic4 == 0xFD2FB528:
            print("  -> zstd compressed!")
            try:
                import zstandard as zstd
                dctx = zstd.ZstdDecompressor()
                dec = dctx.decompress(raw)
                print(f"  Decompressed: {dec[:100]}")
            except ImportError:
                print("  (zstandard not installed)")
        elif magic4 == 0x04034B50:
            print("  -> ZIP file!")
        elif raw[:2] == b'\x78\x9c' or raw[:2] == b'\x78\x01' or raw[:2] == b'\x78\xda':
            print("  -> zlib compressed!")
            try:
                dec = zlib.decompress(raw)
                print(f"  Decompressed: {dec[:100]}")
            except Exception as e:
                print(f"  zlib failed: {e}")
        elif raw[0:1] == b'{' or raw[0:1] == b'[':
            print(f"  -> Plain text JSON: {raw[:100].decode('utf-8','replace')}")
        else:
            # 检查是否是 Godot 二进制格式（以 ECFG 开始）
            if raw[:4] == b'ECFG':
                print("  -> Godot binary config (ECFG)")
                # 打印更多
                print(f"  Content: {raw[:200]}")
            elif raw[:3] == b'GST' or raw[:4] == b'GST2':
                print(f"  -> Godot Streaming Texture")
            else:
                print(f"  -> Unknown format")
                # 尝试作为文本读取
                try:
                    text = raw.decode('utf-8')
                    print(f"  As UTF-8: {text[:100]}")
                except:
                    pass

# 对比：分析我们的 Astrolabe.pck（Python构造的v2版本，之前弃用）
# 现在看的是BaseLib克隆版
print("\n\n" + "="*60)
print("=== 当前部署的 Astrolabe.pck ===")
adata = open(r"F:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Astrolabe\Astrolabe.pck",'rb').read()
pck_ver = struct.unpack_from('<I', adata, 4)[0]
major = struct.unpack_from('<I', adata, 8)[0]
minor = struct.unpack_from('<I', adata, 12)[0]
pack_flags = struct.unpack_from('<I', adata, 20)[0]
file_base = struct.unpack_from('<Q', adata, 24)[0]
print(f"PCK v{pck_ver}, Godot {major}.{minor}, pack_flags={pack_flags}, file_base={file_base}")

# 文件计数（v3格式，file_count在offset 96）
file_count_v3 = struct.unpack_from('<I', adata, 96)[0]
print(f"file_count (v3 format, at 96): {file_count_v3}")
