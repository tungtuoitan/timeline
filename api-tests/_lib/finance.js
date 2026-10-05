/*
**Fullpath:** /api-tests/_lib/finance.js
*/
'use strict';

/**
 * finance.js — sổ giao dịch đa tài sản + cache giá + tổng hợp (TungRoot #1482, FinanceController):
 *   - `fin_transaction`: 1 dòng = 1 thay đổi số dư của 1 asset (VND, USDT, BTC…) ở 1 account, amount có dấu.
 *   - `fin_price_cache`: giá đóng cửa theo ngày, DÙNG CHUNG mọi user → test chỉ dùng asset `TST_*`
 *     (SQL dọn định kỳ xoá theo prefix này).
 * Ngày theo timezone user (mặc định Asia/Ho_Chi_Minh).
 */

/** Asset giá test duy nhất cho mỗi lần gọi, để các lần chạy không đè giá của nhau. */
function testAsset() {
  return `TST_${Date.now().toString(36).toUpperCase()}${Math.floor(Math.random() * 1e4)}`;
}

/**
 * `POST /api/finance/transactions` `{ transactions: [...] }` — chèn theo lô; (source, externalId) đã có
 * → chỉ cập nhật dữ kiện nguồn, giữ kind/category/note sửa tay. Side-effects: KHÔNG tự track id (API
 * không trả id) — case muốn dọn thì gọi `listFinTx` rồi `trackFinTx`.
 */
function postFinTx(s, transactions) {
  return s.call('POST', '/api/finance/transactions', { transactions });
}

/** `GET /api/finance/transactions?from&to&account&kind&uncategorized&limit` — mới nhất trước. */
function listFinTx(s, query = '') {
  return s.call('GET', `/api/finance/transactions${query}`);
}

/** Ghi nhớ id giao dịch để cleanup soft delete. */
function trackFinTx(s, ids) {
  for (const id of ids) (s.tracked.finTransactions ??= new Set()).add(id);
}

/** `PATCH /api/finance/transactions/{id}` — phân loại / ghi chú; "" xoá trường chữ. */
function patchFinTx(s, id, body) {
  return s.call('PATCH', `/api/finance/transactions/${id}`, body);
}

/** `DELETE /api/finance/transactions/{id}` — soft delete; không có / của user khác → HTTP 404. */
function deleteFinTx(s, id) {
  return s.call('DELETE', `/api/finance/transactions/${id}`);
}

/** `PUT /api/finance/prices` `{ prices: [{date, asset, quote, price, source}] }` — upsert (khoá asset+quote+date). */
function putFinPrices(s, prices) {
  return s.call('PUT', '/api/finance/prices', { prices });
}

/** `GET /api/finance/prices/latest` — ngày giá mới nhất theo (asset, quote). */
function getFinPricesLatest(s) {
  return s.call('GET', '/api/finance/prices/latest');
}

/** `GET /api/finance/summary?from&to&interval` — series tài sản ròng, tháng, holdings (VND). */
function getFinSummary(s, query = '') {
  return s.call('GET', `/api/finance/summary${query}`);
}

module.exports = { testAsset, postFinTx, listFinTx, trackFinTx, patchFinTx, deleteFinTx, putFinPrices, getFinPricesLatest, getFinSummary };
