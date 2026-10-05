/*
**Fullpath:** /api-tests/run-tests.js
*/
'use strict';

/**
 * run-tests.js — chạy các case `flows/**\/*.test.js` rồi GHI KẾT QUẢ vào `RESULTS.md`.
 *
 * RESULTS.md là nơi DUY NHẤT ghi trạng thái pass/fail (sinh từ lần chạy thật, không tick tay) —
 * testcase.md chỉ mô tả case + kỳ vọng + file test, không có cột Pass để khỏi lệch nhau.
 *
 * Dùng:
 *   node run-tests.js            # chạy tất cả
 *   node run-tests.js 03         # chỉ file có đường dẫn chứa "03" (flow 03, hoặc case 03-x)
 *   node run-tests.js 05-3 06    # nhiều bộ lọc (OR)
 * Chỉ khi chạy KHÔNG lọc mới ghi đè RESULTS.md (kết quả 1 phần không thay cho cả bộ).
 *
 * Tự liệt kê FILE rồi truyền cho `node --test` (truyền thư mục không chạy được); tuần tự
 * (`--test-concurrency=1`) để không chạm rate limit signup.
 */

const fs = require('node:fs');
const path = require('node:path');
const { spawnSync } = require('node:child_process');
const { BASE_URL } = require('./_lib/config');

const ROOT = __dirname;
const FLOWS = path.join(ROOT, 'flows');
const JUNIT = path.join(ROOT, '.state', 'junit.xml');

function findTestFiles(dir) {
  let out = [];
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, e.name);
    if (e.isDirectory()) out = out.concat(findTestFiles(full));
    else if (e.name.endsWith('.test.js')) out.push(full);
  }
  return out.sort();
}

const filters = process.argv.slice(2);
const rel = (f) => path.relative(ROOT, f).replace(/\\/g, '/');
const files = findTestFiles(FLOWS).filter((f) => !filters.length || filters.some((k) => rel(f).includes(k.replace(/\\/g, '/'))));
if (!files.length) {
  console.error(`Không có file test nào khớp: ${filters.join(', ')}`);
  process.exit(1);
}

fs.mkdirSync(path.dirname(JUNIT), { recursive: true });
const run = spawnSync(process.execPath, [
  '--test', '--test-concurrency=1',
  '--test-reporter=spec', '--test-reporter-destination=stdout',
  '--test-reporter=junit', `--test-reporter-destination=${JUNIT}`,
  ...files,
], { cwd: ROOT, stdio: 'inherit' });

if (!filters.length && fs.existsSync(JUNIT)) writeResults(fs.readFileSync(JUNIT, 'utf8'));
process.exit(run.status ?? 1);

function unescapeXml(s) {
  return s.replace(/&quot;/g, '"').replace(/&apos;/g, "'").replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&amp;/g, '&');
}

function writeResults(xml) {
  const rows = [];
  // Attribute có thể chứa '>' (tên test) -> đọc theo cặp name="..." thay vì [^>]*.
  const re = /<testcase((?:\s+[\w:-]+="[^"]*")*)\s*(\/>|>([\s\S]*?)<\/testcase>)/g;
  let m;
  while ((m = re.exec(xml))) {
    const attrs = m[1];
    const name = unescapeXml((attrs.match(/\bname="([^"]*)"/) || [])[1] || '?');
    const file = (attrs.match(/\bfile="([^"]*)"/) || [])[1];
    const failure = (m[3] || '').match(/<failure\b[^>]*message="([^"]*)"/);
    rows.push({
      name,
      file: file ? path.relative(ROOT, unescapeXml(file)).replace(/\\/g, '/') : '',
      ok: !/<failure\b/.test(m[3] || ''),
      error: failure ? unescapeXml(failure[1]).split('\n')[0].slice(0, 200) : '',
    });
  }
  const pass = rows.filter((r) => r.ok).length;
  const lines = [
    '# Kết quả chạy api-tests',
    '',
    '**Fullpath:** /api-tests/RESULTS.md',
    '',
    '> File SINH TỰ ĐỘNG bởi `node run-tests.js` (chạy cả bộ) — không sửa tay.',
    '',
    `- Thời điểm: ${new Date().toISOString()}`,
    `- BE: ${BASE_URL}`,
    `- Kết quả: **${pass}/${rows.length} pass**`,
    '',
    '| Kết quả | Case | File | Lỗi |',
    '|---|---|---|---|',
    ...rows.map((r) => `| ${r.ok ? '✅' : '❌'} | ${r.name.replace(/\|/g, '\\|')} | ${r.file} | ${r.error.replace(/\|/g, '\\|')} |`),
    '',
  ];
  fs.writeFileSync(path.join(ROOT, 'RESULTS.md'), lines.join('\n'), 'utf8');
  console.log(`\nĐã ghi RESULTS.md: ${pass}/${rows.length} pass`);
}
