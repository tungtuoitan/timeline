/*
**Fullpath:** /api-tests/_cases/_TEMPLATE.js
*/
'use strict';

/**
 * TEMPLATE cho 1 file test. Copy vào `flows/<flow#>-<flow-name>/<flow#>-<group#>-<slug>.test.js`
 * (BẮT BUỘC đuôi `.test.js` — file này không có đuôi đó nên không bị chạy nhầm).
 *
 * - 1 file = 1 NHÓM case liên quan (cùng hành vi / cùng setup), không bắt buộc 1 case 1 file.
 * - Tên test bắt đầu bằng `Flow<NN>#<case>:` khớp đúng cột `#` trong `testcase.md` cùng thư mục,
 *   và thêm `<file>` vào cột "Test" của dòng đó.
 * - Trước khi viết: đọc JSDoc của hàm sẽ dùng trong `_lib/<domain>.js` (Preconditions/Side-effects).
 * - Assert qua API + `_lib/verify.js`, không đọc DB trực tiếp.
 * - Chỉ tự động hoá hành vi đáng giá (nhiều bước, nhiều nhánh, dễ hồi quy âm thầm, rule bảo mật).
 *   Thứ test tay 5 giây là thấy thì không cần.
 * - Kết luận kỹ thuật (vì sao assert thế này) ghi ngắn trong comment; diễn biến điều tra để ở
 *   devlog/commit message.
 *
 * Chạy: `node run-tests.js <flow#>` hoặc `node --test flows/<flow>/<file>.test.js`.
 */

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const dm = require('../../_lib/dataManager');
const { ok } = require('../../_lib/verify');

let a; // session = user test riêng của file này

before(async () => {
  a = await dm.newSession('<flow#>-<group#>');
  // setup chung cho các case trong file
});

after(async () => {
  await a?.cleanup();
});

test('Flow<NN>#<case>: <mô tả ngắn, copy từ testcase.md>', async () => {
  const p = await a.createProject();
  const [got] = ok(await a.getProjectRes(p.id));
  assert.equal(got.id, p.id);
});
