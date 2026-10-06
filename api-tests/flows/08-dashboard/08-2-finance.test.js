/*
**Fullpath:** /api-tests/flows/08-dashboard/08-2-finance.test.js
*/
'use strict';

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const dm = require('../../_lib/dataManager');
const { ok, assertHttp400 } = require('../../_lib/verify');

let a;
let b;
let asset;
let tokenA; // vé mở khoá TOTP (#1489) — summary là API riêng tư
let tokenB;
const run = Date.now().toString(36);
const ext = (k) => `apitest-${run}-${k}`;
/** Trả `body.object` sau khi kiểm tra thành công (summary nằm ở object, không phải data). */
const obj = (res) => {
  ok(res);
  return res.body.object;
};
const tx = (k, account, at, assetName, amount, kind, extra = {}) => ({
  account, occurredAt: `${at}+07:00`, asset: assetName, amount, kind, source: 'import', externalId: ext(k), ...extra,
});

before(async () => {
  a = await dm.newSession('08-2a');
  b = await dm.newSession('08-2b');
  asset = a.testAsset();
  tokenA = (await a.enableTotp()).token;
  tokenB = (await b.enableTotp()).token;
});

after(async () => {
  const rows = (await a?.listFinTx('?limit=5000'))?.body?.data ?? [];
  a?.trackFinTx(rows.map((r) => r.id));
  await a?.cleanup();
  await b?.cleanup();
});

const ledger = () => [
  tx('open', 'BIDV', '2026-09-01T08:00:00', 'VND', 100000, 'adjust'),
  tx('p2p-bank', 'BIDV', '2026-09-01T12:00:00', 'VND', -10000, 'invest', { groupKey: `p2p:${run}` }),
  tx('p2p-coin', 'Binance', '2026-09-01T12:00:00', asset, 10, 'invest', { groupKey: `p2p:${run}`, valueVnd: 10000 }),
  tx('eat', 'BIDV', '2026-09-02T12:00:00', 'VND', -5000, 'expense'),
  tx('salary', 'BIDV', '2026-09-03T09:00:00', 'VND', 7000, 'income'),
  tx('lend', 'BIDV', '2026-09-03T10:00:00', 'VND', -3000, 'debt', { counterparty: 'Nam' }),
];

test('Flow08#4: POST lô giao dịch -> chèn hết; gửi lại y nguyên -> unchanged, không nhân đôi', async () => {
  const first = await a.postFinTx(ledger());
  ok(first);
  assert.deepEqual(first.body.object, { inserted: 6, updated: 0, unchanged: 0 });

  const again = await a.postFinTx(ledger());
  ok(again);
  assert.deepEqual(again.body.object, { inserted: 0, updated: 0, unchanged: 6 });
  assert.equal(ok(await a.listFinTx()).length, 6);
});

test('Flow08#4.1: sửa tay category rồi nguồn gửi lại số mới -> cập nhật số, giữ category', async () => {
  const eat = ok(await a.listFinTx('?kind=expense'))[0];
  ok(await a.patchFinTx(eat.id, { category: 'Ăn uống', note: 'phở' }));

  const changed = ledger().map((r) => (r.externalId === ext('eat') ? { ...r, amount: -5500, kind: 'income' } : r));
  const res = await a.postFinTx(changed);
  assert.deepEqual(res.body.object, { inserted: 0, updated: 1, unchanged: 5 });

  const after1 = ok(await a.listFinTx('?kind=expense'))[0];
  assert.equal(after1.amount, -5500);
  assert.equal(after1.kind, 'expense', 'kind do người sửa/nguồn đầu tiên đặt, không bị ghi đè');
  assert.equal(after1.category, 'Ăn uống');
  assert.equal(after1.note, 'phở');
  ok(await a.postFinTx(ledger())); // trả số về -5000 cho các case sau
});

test('Flow08#4.2: dữ liệu sai -> HTTP 400, báo đúng dòng', async () => {
  const bad = await a.postFinTx([tx('x1', 'BIDV', '2026-09-01T08:00:00', 'VND', 1, 'income'), tx('x2', 'BIDV', '2026-09-01T08:00:00', 'VND', 1, 'spend')]);
  assertHttp400(bad);
  assert.match(bad.body.message, /transactions\[1\]/);
  assertHttp400(await a.postFinTx([tx('x3', 'BIDV', '2026-09-01T08:00:00', 'VND', 0, 'income')]));
  assertHttp400(await a.postFinTx([{ ...tx('x4', 'BIDV', '2026-09-01T08:00:00', 'VND', 1, 'income'), occurredAt: '2026-09-01T08:00:00' }]));
  assertHttp400(await a.postFinTx([]));
  assert.equal(ok(await a.listFinTx()).length, 6, 'lô lỗi không chèn dòng nào');
});

