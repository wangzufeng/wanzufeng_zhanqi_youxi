import fs from 'fs';
import path from 'path';
import zlib from 'zlib';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const KW_DIR = path.resolve(__dirname, '../../kw');
const OUT_DIR = path.resolve(__dirname, '../public/assets');

console.log(`[Asset Extractor] Source kw dir: ${KW_DIR}`);
console.log(`[Asset Extractor] Output assets dir: ${OUT_DIR}`);

// -------------------------------------------------------------
// PNG Builder (Zero dependencies, uses native zlib)
// -------------------------------------------------------------
function makeCrcTable() {
  const table = new Uint32Array(256);
  for (let n = 0; n < 256; n++) {
    let c = n;
    for (let k = 0; k < 8; k++) {
      if (c & 1) c = 0xedb88320 ^ (c >>> 1);
      else c = c >>> 1;
    }
    table[n] = c;
  }
  return table;
}
const CRC_TABLE = makeCrcTable();

function crc32(buf) {
  let crc = 0xffffffff;
  for (let i = 0; i < buf.length; i++) {
    crc = CRC_TABLE[(crc ^ buf[i]) & 0xff] ^ (crc >>> 8);
  }
  return (crc ^ 0xffffffff) >>> 0;
}

function writePngRgba(width, height, rgbaBuffer) {
  // rgbaBuffer is width * height * 4
  const rowSize = width * 4;
  const rawData = Buffer.alloc(height * (rowSize + 1));
  for (let y = 0; y < height; y++) {
    const rawPos = y * (rowSize + 1);
    rawData[rawPos] = 0; // Filter: None
    rgbaBuffer.copy(rawData, rawPos + 1, y * rowSize, (y + 1) * rowSize);
  }
  const compressed = zlib.deflateSync(rawData);

  const pngHeader = Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);

  function makeChunk(type, data) {
    const len = Buffer.alloc(4);
    len.writeUInt32BE(data.length, 0);
    const typeBuf = Buffer.from(type, 'ascii');
    const crcBuf = Buffer.alloc(4);
    const combined = Buffer.concat([typeBuf, data]);
    crcBuf.writeUInt32BE(crc32(combined), 0);
    return Buffer.concat([len, typeBuf, data, crcBuf]);
  }

  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr[8] = 8; // 8 bit depth
  ihdr[9] = 6; // RGBA
  ihdr[10] = 0; // compression
  ihdr[11] = 0; // filter
  ihdr[12] = 0; // interlace

  const ihdrChunk = makeChunk('IHDR', ihdr);
  const idatChunk = makeChunk('IDAT', compressed);
  const iendChunk = makeChunk('IEND', Buffer.alloc(0));

  return Buffer.concat([pngHeader, ihdrChunk, idatChunk, iendChunk]);
}

// -------------------------------------------------------------
// Ls11 / Ls12 Decompressor
// -------------------------------------------------------------
class BitReader {
  constructor(buf, offset) {
    this.buf = buf;
    this.pos = offset;
    this.cur = 0;
    this.nbits = 0;
  }
  bit() {
    if (this.nbits === 0) {
      this.cur = this.buf[this.pos++];
      this.nbits = 8;
    }
    const b = (this.cur >> 7) & 1;
    this.cur = (this.cur << 1) & 0xff;
    this.nbits--;
    return b;
  }
  bits(n) {
    let v = 0;
    for (let i = 0; i < n; i++) {
      v = (v << 1) | this.bit();
    }
    return v;
  }
}

