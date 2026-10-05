/*
**Fullpath:** /api-tests/flows/07-links/07-1-task-links.test.js
*/
'use strict';

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const dm = require('../../_lib/dataManager');
const { ok, assertFail } = require('../../_lib/verify');

const LINK_MIME = 'text/x-uri';
let a;
let project;
let task;
let link; // link tạo ở case 1

before(async () => {
  a = await dm.newSession('07-1');
  project = await a.createProject();
  task = await a.createTask(project.id);
});

after(async () => {
  await a?.cleanup();
});

test('Flow07#1: thêm link vào task chưa có folder -> BE tạo folder, link nằm trong đó', async () => {
  assert.equal(task.folderWorkspaceItemId ?? null, null, 'task mới chưa có folder');
  const [added] = ok(await a.addTaskLink(task.id, { url: 'https://github.com/tungtuoitan/super-app', name: 'Repo FE' }));
  assert.equal(added.isLink, true);
  assert.equal(added.mimeType, LINK_MIME);
  assert.equal(added.name, 'Repo FE');
  assert.equal(added.url, 'https://github.com/tungtuoitan/super-app');
  assert.ok(added.taskWorkspaceItemId > 0, 'gắn qua task_workspace_item');

  const fresh = await a.getTask(task.id);
  assert.ok(fresh.folderWorkspaceItemId > 0, 'task có folder');
  assert.equal(added.parentId, fresh.folderWorkspaceItemId, 'link nằm trong folder task');
  task = fresh;
  link = added;

  const list = ok(await a.getTaskLinks(task.id));
  assert.deepEqual(list.map((l) => l.workspaceItemId), [added.workspaceItemId]);
});

test('Flow07#2: link hiện ngay trong tree/v2 (không đợi cache)', async () => {
  // gọi tree trước 1 lần để chắc cache đã có, rồi thêm link mới -> phải thấy ngay
  await a.getTree(project.workspaceId);
  const [second] = ok(await a.addTaskLink(task.id, { url: 'https://drive.google.com/file/d/abc/view', name: 'Spec' }));
  const items = await a.liveItems(project.workspaceId);
  const hit = items.find((i) => i.id === second.workspaceItemId);
  assert.ok(hit, 'link mới có trong tree');
  assert.equal(hit.entityType, 4);
  assert.equal(hit.parentId, task.folderWorkspaceItemId);
  assert.equal(hit.data.mimeType, LINK_MIME);
  assert.equal(hit.data.url, 'https://drive.google.com/file/d/abc/view');
});

test('Flow07#3: url không hợp lệ -> 400, không tạo gì', async () => {
  const before = ok(await a.getTaskLinks(task.id)).length;
  for (const url of ['javascript:alert(1)', '/relative/path', '', 'ftp://x.com/a']) {
    assertFail(await a.addTaskLink(task.id, { url, name: 'x' }), 400, /http/i, `url=${url}`);
  }
  // batch create của workspace cũng chặn
  const res = await a.batchItems(project.workspaceId, [{
    action: 'create', entityType: 4, parentId: task.folderWorkspaceItemId,
    fileData: { name: 'bad', url: 'javascript:alert(1)', mimeType: LINK_MIME },
  }]);
  assert.equal(res.body?.success, false, `batch phải fail: ${JSON.stringify(res.body)}`);
  assert.equal(ok(await a.getTaskLinks(task.id)).length, before);
});

test('Flow07#4: không truyền name -> name = host + path', async () => {
  const [l] = ok(await a.addTaskLink(task.id, { url: 'https://example.com/docs/page/' }));
  assert.equal(l.name, 'example.com/docs/page');
});

test('Flow07#5: gắn item có sẵn (note) của cùng workspace, gắn lại không trùng', async () => {
  const created = ok(await a.batchItems(project.workspaceId, [{
    action: 'create', entityType: 3, parentId: null, noteData: { name: 'APITEST note', description: '' },
  }]))[0];
  const [first] = ok(await a.addTaskLink(task.id, { workspaceItemId: created.id }));
  assert.equal(first.isLink, false);
  assert.equal(first.entityType, 3);
  assert.equal(first.name, 'APITEST note');
  ok(await a.addTaskLink(task.id, { workspaceItemId: created.id }));
  const list = ok(await a.getTaskLinks(task.id));
  assert.equal(list.filter((l) => l.workspaceItemId === created.id).length, 1, 'không trùng');

  // item của workspace khác (project khác cùng user) -> 400
  const other = await a.createProject();
  const otherNote = ok(await a.batchItems(other.workspaceId, [{
    action: 'create', entityType: 3, parentId: null, noteData: { name: 'other ws note', description: '' },
  }]))[0];
  assertFail(await a.addTaskLink(task.id, { workspaceItemId: otherNote.id }), 400, /workspace/i);
});

test('Flow07#6: link tạo bằng workspace batch trong folder task cũng có trong task links', async () => {
  const item = ok(await a.batchItems(project.workspaceId, [{
    action: 'create', entityType: 4, parentId: task.folderWorkspaceItemId,
    fileData: { name: 'From explorer', url: 'https://www.tungle.uk', mimeType: LINK_MIME, statusCode: 'active' },
  }]))[0];
  const hit = ok(await a.getTaskLinks(task.id)).find((l) => l.workspaceItemId === item.id);
  assert.ok(hit, 'có trong list');
  assert.equal(hit.isLink, true);
  assert.equal(hit.taskWorkspaceItemId, null);
});

test('Flow07#7: xoá link -> link trong folder bị soft delete, item có sẵn chỉ bỏ gắn', async () => {
  const list = ok(await a.getTaskLinks(task.id));
  const note = list.find((l) => l.entityType === 3);

  ok(await a.removeTaskLink(task.id, link.workspaceItemId));
  ok(await a.removeTaskLink(task.id, note.workspaceItemId));

  const after = ok(await a.getTaskLinks(task.id)).map((l) => l.workspaceItemId);
  assert.ok(!after.includes(link.workspaceItemId));
  assert.ok(!after.includes(note.workspaceItemId));

  const live = (await a.liveItems(project.workspaceId)).map((i) => i.id);
  assert.ok(!live.includes(link.workspaceItemId), 'link bị soft delete khỏi tree');
  assert.ok(live.includes(note.workspaceItemId), 'note vẫn còn trong tree');

  assertFail(await a.removeTaskLink(task.id, link.workspaceItemId), 404);
});