test('Flow08#5: GET lọc kind / account / uncategorized / from-to', async () => {
  assert.equal(ok(await a.listFinTx('?account=Binance')).length, 1);
  assert.deepEqual(ok(await a.listFinTx('?uncategorized=true')).map((r) => r.kind), ['income']);
  const day3 = ok(await a.listFinTx('?from=2026-09-03&to=2026-09-03'));
  assert.deepEqual(day3.map((r) => r.kind), ['debt', 'income'], 'mới nhất trước');
  assertHttp400(await a.listFinTx('?from=hôm-qua'));
  assertHttp400(await a.listFinTx('?kind=spend'));
});

test('Flow08#6: summary — tài sản ròng theo ngày (giá carry-forward), tháng tách chi tiêu/đầu tư', async () => {
  ok(await a.putFinPrices([
    { date: '2026-09-01', asset, quote: 'VND', price: 1000, source: 'test' },
    { date: '2026-09-03', asset, quote: 'VND', price: 2000, source: 'test' },
  ]));
  const s = obj(await a.getFinSummary('?from=2026-09-01&to=2026-09-03', tokenA));
  assert.deepEqual(s.series.map((p) => p.totalVnd), [100000, 95000, 112000]);
  assert.deepEqual(s.series[2].accounts, { BIDV: 89000, Binance: 20000 });
  assert.equal(s.series[2].debtVnd, 3000, 'tiền cho mượn vẫn là tài sản');
  assert.equal(s.months.length, 1);
  const m = s.months[0];
  assert.equal(m.month, '2026-09-01');
  assert.equal(m.incomeVnd, 7000);
  assert.equal(m.expenseVnd, 5000, 'mua coin (invest) và cho mượn (debt) không tính chi tiêu');
  assert.equal(m.investedVnd, 10000);
  assert.equal(m.uncategorized, 1);
  assert.equal(s.investedVnd, 10000);
  assert.deepEqual(s.holdings.map((h) => [h.account, h.asset, h.valueVnd]), [['BIDV', 'VND', 89000], ['Binance', asset, 20000]]);
  assert.deepEqual(s.missingPrices, []);
});

test('Flow08#6.1: summary interval=week / tham số sai -> HTTP 400', async () => {
  const s = obj(await a.getFinSummary('?from=2026-09-01&to=2026-09-13&interval=week', tokenA));
  assert.deepEqual(s.series.map((p) => p.date), ['2026-09-06', '2026-09-13'], 'điểm cuối tuần (CN)');
  assertHttp400(await a.getFinSummary('?interval=year', tokenA));
  assertHttp400(await a.getFinSummary('?from=2026-09-05&to=2026-09-01', tokenA));
});

test('Flow08#7: giá — latest theo (asset, quote); giá <= 0 -> HTTP 400', async () => {
  const latest = ok(await a.getFinPricesLatest());
  assert.deepEqual(latest.find((r) => r.asset === asset), { asset, quote: 'VND', date: '2026-09-03' });
  assertHttp400(await a.putFinPrices([{ date: '2026-09-04', asset, quote: 'VND', price: 0, source: 'test' }]));
});

test('Flow08#8: user B không thấy / không sửa / không xoá được giao dịch của A', async () => {
  const id = ok(await a.listFinTx())[0].id;
  assert.deepEqual(ok(await b.listFinTx()), []);
  assert.equal((await b.patchFinTx(id, { category: 'hack' })).http, 404);
  assert.equal((await b.deleteFinTx(id)).http, 404);
  const s = obj(await b.getFinSummary('?from=2026-09-01&to=2026-09-03', tokenB));
  assert.deepEqual(s.holdings, []);
  assert.ok(s.series.every((p) => p.totalVnd === 0));
});

test('Flow08#9: DELETE -> biến mất khỏi list; xoá lại -> HTTP 404', async () => {
  const lend = ok(await a.listFinTx('?kind=debt'))[0];
  ok(await a.deleteFinTx(lend.id));
  assert.equal(ok(await a.listFinTx('?kind=debt')).length, 0);
  assert.equal((await a.deleteFinTx(lend.id)).http, 404);
});
