import struct

data = open(r'F:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Astrolabe\Astrolabe.pck','rb').read()
print(f"PCK size: {len(data)} bytes")

# 打出前200字节的hex dump
print("\n=== Hex dump (first 200 bytes) ===")
for i in range(0, 200, 16):
    row = data[i:i+16]
    hex_part = ' '.join(f'{b:02X}' for b in row)
    asc_part = ''.join(chr(b) if 32 <= b < 127 else '.' for b in row)
    print(f"{i:4d}: {hex_part:<48s} {asc_part}")

# 找所有 res:// 路径
print("\n=== All res:// paths ===")
i = 0
while i < len(data) - 6:
    if data[i:i+6] == b'res://':
        path_bytes = bytearray()
        j = i
        while j < len(data) and data[j] != 0:
            path_bytes.append(data[j])
            j += 1
        path = path_bytes.decode('utf-8', 'replace')
        # 往前4字节是path_len
        path_len = struct.unpack_from('<I', data, i-4)[0] if i >= 4 else 0
        print(f"  offset={i}, path_len={path_len}, path={path!r}")
    i += 1

# 扫描合理的file_count
print("\n=== Possible file_count positions ===")
for off in range(20, 150, 4):
    if off + 4 > len(data):
        break
    val = struct.unpack_from('<I', data, off)[0]
    if 1 <= val <= 15:
        print(f"  [{off}] = {val}")
