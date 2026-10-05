/*
**Fullpath:** /api-tests/_lib/verify.js
*/
'use strict';

/**
 * verify.js — assert dùng chung. Dùng lại các hàm này thay vì viết lại trong case.
 */

const assert = require('node:assert/strict');

/** ISO 8601 có offset hoặc Z — mọi instant BE trả ra phải khớp (task #1450, không còn Fake UTC). */
const ISO_WITH_OFFSET = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?([+-]\d{2}:\d{2}|Z)$/;

/** Response phải thành công (HTTP 200 + body.success). @returns body.data */
function ok(res, label = '') {
  if (res.http !== 200 || !res.body?.success) {
    throw new assert.AssertionError({ message: `${label} expected success, got HTTP ${res.http}: ${JSON.stringify(res.body)}` });
  }
  return res.body.data;
}

/** Lỗi nghiệp vụ: HTTP 200 nhưng `success=false` và `body.status` = mong đợi (+ message khớp regex). */
function assertFail(res, status, messageRe, label = '') {
  assert.equal(res.body?.success, false, `${label}: phải thất bại, thực tế HTTP ${res.http} ${JSON.stringify(res.body)}`);
  assert.equal(res.body?.status, status, `${label}: body.status phải là ${status}`);
  if (messageRe) assert.match(res.body?.message ?? '', messageRe, `${label}: message`);
}

/** Bị OwnershipGuard chặn: body.status 404 "... not found or access denied" (không lộ việc row tồn tại). */
function assertDenied(res, label = '') {
  assertFail(res, 404, /not found/i, label);
}

/** Lỗi model validation / JSON converter: HTTP 400. */
function assertHttp400(res, label = '') {
  assert.equal(res.http, 400, `${label}: phải HTTP 400, thực tế ${res.http} ${JSON.stringify(res.body)}`);
}

/** So sánh danh sách id không quan tâm thứ tự. */
function assertSameIds(actual, expected, label = '') {
  assert.deepEqual([...actual].sort((x, y) => x - y), [...expected].sort((x, y) => x - y), label);
}

module.exports = { ISO_WITH_OFFSET, ok, assertFail, assertDenied, assertHttp400, assertSameIds };
