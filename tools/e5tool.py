#!/usr/bin/env python3
"""
e5tool.py — 曹操传（Ekd5 引擎）Ls11/Ls12 封包解析工具

用法:
  python3 e5tool.py list   <file.e5>              # 列出封包内条目
  python3 e5tool.py unpack <file.e5> <out_dir>    # 解包全部条目
  python3 e5tool.py info   <file.e5>              # 头部信息
  python3 e5tool.py map    <hexzmap.e5> <序号>    # 以字符画预览一张战场地形图
  python3 e5tool.py hmrender <kw目录> <序号> <out.png>   # 渲染原版战场画面为 PNG
  python3 e5tool.py smlrender <kw目录> <序号> <out.png>  # 渲染战场小地图为 PNG
  python3 e5tool.py unitrender <kw目录> <形象号> <out.png> # 渲染单位行走图 20 帧

格式说明（社区逆向成果，公开资料；本实现为原创代码）:
  0x000            "Ls11" / "Ls12" + 12 字节填充
  0x010            256 字节字典
  0x110            条目表：每条 12 字节（大端 u32 x3：压缩长度、原始长度、数据偏移），
                   压缩长度为 0 表示表结束；压缩长度 == 原始长度表示该条目未压缩
  解压算法          MSB 位流；变长码：读 1 直到读到 0（前缀），前缀值 code1，
                   再读同样位数得 code2，码值 = code1 + code2。
                   码值 < 0x100 → 输出 dictionary[码值]
                   码值 >= 0x100 → 回溯偏移 = 码值 - 0x100，长度 = 下一个码值 + 3，
                   从已输出数据回拷（允许重叠）。
"""
import os
import struct
import sys


class BitReader:
    def __init__(self, data: bytes, pos: int):
        self.data = data
        self.pos = pos
        self.cur = 0
        self.nbits = 0

    def bit(self) -> int:
        if self.nbits == 0:
            self.cur = self.data[self.pos]
            self.pos += 1
            self.nbits = 8
        b = (self.cur >> 7) & 1
        self.cur = (self.cur << 1) & 0xFF
        self.nbits -= 1
        return b

    def bits(self, n: int) -> int:
        v = 0
        for _ in range(n):
            v = (v << 1) | self.bit()
        return v


def read_code(br: BitReader) -> int:
    code1 = 0
    n = 0
    while True:
        b = br.bit()
        code1 = (code1 << 1) | b
        n += 1
        if b == 0:
            break
    code2 = br.bits(n)
    return code1 + code2


def ls_parse(path: str):
    data = open(path, "rb").read()
    magic = data[:4]
    if magic not in (b"Ls11", b"Ls12"):
        raise ValueError(f"{path}: 不是 Ls11/Ls12 封包 (magic={magic!r})")
    dictionary = data[0x10:0x110]
    entries = []
    off = 0x110
    while True:
        comp, orig, pos = struct.unpack_from(">III", data, off)
        off += 12
        if comp == 0:
            break
        entries.append((comp, orig, pos))
    return magic.decode(), dictionary, entries, data


def ls_decode(data: bytes, dictionary: bytes, offset: int, orig_size: int) -> bytes:
    out = bytearray()
    br = BitReader(data, offset)
    while len(out) < orig_size:
        code = read_code(br)
        if code < 0x100:
            out.append(dictionary[code])
            continue
        back = code - 0x100
        if back > len(out):
            raise ValueError(f"回溯越界: back={back} produced={len(out)}")
        count = read_code(br) + 3
        for _ in range(count):
            out.append(out[-back])
    if len(out) != orig_size:
        raise ValueError(f"长度不符: got={len(out)} want={orig_size}")
    return bytes(out)


def ls_extract(path: str, index: int) -> bytes:
    _, dictionary, entries, data = ls_parse(path)
    comp, orig, pos = entries[index]
    if comp == orig:
        return data[pos:pos + orig]
    return ls_decode(data, dictionary, pos, orig)


def cmd_info(path: str):
    magic, _, entries, data = ls_parse(path)
    print(f"{path}: {magic}, {len(entries)} 个条目, 文件 {len(data)} 字节")


