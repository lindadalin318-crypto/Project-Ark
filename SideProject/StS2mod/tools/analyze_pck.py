"""
精确解析两个PCK文件的二进制结构
"""
import struct

def analyze_pck(path, label):
    with open(path, 'rb') as f:
        data = f.read()
    
    print(f"\n{'='*60}")
    print(f"Analyzing: {label}")
    print(f"File size: {len(data)} bytes")
    
    pos = 0
    magic = data[pos:pos+4]; pos += 4
    print(f"Magic: {magic}")
    
    pck_ver = struct.unpack_from('<I', data, pos)[0]; pos += 4
    major   = struct.unpack_from('<I', data, pos)[0]; pos += 4
    minor   = struct.unpack_from('<I', data, pos)[0]; pos += 4
    patch   = struct.unpack_from('<I', data, pos)[0]; pos += 4
    print(f"PCK ver: {pck_ver}, Godot: {major}.{minor}.{patch}")
    
    # v2格式: 之后是16个uint32保留字段（已确认offset 20-83是reserved，total 64bytes）
    # 但根据实验，文件数在offset 96，所以reserved是 (96-20)/4 = 19 个 uint32？
    # 不对... 让我先打印offset 16到100的所有uint32
    print(f"\nOffsets 16-100 (uint32 values):")
    for off in range(16, 104, 4):
        val = struct.unpack_from('<I', data, off)[0]
        if val != 0:
            print(f"  [{off:3d}] = {val} (0x{val:08X})")
    
    # 明确找文件数所在位置
    # 我们知道第一个路径是 res://.godot/global_script_class_cache.cfg (44字节含padding)
    target = b'res://.godot/global_script_class_cache.cfg'
    idx = data.find(target)
    if idx > 0:
        file_count_offset = idx - 4 - 4  # path_len(4) comes before path, file_count(4) before all entries
        file_count = struct.unpack_from('<I', data, file_count_offset)[0]
        path_len = struct.unpack_from('<I', data, file_count_offset + 4)[0]
        print(f"\nFirst path found at offset: {idx}")
        print(f"path_len at {file_count_offset+4}: {path_len}")
        print(f"file_count at {file_count_offset}: {file_count}")
        
        header_end = file_count_offset  # everything before file_count is header
        print(f"\nHeader ends at offset: {header_end}")
        print(f"Reserved bytes: {header_end - 20} bytes = {(header_end-20)//4} uint32s")
        
        # 现在解析所有文件
        pos = file_count_offset + 4  # skip file_count
        files = []
        for i in range(file_count):
            path_len = struct.unpack_from('<I', data, pos)[0]; pos += 4
            path_bytes = data[pos:pos+path_len]
            path = path_bytes.rstrip(b'\x00').decode('utf-8', errors='replace')
            pos += path_len
            
            offset = struct.unpack_from('<Q', data, pos)[0]; pos += 8
            size   = struct.unpack_from('<Q', data, pos)[0]; pos += 8
            md5    = data[pos:pos+16]; pos += 16
            
            # Check if flags field exists (pck v2 added flags)
            # Try reading 4 more bytes as flags
            if pck_ver >= 2:
                flags = struct.unpack_from('<I', data, pos)[0]; pos += 4
            else:
                flags = 0
            
            files.append({'path': path, 'offset': offset, 'size': size, 
                         'md5': md5.hex(), 'flags': flags, 'path_len': path_len})
            print(f"  [{i}] {path!r}")
            print(f"       offset={offset}, size={size}, flags={flags}, path_len={path_len}")
        
        print(f"\nData region starts at: {pos}")
        
        # Verify first file
        if files:
            f0 = files[0]
            content = data[f0['offset']:f0['offset']+f0['size']]
            print(f"\nFirst file content: {content[:100]}")
        
        return {'header_end': file_count_offset, 'pck_ver': pck_ver, 
                'major': major, 'minor': minor, 'patch': patch,
                'files': files, 'data': data, 'file_count_offset': file_count_offset}

# BetterSpire2
r1 = analyze_pck(
    r"F:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\BetterSpire2-2-1-62-1773426336\BetterSpire2.pck",
    "BetterSpire2.pck"
)

# 我们的PCK
r2 = analyze_pck(
    r"F:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Astrolabe\Astrolabe.pck",
    "Astrolabe.pck (current)"
)

print("\n\n=== DIFFERENCE SUMMARY ===")
print(f"BetterSpire2: PCK ver={r1['pck_ver']}, Godot={r1['major']}.{r1['minor']}.{r1['patch']}, header_end={r1['header_end']}")
print(f"Astrolabe:    PCK ver={r2['pck_ver']}, Godot={r2['major']}.{r2['minor']}.{r2['patch']}, header_end={r2['header_end']}")
