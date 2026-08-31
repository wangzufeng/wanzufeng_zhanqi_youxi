/**
 * Ls11 / Ls12 封包解压缩器（TypeScript 原生实现）
 * 适用于《三国志曹操传》Ekd5 引擎各类 .e5 文件
 */

class BitReader {
  private data: Uint8Array;
  private pos: number;
  private cur: number = 0;
  private nbits: number = 0;

  constructor(data: Uint8Array, offset: number) {
    this.data = data;
    this.pos = offset;
  }

  public bit(): number {
    if (this.nbits === 0) {
      this.cur = this.data[this.pos++];
      this.nbits = 8;
    }
    const b = (this.cur >> 7) & 1;
    this.cur = (this.cur << 1) & 0xff;
    this.nbits--;
    return b;
  }

  public bits(n: number): number {
    let v = 0;
    for (let i = 0; i < n; i++) {
      v = (v << 1) | this.bit();
    }
    return v;
  }
}

function readCode(br: BitReader): number {
  let code1 = 0;
  let n = 0;
  while (true) {
    const b = br.bit();
    code1 = (code1 << 1) | b;
    n++;
    if (b === 0) break;
  }
  const code2 = br.bits(n);
  return code1 + code2;
}

export interface E5Entry {
  compressed: number;
  original: number;
  offset: number;
}

export class E5Archive {
  public magic: string;
  public entries: E5Entry[] = [];
  private data: Uint8Array;
  private dict: Uint8Array = new Uint8Array(256);

  constructor(data: Uint8Array) {
    this.data = data;
    const magicChars = String.fromCharCode(data[0], data[1], data[2], data[3]);
    if (magicChars !== 'Ls11' && magicChars !== 'Ls12') {
      throw new Error(`Invalid magic: ${magicChars}`);
    }
    this.magic = magicChars;
    this.dict.set(data.subarray(0x10, 0x110));

    const view = new DataView(data.buffer, data.byteOffset, data.byteLength);
    let off = 0x110;
    while (off + 12 <= data.byteLength) {
      const comp = view.getUint32(off, false);
      const orig = view.getUint32(off + 4, false);
      const pos = view.getUint32(off + 8, false);
      off += 12;
      if (comp === 0) break;
      this.entries.push({ compressed: comp, original: orig, offset: pos });
    }
  }

  public get count(): number {
    return this.entries.length;
  }

  public extract(index: number): Uint8Array {
    if (index < 0 || index >= this.entries.length) {
      throw new Error(`Entry index out of bounds: ${index}`);
    }
    const e = this.entries[index];
    if (e.compressed === e.original) {
      return this.data.subarray(e.offset, e.offset + e.original);
    }

    const out = new Uint8Array(e.original);
    let produced = 0;
    const br = new BitReader(this.data, e.offset);

    while (produced < e.original) {
      const code = readCode(br);
      if (code < 0x100) {
        out[produced++] = this.dict[code];
        continue;
      }
      const back = code - 0x100;
      if (back > produced) {
        throw new Error(`Ls12 corrupt back pointer: ${back} > ${produced}`);
      }
      const count = readCode(br) + 3;
      for (let i = 0; i < count && produced < e.original; i++) {
        out[produced] = out[produced - back];
        produced++;
      }
    }
    return out;
  }
}
