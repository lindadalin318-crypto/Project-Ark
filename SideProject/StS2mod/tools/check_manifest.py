import struct, zlib

data = open(r'F:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\BetterSpire2-2-1-62-1773426336\BetterSpire2.pck','rb').read()

# BetterSpire2 manifest 在 offset=3584, size=283
manifest_raw = data[3584:3584+283]
print("Raw manifest bytes (hex):", manifest_raw[:20].hex())
print("Raw manifest (repr):", repr(manifest_raw[:40]))

# 检查 Godot PCK 文件数据格式
# Godot PCK 中文件可能有 flags=0 (无压缩) 或 flags=1 (zstd压缩)
# 根据 BetterSpire2 flags=0，所以应该是明文
# 但数据看起来是乱码...

# 实际上我们之前读到的 BetterSpire2 manifest 内容是 JSON 明文
# 因为之前运行 analyze_pck.py 时看到了明文JSON
# 那个"乱码"是 PowerShell 解析 Python 输出时的乱码，不是数据本身的问题

# 直接验证
print("\nFull manifest content:")
print(manifest_raw.decode('utf-8', errors='replace'))
