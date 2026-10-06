/*
**Fullpath:** /api-tests/_lib/http.js
*/
'use strict';

const { BASE_URL } = require('./config');

/**
 * Gọi 1 endpoint của BE.
 *
 * @param {string} method  GET/POST/PATCH/DELETE
 * @param {string} path    vd `/api/task?ids=1`
 * @param {object} [opts]
 * @param {string} [opts.token]  JWT (Bearer). Không có -> request ẩn danh.
 * @param {*}      [opts.body]   JSON body.
 * @param {object} [opts.form]   form-urlencoded body (auth/signup dùng [FromForm]).
 * @param {object} [opts.headers] header thêm (vd `X-Unlock-Token`, #1489).
 * @returns {Promise<{http:number, body:any}>} `http` = HTTP status; `body` = JSON đã parse
 *   (thường là ResultOptions `{success, message, data, status}`).
 *
 * ⚠️ BE trả HTTP 200 cho phần lớn lỗi nghiệp vụ, lỗi thật nằm ở `body.status` (400/403/404).
 *   Chỉ model validation / JSON converter (thiếu field bắt buộc, ngày không offset...) mới ra
 *   HTTP 400, và PATCH task không tìm thấy mới ra HTTP 404. Assert đúng tầng.
 */
async function request(method, path, { token, body, form, headers: extra } = {}) {
  const headers = { ...extra };
  if (token) headers.Authorization = `Bearer ${token}`;
  let payload;
  if (form) {
    payload = new URLSearchParams(form);
  } else if (body !== undefined) {
    headers['Content-Type'] = 'application/json';
    payload = JSON.stringify(body);
  }
  const res = await fetch(`${BASE_URL}${path}`, { method, headers, body: payload });
  const text = await res.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text.slice(0, 500) }; }
  return { http: res.status, body: json };
}

module.exports = { request };
