m = '7fe7ffff7fe7ffff7fe7ffffffffffff7fe7ffff7fe7ffff7ffffe7b08e7ff3bffe6ffbfffe7fffbffe7fff948e0ffff48e0ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff'
names = ['Default','TransparentFX','IgnoreRaycast','(3)','Water','UI','Player','PlayerProjectile','Enemy','Wall','EchoWave','RoomBounds','Hazard']
layers = [m[i*8:(i+1)*8] for i in range(32)]
for i in range(13):
    mask = int(layers[i], 16)
    collisions = []
    for j in range(13):
        if mask & (1 << j):
            collisions.append(names[j])
    print(f"Layer {i:2d} ({names[i]:20s}): 0x{layers[i]} collides_with: {', '.join(collisions)}")

print()
player = int(layers[6], 16)
rb = int(layers[11], 16)
print(f"Player(6) bit 11 set (collides RoomBounds): {bool(player & (1 << 11))}")
print(f"RoomBounds(11) bit 6 set (collides Player): {bool(rb & (1 << 6))}")
