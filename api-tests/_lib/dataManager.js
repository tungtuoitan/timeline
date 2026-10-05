/*
**Fullpath:** /api-tests/_lib/dataManager.js
*/
'use strict';

/**
 * dataManager.js — FACADE: gộp các module domain thành 1 entry point. KHÔNG thêm logic ở đây.
 *   - user/đăng nhập        -> auth.js
 *   - project               -> project.js
 *   - task (upsert/patch/…) -> task.js
 *   - comment               -> comment.js
 *   - dọn data              -> cleanupData.js
 *   - Domain MỚI (vd workspace, keyword): tạo file mới trong _lib/, thêm vào DOMAINS bên dưới.
 * JSDoc (Preconditions / Side-effects) nằm ở từng module — đọc ở đó trước khi viết case.
 *
 * Dùng trong case:
 *   const dm = require('../../_lib/dataManager');
 *   const a = await dm.newSession('p01');      // signup user test mới
 *   const p = await a.createProject();
 *   ...
 *   await a.cleanup();                          // trong after()
 *
 * Nguyên tắc: mọi thao tác đi qua API thật (như FE / superapp.py), assert cũng qua API — không
 * đọc DB trực tiếp, để test không dính schema.
 */

const { request } = require('./http');
const { signupUser } = require('./auth');
const { cleanupSession } = require('./cleanupData');

const DOMAINS = [require('./project'), require('./task'), require('./comment')];

/**
 * Tạo session = 1 user test mới + mọi hàm domain đã bind sẵn user đó.
 * @param {string} tag  hậu tố email để dễ nhận ra file nào tạo (vd 'p01-1').
 * @returns {Promise<object>} `{ id, email, token, tracked, call, cleanup, ...domainFns }`
 */
async function newSession(tag) {
  const user = await signupUser(tag);
  const s = {
    ...user,
    tracked: { projects: new Map(), tasks: new Set() },
    call: (method, path, body) => request(method, path, { token: user.token, body }),
    cleanup: () => cleanupSession(s),
  };
  for (const mod of DOMAINS) {
    for (const [name, fn] of Object.entries(mod)) s[name] = (...args) => fn(s, ...args);
  }
  return s;
}

module.exports = { newSession, request };
