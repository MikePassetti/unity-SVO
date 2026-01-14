public static class MortonEncoder {
    public static ulong EncodeMorton(int x, int y, int z) {
        return (Part1By2((ulong)x) << 0) | (Part1By2((ulong)y) << 1) | (Part1By2((ulong)z) << 2);
    }

    public static void DecodeMorton(ulong code, out int x, out int y, out int z) {
        x = (int)Compact1By2(code >> 0);
        y = (int)Compact1By2(code >> 1);
        z = (int)Compact1By2(code >> 2);
    }

    private static ulong Part1By2(ulong n) {
        n &= 0x1fffff; // 21 bits
        n = (n | (n << 32)) & 0x1f00000000ffff;
        n = (n | (n << 16)) & 0x1f0000ff0000ff;
        n = (n | (n << 8)) & 0x100f00f00f00f00f;
        n = (n | (n << 4)) & 0x10c30c30c30c30c3;
        n = (n | (n << 2)) & 0x1249249249249249;
        return n;
    }

    private static ulong Compact1By2(ulong n) {
        n &= 0x1249249249249249;
        n = (n ^ (n >> 2)) & 0x10c30c30c30c30c3;
        n = (n ^ (n >> 4)) & 0x100f00f00f00f00f;
        n = (n ^ (n >> 8)) & 0x1f0000ff0000ff;
        n = (n ^ (n >> 16)) & 0x1f00000000ffff;
        n = (n ^ (n >> 32)) & 0x1fffff;
        return n;
    }
}


