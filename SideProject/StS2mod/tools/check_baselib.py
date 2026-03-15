import struct
data = open(r'D:\UnityProjects\Project-Ark\SideProject\StS2mod\tools\BaseLib.pck','rb').read()
print(f'Size: {len(data)}')
print(f'Magic: {data[:4]}')
pck_ver = struct.unpack_from('<I',data,4)[0]
major = struct.unpack_from('<I',data,8)[0]
minor = struct.unpack_from('<I',data,12)[0]
patch = struct.unpack_from('<I',data,16)[0]
print(f'PCK v{pck_ver}, Godot {major}.{minor}.{patch}')

# hex dump first 100 bytes
print('\nHex dump:')
for i in range(0, min(200, len(data)), 16):
    row = data[i:i+16]
    hex_part = ' '.join(f'{b:02X}' for b in row)
    asc_part = ''.join(chr(b) if 32 <= b < 127 else '.' for b in row)
    print(f'{i:4d}: {hex_part:<48s} {asc_part}')

# find res:// paths
print('\nPaths:')
i = 0
while i < len(data)-6:
    if data[i:i+6] == b'res://':
        j = i
        p = bytearray()
        while j < len(data) and data[j] != 0:
            p.append(data[j])
            j += 1
        try:
            print(f'  offset={i}: {p.decode("utf-8")}')
        except:
            print(f'  offset={i}: (decode error)')
    i += 1

# find manifest content
idx = data.find(b'pck_name')
if idx >= 0:
    print(f'\nmanifest at offset {idx}: {data[idx-5:idx+80]}')