function readCode(br) {
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

function parseE5Archive(buf) {
  if (!buf || buf.length < 0x110 + 12) return null;
  const magic = buf.subarray(0, 4).toString('ascii');
  if (magic !== 'Ls11' && magic !== 'Ls12') return null;

  const dict = buf.subarray(0x10, 0x110);
  const entries = [];
  let off = 0x110;
  while (off + 12 <= buf.length) {
    const comp = buf.readUInt32BE(off);
    const orig = buf.readUInt32BE(off + 4);
    const pos = buf.readUInt32BE(off + 8);
    off += 12;
    if (comp === 0) break;
    entries.push({ comp, orig, pos });
  }

  function extract(idx) {
    const e = entries[idx];
    if (!e) return null;
    if (e.comp === e.orig) {
      return buf.subarray(e.pos, e.pos + e.orig);
    }
    const out = Buffer.alloc(e.orig);
    let produced = 0;
    const br = new BitReader(buf, e.pos);
    while (produced < e.orig) {
      const code = readCode(br);
      if (code < 0x100) {
        out[produced++] = dict[code];
        continue;
      }
      const back = code - 0x100;
      if (back > produced) {
        throw new Error(`Ls12 corrupt back pointer: ${back} > produced ${produced}`);
      }
      const count = readCode(br) + 3;
      for (let i = 0; i < count && produced < e.orig; i++) {
        out[produced] = out[produced - back];
        produced++;
      }
    }
    return out;
  }

  return { magic, count: entries.length, entries, extract };
}

function decodePalette(raw) {
  const pal = [];
  for (let i = 0; i < 256; i++) {
    if (i * 3 + 2 < raw.length) {
      // (b0, b1, b2) in E5 -> (R, G, B) = (b1, b2, b0)
      const r = raw[i * 3 + 1];
      const g = raw[i * 3 + 2];
      const b = raw[i * 3 + 0];
      pal.push([r, g, b]);
    } else {
      pal.push([i, i, i]);
    }
  }
  return pal;
}

const gbkDecoder = new TextDecoder('gbk');
function decodeGbk(buf, start = 0, len = buf.length) {
  const sub = buf.subarray(start, start + len);
  let nullPos = sub.indexOf(0);
  if (nullPos === -1) nullPos = sub.length;
  return gbkDecoder.decode(sub.subarray(0, nullPos)).trim();
}

// -------------------------------------------------------------
// Extraction Tasks
// -------------------------------------------------------------
async function runExtraction() {
  fs.mkdirSync(path.join(OUT_DIR, 'faces'), { recursive: true });
  fs.mkdirSync(path.join(OUT_DIR, 'sprites'), { recursive: true });
  fs.mkdirSync(path.join(OUT_DIR, 'items'), { recursive: true });
  fs.mkdirSync(path.join(OUT_DIR, 'minimaps'), { recursive: true });
  fs.mkdirSync(path.join(OUT_DIR, 'battlemaps'), { recursive: true });
  fs.mkdirSync(path.join(OUT_DIR, 'data'), { recursive: true });
  fs.mkdirSync(path.join(OUT_DIR, 'audio/bgm'), { recursive: true });
  fs.mkdirSync(path.join(OUT_DIR, 'audio/sfx'), { recursive: true });

  console.log('1. Parsing Palettes (Pmpalet.e5 & Spalet.e5)...');
  const pmpaletBuf = fs.readFileSync(path.join(KW_DIR, 'Pmpalet.e5'));
  const pmpaletArc = parseE5Archive(pmpaletBuf);
  const defaultPalette = decodePalette(pmpaletArc.extract(0));

  const spaletBuf = fs.readFileSync(path.join(KW_DIR, 'Spalet.e5'));
  const spaletArc = parseE5Archive(spaletBuf);

  console.log('2. Parsing Imsg.e5 Text Records...');
  const imsgBuf = fs.readFileSync(path.join(KW_DIR, 'Imsg.e5'));
  const imsgRecords = [];
  const IMSG_STRIDE = 200;
  const imsgCount = Math.floor(imsgBuf.length / IMSG_STRIDE);
  for (let i = 0; i < imsgCount; i++) {
    imsgRecords.push(decodeGbk(imsgBuf, i * IMSG_STRIDE, IMSG_STRIDE));
  }

  const battleNames = [];
  for (let i = 250; i < 250 + 90; i++) {
    if (i < imsgRecords.length && imsgRecords[i]) {
      battleNames.push(imsgRecords[i]);
    }
  }

  console.log('3. Parsing Data.e5 (Officers, Items, Strategies, Classes)...');
  const dataBuf = fs.readFileSync(path.join(KW_DIR, 'Data.e5'));
  const dataArc = parseE5Archive(dataBuf);

  // Officers (Entry 0: 32 bytes per officer)
  const officerTableBuf = dataArc.extract(0);
  const officers = [];
  const officerCount = Math.floor(officerTableBuf.length / 32);
  for (let i = 0; i < officerCount; i++) {
    const o = i * 32;
    if (officerTableBuf[o] === 0) continue;
    const name = decodeGbk(officerTableBuf, o, 13) || `武将${i}`;
    const spriteId = officerTableBuf.readUInt16LE(o + 13);
    const faceId = officerTableBuf[o + 15];
    const classId = officerTableBuf[o + 16];
    const stats5 = [
      officerTableBuf[o + 18], // Force / 武力
      officerTableBuf[o + 19], // Intelligence / 智力
      officerTableBuf[o + 20], // Command / 统率
      officerTableBuf[o + 21], // Agility / 敏捷
      officerTableBuf[o + 22]  // Luck / 运气
    ];
    const hpBase = officerTableBuf.readUInt16LE(o + 23);
    const level = Math.max(1, officerTableBuf[o + 27] || 1);
    const history = imsgRecords[450 + i] || '';
    const retire = imsgRecords[650 + i] || '';
    const critical = imsgRecords[700 + i] || '';

    officers.push({
      id: i,
      name,
      spriteId,
      faceId,
      classId,
      stats: {
        force: stats5[0],
        intel: stats5[1],
        command: stats5[2],
        agility: stats5[3],
        luck: stats5[4]
      },
      hpBase,
      level,
      history,
      retire,
      critical
    });
  }

  // Items (Entry 1: 25 bytes per item)
  const itemTableBuf = dataArc.extract(1);
  const items = [];
  const itemCount = Math.floor(itemTableBuf.length / 25);
  for (let i = 0; i < itemCount; i++) {
    const o = i * 25;
    const name = decodeGbk(itemTableBuf, o, 12);
    if (!name) continue;
    const atk = itemTableBuf[o + 15] <= 50 ? itemTableBuf[o + 15] : 0;
    const def = itemTableBuf[o + 16] <= 50 ? itemTableBuf[o + 16] : 0;
    const desc = imsgRecords[i] || '';
    items.push({
      id: i,
      name,
      atk,
      def,
      desc
    });
  }

  // Strategies (Entry 5: 97 bytes per skill)
  const strategyTableBuf = dataArc.extract(5);
  const strategies = [];
  const stratStride = 97;
  const stratCount = Math.floor(strategyTableBuf.length / stratStride);
  for (let i = 0; i < stratCount; i++) {
    const o = i * stratStride;
    const name = decodeGbk(strategyTableBuf, o, 10);
    if (!name) continue;
    const mp = strategyTableBuf[o + 15];
    const power = Math.max(strategyTableBuf[o + 18], strategyTableBuf[o + 19]);
    strategies.push({
      id: i,
      name,
      mp,
      power: power || 12
    });
  }

  fs.writeFileSync(path.join(OUT_DIR, 'data/officers.json'), JSON.stringify(officers, null, 2));
  fs.writeFileSync(path.join(OUT_DIR, 'data/items.json'), JSON.stringify(items, null, 2));
  fs.writeFileSync(path.join(OUT_DIR, 'data/strategies.json'), JSON.stringify(strategies, null, 2));
  fs.writeFileSync(path.join(OUT_DIR, 'data/battles.json'), JSON.stringify(battleNames, null, 2));
  console.log(`Saved officers (${officers.length}), items (${items.length}), strategies (${strategies.length}), battle names (${battleNames.length})`);

  console.log('4. Extracting Face Portraits (Face.e5)...');
  const faceBuf = fs.readFileSync(path.join(KW_DIR, 'Face.e5'));
  const faceArc = parseE5Archive(faceBuf);
  const FW = 64, FH = 80;
  for (let i = 0; i < faceArc.count; i++) {
    const raw = faceArc.extract(i);
    if (!raw || raw.length < FW * FH) continue;
    const rgba = Buffer.alloc(FW * FH * 4);
    for (let y = 0; y < FH; y++) {
      for (let x = 0; x < FW; x++) {
        const palIdx = raw[y * FW + x];
        const [r, g, b] = defaultPalette[palIdx] || [0, 0, 0];
        const dst = (y * FW + x) * 4;
        rgba[dst] = r;
        rgba[dst + 1] = g;
        rgba[dst + 2] = b;
        rgba[dst + 3] = 255;
      }
    }
    const png = writePngRgba(FW, FH, rgba);
    fs.writeFileSync(path.join(OUT_DIR, `faces/face_${i}.png`), png);
  }
  console.log(`Extracted ${faceArc.count} face portraits.`);

  console.log('5. Extracting Item Icons (Item.e5)...');
  const itemBuf = fs.readFileSync(path.join(KW_DIR, 'Item.e5'));
  const itemArc = parseE5Archive(itemBuf);
  const IW = 32, IH = 32;
  for (let i = 0; i < itemArc.count; i++) {
    const raw = itemArc.extract(i);
    if (!raw || raw.length < IW * IH) continue;
    const rgba = Buffer.alloc(IW * IH * 4);
    for (let y = 0; y < IH; y++) {
      for (let x = 0; x < IW; x++) {
        const palIdx = raw[y * IW + x];
        const dst = (y * IW + x) * 4;
        if (palIdx === 0) {
          rgba[dst + 3] = 0; // Transparent
        } else {
          const [r, g, b] = defaultPalette[palIdx] || [0, 0, 0];
          rgba[dst] = r;
          rgba[dst + 1] = g;
          rgba[dst + 2] = b;
          rgba[dst + 3] = 255;
        }
      }
    }
    const png = writePngRgba(IW, IH, rgba);
    fs.writeFileSync(path.join(OUT_DIR, `items/item_${i}.png`), png);
  }
  console.log(`Extracted ${itemArc.count} item icons.`);

  console.log('6. Extracting Unit Action Spritesheets (Pmapobj.e5)...');
  const spriteBuf = fs.readFileSync(path.join(KW_DIR, 'Pmapobj.e5'));
  const spriteArc = parseE5Archive(spriteBuf);
  const SW = 48, SH = 64, FRAMES = 20;
  // Sprite sheet layout: 5 cols x 4 rows -> (5 * 48) x (4 * 64) = 240 x 256
  const SHEET_COLS = 5;
  const SHEET_ROWS = 4;
  const SHEET_W = SHEET_COLS * SW;
  const SHEET_H = SHEET_ROWS * SH;

  for (let i = 0; i < spriteArc.count; i++) {
    const raw = spriteArc.extract(i);
    if (!raw || raw.length < SW * SH) continue;
    const count = Math.min(FRAMES, Math.floor(raw.length / (SW * SH)));
    const rgba = Buffer.alloc(SHEET_W * SHEET_H * 4);

    for (let f = 0; f < count; f++) {
      const col = f % SHEET_COLS;
      const row = Math.floor(f / SHEET_COLS);
      const startX = col * SW;
      const startY = row * SH;
      const frameOffset = f * SW * SH;

      for (let y = 0; y < SH; y++) {
        for (let x = 0; x < SW; x++) {
          const palIdx = raw[frameOffset + y * SW + x];
          const dst = ((startY + y) * SHEET_W + (startX + x)) * 4;
          if (palIdx === 0) {
            rgba[dst + 3] = 0;
          } else {
            const [r, g, b] = defaultPalette[palIdx] || [0, 0, 0];
            rgba[dst] = r;
            rgba[dst + 1] = g;
            rgba[dst + 2] = b;
            rgba[dst + 3] = 255;
          }
        }
      }
    }
    const png = writePngRgba(SHEET_W, SHEET_H, rgba);
    fs.writeFileSync(path.join(OUT_DIR, `sprites/unit_${i}.png`), png);
  }
  console.log(`Extracted ${spriteArc.count} unit action sprite sheets.`);

  console.log('7. Extracting Maps & Backgrounds (hexzmap.e5 & Hm??.e5)...');
  const hexzmapBuf = fs.readFileSync(path.join(KW_DIR, 'hexzmap.e5'));
  const hexzmapArc = parseE5Archive(hexzmapBuf);
  const mapsManifest = [];

  const smlBuf = fs.readFileSync(path.join(KW_DIR, 'Smlmap.e5'));
  const smlArc = parseE5Archive(smlBuf);

  for (let i = 0; i < hexzmapArc.count; i++) {
    const blob = hexzmapArc.extract(i);
    if (!blob || blob.length < 2) continue;
    const w = Math.floor(blob[0] / 3);
    const h = Math.floor(blob[1] / 3);
    if (blob.length < 2 + w * h) continue;
    const grid = Array.from(blob.subarray(2, 2 + w * h));
    const title = battleNames[i] || `第 ${i + 1} 战`;

    // Minimap extraction
    let hasMinimap = false;
    if (i < smlArc.count) {
      const smlBlob = smlArc.extract(i);
      const palRaw = spaletArc.extract(Math.min(i, spaletArc.count - 1));
      const stagePal = decodePalette(palRaw);
      const mw = w * 6;
      const mh = h * 6;
      if (smlBlob && smlBlob.length >= mw * mh) {
        const rgba = Buffer.alloc(mw * mh * 4);
        for (let y = 0; y < mh; y++) {
          for (let x = 0; x < mw; x++) {
            const palIdx = smlBlob[y * mw + x];
            const [r, g, b] = stagePal[palIdx] || [0, 0, 0];
            const dst = (y * mw + x) * 4;
            rgba[dst] = r;
            rgba[dst + 1] = g;
            rgba[dst + 2] = b;
            rgba[dst + 3] = 255;
          }
        }
        const png = writePngRgba(mw, mh, rgba);
        fs.writeFileSync(path.join(OUT_DIR, `minimaps/map_${i}.png`), png);
        hasMinimap = true;
      }
    }

    // Battlefield background Hm??.e5
    const hmName = `Hm${String(i).padStart(2, '0')}.e5`;
    const hmPath = path.join(KW_DIR, hmName);
    let hasHm = false;
    if (fs.existsSync(hmPath)) {
      const hmBuf = fs.readFileSync(hmPath);
      const TW = 96, TH = 24;
      const need = w * h * 2304;
      if (hmBuf.length >= need) {
        const palRaw = spaletArc.extract(Math.min(i, spaletArc.count - 1));
        const stagePal = decodePalette(palRaw);
        const imgW = w * TW;
        const imgH = h * TH;
        const rgba = Buffer.alloc(imgW * imgH * 4);

        for (let ti = 0; ti < w * h; ti++) {
          const tile = hmBuf.subarray(ti * 2304, (ti + 1) * 2304);
          const tx = (ti % w) * TW;
          const ty = Math.floor(ti / w) * TH;
          for (let y = 0; y < TH; y++) {
            for (let x = 0; x < TW; x++) {
              const palIdx = tile[y * TW + x];
              const [r, g, b] = stagePal[palIdx] || [0, 0, 0];
              const dst = ((ty + y) * imgW + (tx + x)) * 4;
              rgba[dst] = r;
              rgba[dst + 1] = g;
              rgba[dst + 2] = b;
              rgba[dst + 3] = 255;
            }
          }
        }
        const png = writePngRgba(imgW, imgH, rgba);
        fs.writeFileSync(path.join(OUT_DIR, `battlemaps/bg_${i}.png`), png);
        hasHm = true;
      }
    }

    mapsManifest.push({
      id: i,
      title,
      width: w,
      height: h,
      grid,
      hasMinimap,
      hasHm
    });
  }

  fs.writeFileSync(path.join(OUT_DIR, 'data/maps.json'), JSON.stringify(mapsManifest, null, 2));
  console.log(`Processed ${mapsManifest.length} battlefield maps.`);

  console.log('8. Copying Audio Assets (BGM & SFX)...');
  const soundTrkDir = path.join(KW_DIR, 'SoundTrk');
  const bgmList = [];
  if (fs.existsSync(soundTrkDir)) {
    const files = fs.readdirSync(soundTrkDir);
    for (const f of files) {
      if (f.endsWith('.mp3')) {
        const src = path.join(soundTrkDir, f);
        const dst = path.join(OUT_DIR, 'audio/bgm', f);
        fs.copyFileSync(src, dst);
        bgmList.push(f);
      }
    }
  }
  fs.writeFileSync(path.join(OUT_DIR, 'data/bgm_list.json'), JSON.stringify(bgmList, null, 2));

  const sfxList = [];
  const kwFiles = fs.readdirSync(KW_DIR);
  for (const f of kwFiles) {
    if (f.toLowerCase().endsWith('.wav')) {
      const src = path.join(KW_DIR, f);
      const dst = path.join(OUT_DIR, 'audio/sfx', f.toLowerCase());
      fs.copyFileSync(src, dst);
      sfxList.push(f.toLowerCase());
    }
  }
  fs.writeFileSync(path.join(OUT_DIR, 'data/sfx_list.json'), JSON.stringify(sfxList, null, 2));
  console.log(`Copied ${bgmList.length} BGM tracks and ${sfxList.length} SFX files.`);

  console.log('=== All kw Assets Successfully Extracted and Processed! ===');
}

runExtraction().catch(err => {
  console.error('Extraction failed:', err);
  process.exit(1);
});