def cmd_list(path: str):
    magic, _, entries, _ = ls_parse(path)
    print(f"{path}: {magic}, {len(entries)} 个条目")
    print(f"{'idx':>4} {'压缩':>10} {'原始':>10} {'偏移':>10} 压缩率")
    for i, (comp, orig, pos) in enumerate(entries):
        flag = "存储" if comp == orig else f"{comp * 100 // max(orig, 1):3d}%"
        print(f"{i:>4} {comp:>10} {orig:>10} {pos:>10} {flag}")


def cmd_unpack(path: str, out_dir: str):
    magic, dictionary, entries, data = ls_parse(path)
    os.makedirs(out_dir, exist_ok=True)
    base = os.path.splitext(os.path.basename(path))[0]
    for i, (comp, orig, pos) in enumerate(entries):
        if comp == orig:
            blob = data[pos:pos + orig]
        else:
            blob = ls_decode(data, dictionary, pos, orig)
        out = os.path.join(out_dir, f"{base}_{i:03d}.bin")
        with open(out, "wb") as f:
            f.write(blob)
        print(f"[{i}] -> {out} ({len(blob)} 字节)")


def cmd_map(path: str, index: int):
    """hexzmap 条目：1 字节宽×3、1 字节高×3、然后 w*h 个地形 ID（行主序）。"""
    blob = ls_extract(path, index)
    w, h = blob[0] // 3, blob[1] // 3
    if 2 + w * h > len(blob):
        print(f"条目 {index} 不符合 hexzmap 结构 (len={len(blob)}, head={list(blob[:2])})")
        return
    grid = blob[2:]
    chars = "._23456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"
    print(f"map[{index}]: {w}x{h}  (字符=地形ID: '.'=0 '_'=1 '2'=2 ... 'a'=10 ...)")
    for y in range(h):
        print(''.join(chars[min(grid[y * w + x], len(chars) - 1)] for x in range(w)))


def _write_png_rgb(path, w, h, rows):
    import zlib
    def chunk(tag, data):
        c = tag + data
        return struct.pack(">I", len(data)) + c + struct.pack(">I", zlib.crc32(c) & 0xffffffff)
    ihdr = struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0)
    raw = b"".join(b"\x00" + r for r in rows)
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) +
                chunk(b"IDAT", zlib.compress(raw, 6)) + chunk(b"IEND", b""))


