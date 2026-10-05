/*
**Fullpath:** /api-tests/_lib/api.js
*/
'use strict';

// HTTP helper cho API test (task SuperApp #1471). Gọi BE thật (mặc định http://localhost:5000 —
// BE local đang trỏ SuperApp-dev), KHÔNG đụng SQL trực tiếp: mọi assert đi qua API như FE/script.
//
// Mỗi lần chạy tự signup user test mới (`apitest+<run>-<tag>@test.local`, password ngẫu nhiên chỉ
// nằm trong RAM) -> data test cô lập hoàn toàn với data thật của Tung, không cần login Google.
// Cleanup: hard-delete task đã tạo + soft-delete project (API chưa có hard-delete project).

const crypto = require('node:crypto');

const BASE_URL = (process.env.SA_API_URL || 'http://localhost:5000').replace(/\/$/, '');
const RUN_ID = `${Date.now().toString(36)}${crypto.randomBytes(2).toString('hex')}`;

if (!/^https?:\/\/(localhost|127\.0\.0\.1)(:\d+)?$/.test(BASE_URL) && process.env.SA_API_ALLOW_REMOTE !== '1') {
  // Chặn lỡ tay chạy vào prod (tungle.uk) — signup + tạo data rác trên prod.
  throw new Error(`api-tests chỉ chạy với BE local (dev DB). SA_API_URL=${BASE_URL} — set SA_API_ALLOW_REMOTE=1 nếu thật sự muốn.`);
}

async function request(method, path, { token, body, form } = {}) {
  const headers = {};
  if (token) headers.Authorization = `Bearer ${token}`;
  let payload;
  if (form) {
    payload = new URLSearchParams(form);
  } else if (body !== undefined) {
    headers['Content-Type'] = 'application/json';
    payload = typeof body === 'string' ? body : JSON.stringify(body);
  }
  const res = await fetch(`${BASE_URL}${path}`, { method, headers, body: payload });
  const text = await res.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text.slice(0, 500) }; }
  return { http: res.status, body: json };
}

/** Signup 1 user test mới, trả client đã gắn token. */
async function createUser(tag) {
  const email = `apitest+${RUN_ID}-${tag}@test.local`;
  const password = `${crypto.randomBytes(12).toString('base64url')}A1!`;
  const res = await request('POST', '/api/auth/signup', { form: { email, password } });
  const token = res.body?.user?.token;
  if (res.http !== 200 || !token) {
    throw new Error(`Signup ${email} thất bại: HTTP ${res.http} ${JSON.stringify(res.body)}`);
  }
  return new Client({ id: res.body.user.id, email, token });
}

class Client {
  constructor({ id, email, token }) {
    this.id = id;
    this.email = email;
    this.token = token;
    this.createdTaskIds = new Set();
    this.createdProjects = new Map(); // id -> last known project (để soft-delete lúc cleanup)
  }

  call(method, path, body) {
    return request(method, path, { token: this.token, body });
  }

  // ---------------- project
  async upsertProjects(items) {
    const res = await this.call('POST', '/api/project', items);
    for (const p of res.body?.data ?? []) if (p?.id) this.createdProjects.set(p.id, p);
    return res;
  }

  /** Tạo 1 project, assert thành công, trả object project. */
  async createProject(overrides = {}) {
    const res = await this.upsertProjects([{ id: 0, name: `APITEST ${RUN_ID} project`, status: 'active', ...overrides }]);
    if (!res.body?.success) throw new Error(`createProject fail: ${JSON.stringify(res.body)}`);
    return res.body.data[0];
  }

  getProjects(query = '') { return this.call('GET', `/api/project${query}`); }
  getProject(id) { return this.call('GET', `/api/project/${id}`); }

  // ---------------- task
  async upsertTasks(items) {
    const res = await this.call('POST', '/api/task', items);
    for (const t of res.body?.data ?? []) if (t?.id) this.createdTaskIds.add(t.id);
    return res;
  }

  async createTask(projectId, overrides = {}) {
    const res = await this.upsertTasks([{ id: 0, projectId, title: `APITEST ${RUN_ID} task`, ...overrides }]);
    if (!res.body?.success) throw new Error(`createTask fail: ${JSON.stringify(res.body)}`);
    return res.body.data[0];
  }

  getTasks(query = '') { return this.call('GET', `/api/task${query}`); }

  /** GET /api/task/{id} -> task hoặc undefined. */
  async getTask(id) {
    const res = await this.call('GET', `/api/task/${id}`);
    return res.body?.data?.[0];
  }

  patchTask(id, body) { return this.call('PATCH', `/api/task/${id}`, body); }
  hardDeleteTasks(ids) { return this.call('DELETE', '/api/task', { ids }); }

  // ---------------- comment
  getComments(taskId, type) {
    const q = type ? `&type=${encodeURIComponent(type)}` : '';
    return this.call('GET', `/api/taskcomment?taskId=${taskId}${q}`);
  }
  upsertComment(body) { return this.call('POST', '/api/taskcomment', body); }
  deleteComment(id) { return this.call('DELETE', `/api/taskcomment/${id}`); }

  // ---------------- cleanup
  async cleanup() {
    const ids = [...this.createdTaskIds];
    for (let i = 0; i < ids.length; i += 200) {
      await this.hardDeleteTasks(ids.slice(i, i + 200)).catch(() => {});
    }
    const now = new Date().toISOString();
    const projects = [...this.createdProjects.values()].map((p) => ({
      id: p.id, name: p.name || 'APITEST', status: p.status || 'active', workspaceId: p.workspaceId ?? undefined, deletedAt: now,
    }));
    if (projects.length) await this.call('POST', '/api/project', projects).catch(() => {});
  }
}

/** Assert body ResultOptions thành công, trả data. */
function ok(res, label = '') {
  if (res.http !== 200 || !res.body?.success) {
    throw new Error(`${label} expected success, got HTTP ${res.http}: ${JSON.stringify(res.body)}`);
  }
  return res.body.data;
}

module.exports = { BASE_URL, RUN_ID, request, createUser, ok };
