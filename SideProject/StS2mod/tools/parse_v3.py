"""
PCK v3 格式（Godot 4.5+）:
magic(4) + pck_ver(4) + major(4) + minor(4) + patch(4)
+ pack_flags(4) + file_base(8)
+ reserved(16*4=64)
+ file_count(4)
+ [file_entries...]
+ [file_data at offsets relative to file_base]
"""
import struct

def parse_v3(path):
    data = open(path, 'rb').read()
    print(f"\n=== {path} ===")
    print(f"Size: {len(data)}")

    pos = 0
    magic = data[pos:pos+4]; pos += 4
    pck_ver = struct.unpack_from('<I', data, pos)[0]; pos += 4
    major   = struct.unpack_from('<I', data, pos)[0]; pos += 4
    minor   = struct.unpack_from('<I', data, pos)[0]; pos += 4
    patch   = struct.unpack_from('<I', data, pos)[0]; pos += 4
    pack_flags = struct.unpack_from('<I', data, pos)[0]; pos += 4
    file_base  = struct.unpack_from('<Q', data, pos)[0]; pos += 8
    print(f"PCK v{pck_ver}, Godot {major}.{minor}.{patch}")
    print(f"pack_flags={pack_flags} (bit0=relative_to_exe, bit1=encrypted_index)")
    print(f"file_base={file_base} (=0x{file_base:X})")
    
    # reserved: 16 * int32 = 64 bytes
    pos += 64  # skip reserved (pos goes from 28 to 92)
    
    file_count = struct.unpack_from('<I', data, pos)[0]; pos += 4
    print(f"file_count={file_count} at offset {pos-4}")
    
    files = []
    for i in range(file_count):
        path_len = struct.unpack_from('<I', data, pos)[0]; pos += 4
        fpath = data[pos:pos+path_len].rstrip(b'\x00').decode('utf-8'); pos += path_len
        # In v3: offset is relative to file_base
        offset = struct.unpack_from('<Q', data, pos)[0]; pos += 8
        size   = struct.unpack_from('<Q', data, pos)[0]; pos += 8
        md5    = data[pos:pos+16]; pos += 16
        flags  = struct.unpack_from('<I', data, pos)[0]; pos += 4
        
        abs_offset = file_base + offset
        file_data = data[abs_offset:abs_offset+size]
        files.append({'path': fpath, 'rel_offset': offset, 'abs_offset': abs_offset,
                      'size': size, 'flags': flags, 'data': file_data})
        
        preview = ''
        try:
            preview = file_data[:60].decode('utf-8').replace('\n','\\n').replace('\r','')
        except:
            preview = file_data[:20].hex()
        print(f"  [{i}] {fpath!r}")
        print(f"       abs_offset={abs_offset} size={size} flags={flags}")
        print(f"       preview: {preview!r}")
    
    return files

# 分析 BaseLib.pck
parse_v3(r'D:\UnityProjects\Project-Ark\SideProject\StS2mod\tools\BaseLib.pck')

# 分析当前的 Astrolabe.pck 
parse_v3(r'F:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Astrolabe\Astrolabe.pck')