def cmd_hmrender(kw_dir: str, index: int, out_png: str):
    """
    渲染原版战场画面：
      Hm??.e5 = 地图 w×h 个 96×24 像素 8bpp 图块（行主序，格子轴对齐无偏移）
      Spalet.e5 = 每关 768 字节 RGB 调色板（Ls12 封包，序号对应地图）
      尺寸取自 hexzmap.e5 同序号条目
    """
    hex_path = os.path.join(kw_dir, "hexzmap.e5")
    hm_path = os.path.join(kw_dir, f"Hm{index:02d}.e5")
    pal_path = os.path.join(kw_dir, "Spalet.e5")

    blob = ls_extract(hex_path, index)
    w, h = blob[0] // 3, blob[1] // 3

    hm = open(hm_path, "rb").read()
    need = w * h * 2304
    if len(hm) < need:
        raise ValueError(f"{hm_path} 大小 {len(hm)} < 期望 {need} (地图 {w}x{h})")

    _, _, pal_entries, _ = ls_parse(pal_path)
    pal_raw = ls_extract(pal_path, min(index, len(pal_entries) - 1))
    # 实测调色板字节序：存储 (b0,b1,b2) → 显示 (R,G,B) = (b1,b2,b0)
    pal = [bytes((pal_raw[i*3+1], pal_raw[i*3+2], pal_raw[i*3])) if i*3+2 < len(pal_raw)
           else bytes((i, i, i)) for i in range(256)]

    TW, TH = 96, 24
    imw, imh = w * TW, h * TH
    canvas = [bytearray(imw * 3) for _ in range(imh)]
    for ti in range(w * h):
        tile = hm[ti*2304:(ti+1)*2304]
        tx, ty = (ti % w) * TW, (ti // w) * TH
        for y in range(TH):
            row = canvas[ty + y]
            trow = tile[y*TW:(y+1)*TW]
            for x in range(TW):
                row[(tx+x)*3:(tx+x)*3+3] = pal[trow[x]]
    _write_png_rgb(out_png, imw, imh, [bytes(r) for r in canvas])
    print(f"已输出 {out_png} ({imw}x{imh})")


def cmd_smlrender(kw_dir: str, index: int, out_png: str):
    """小地图：Smlmap.e5 每条 = 地图 (w*6)×(h*6) 像素 8bpp，调色板同 Spalet 同序号。"""
    blob = ls_extract(os.path.join(kw_dir, "hexzmap.e5"), index)
    w, h = blob[0] // 3 * 6, blob[1] // 3 * 6
    img = ls_extract(os.path.join(kw_dir, "Smlmap.e5"), index)
    if len(img) < w * h:
        raise ValueError(f"Smlmap[{index}] 大小 {len(img)} < 期望 {w*h}")
    pal_raw = ls_extract(os.path.join(kw_dir, "Spalet.e5"), index)
    pal = [bytes((pal_raw[i*3+1], pal_raw[i*3+2], pal_raw[i*3])) if i*3+2 < len(pal_raw)
           else bytes((i, i, i)) for i in range(256)]
    rows = []
    for y in range(h):
        row = bytearray()
        for x in range(w):
            row += pal[img[y*w+x]]
        rows.append(bytes(row))
    _write_png_rgb(out_png, w, h, rows)
    print(f"已输出 {out_png} ({w}x{h})")


def cmd_unitrender(kw_dir: str, sprite_id: int, out_png: str):
    """单位形象：Pmapobj.e5 每条 = 20 帧 48×64 8bpp，透明键=索引0，调色板 Pmpalet[0]。"""
    blob = ls_extract(os.path.join(kw_dir, "Pmapobj.e5"), sprite_id)
    pal_raw = ls_extract(os.path.join(kw_dir, "Pmpalet.e5"), 0)
    pal = [bytes((pal_raw[i*3+1], pal_raw[i*3+2], pal_raw[i*3])) for i in range(256)]
    FW, FH, N = 48, 64, 20
    cols = 5
    rows_n = (N + cols - 1) // cols
    W, H = cols * (FW + 2), rows_n * (FH + 2)
    canvas = [bytearray(W * 3) for _ in range(H)]
    for f in range(min(N, len(blob) // (FW * FH))):
        ox, oy = (f % cols) * (FW + 2), (f // cols) * (FH + 2)
        for y in range(FH):
            for x in range(FW):
                v = blob[f*FW*FH + y*FW + x]
                px = (ox + x) * 3
                canvas[oy+y][px:px+3] = b'\x28\x28\x28' if v == 0 else pal[v]
    _write_png_rgb(out_png, W, H, [bytes(r) for r in canvas])
    print(f"已输出 {out_png}（形象 {sprite_id}，20 帧）")


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        sys.exit(1)
    cmd, path = sys.argv[1], sys.argv[2]
    if cmd == "info":
        cmd_info(path)
    elif cmd == "list":
        cmd_list(path)
    elif cmd == "unpack":
        if len(sys.argv) < 4:
            print("unpack 需要输出目录")
            sys.exit(1)
        cmd_unpack(path, sys.argv[3])
    elif cmd == "map":
        if len(sys.argv) < 4:
            print("map 需要条目序号")
            sys.exit(1)
        cmd_map(path, int(sys.argv[3]))
    elif cmd == "hmrender":
        if len(sys.argv) < 5:
            print("hmrender 需要: <kw目录> <序号> <out.png>")
            sys.exit(1)
        cmd_hmrender(path, int(sys.argv[3]), sys.argv[4])
    elif cmd == "smlrender":
        if len(sys.argv) < 5:
            print("smlrender 需要: <kw目录> <序号> <out.png>")
            sys.exit(1)
        cmd_smlrender(path, int(sys.argv[3]), sys.argv[4])
    elif cmd == "unitrender":
        if len(sys.argv) < 5:
            print("unitrender 需要: <kw目录> <形象号> <out.png>")
            sys.exit(1)
        cmd_unitrender(path, int(sys.argv[3]), sys.argv[4])
    else:
        print(__doc__)
        sys.exit(1)


if __name__ == "__main__":
    main()
