/*
**Fullpath:** /api-tests/flows/07-links/07-2-project-links-file.test.js
*/
'use strict';

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const dm = require('../../_lib/dataManager');
const { ok, assertFail } = require('../../_lib/verify');

let a;
let project;

before(async () => {
  a = await dm.newSession('07-2');
  project = await a.createProject();
});

after(async () => {
  await a?.cleanup();
});

test('Flow07#8: đổi tên + url link qua PATCH /api/file/{id}', async () => {
  const task = await a.createTask(project.id);
  const [l] = ok(await a.addTaskLink(task.id, { url: 'https://github.com/a/b', name: 'Old' }));

  const upd = await a.updateFile(l.entityId, { name: 'New name', url: 'https://github.com/a/c' });
  ok(upd);
  const updated = upd.body.object;
  assert.equal(updated.name, 'New name');
  assert.equal(updated.url, 'https://github.com/a/c');

  const [again] = ok(await a.getTaskLinks(task.id));
  assert.equal(again.name, 'New name');
  const item = (await a.liveItems(project.workspaceId)).find((i) => i.id === l.workspaceItemId);
  assert.equal(item.data.name, 'New name', 'tree thấy tên mới ngay');
  assert.equal(item.data.url, 'https://github.com/a/c');

  assertFail(await a.updateFile(l.entityId, { url: 'javascript:alert(1)' }), 400, /http/i);
  assertFail(await a.updateFile(l.entityId, { name: '   ' }), 400, /Name/);
  assertFail(await a.updateFile(l.entityId, {}), 400);

  // file thường (không phải link): đổi tên được, đổi url thì không
  const file = ok(await a.batchItems(project.workspaceId, [{
    action: 'create', entityType: 4, parentId: null,
    fileData: { name: 'plain.pdf', url: 'https://drive.google.com/x', mimeType: 'application/pdf' },
  }]))[0];
  ok(await a.updateFile(file.entityId, { name: 'renamed.pdf' }));
  assertFail(await a.updateFile(file.entityId, { url: 'https://example.com' }), 400, /link/i);
});

test('Flow07#9: link project nằm trong folder Links ở gốc workspace, xoá = soft delete', async () => {
  const [l1] = ok(await a.addProjectLink(project.id, { url: 'https://www.tungle.uk', name: 'Prod' }));
  const [l2] = ok(await a.addProjectLink(project.id, { url: 'https://github.com/tungtuoitan/timeline' }));
  assert.equal(l1.parentId, l2.parentId, 'cùng 1 folder Links');

  const items = await a.liveItems(project.workspaceId);
  const folder = items.find((i) => i.id === l1.parentId);
  assert.equal(folder.entityType, 2);
  assert.equal(folder.parentId, null, 'folder ở gốc');
  assert.equal(folder.data.name, 'Links');
  assert.equal(items.filter((i) => i.entityType === 2 && i.parentId === null && i.data?.name === 'Links').length, 1);

  const list = ok(await a.getProjectLinks(project.id));
  assert.deepEqual(list.map((l) => l.workspaceItemId), [l1.workspaceItemId, l2.workspaceItemId]);
  assert.equal(list[1].name, 'github.com/tungtuoitan/timeline');

  ok(await a.removeProjectLink(project.id, l1.workspaceItemId));
  const after = ok(await a.getProjectLinks(project.id)).map((l) => l.workspaceItemId);
  assert.deepEqual(after, [l2.workspaceItemId]);
  assert.ok(!(await a.liveItems(project.workspaceId)).some((i) => i.id === l1.workspaceItemId));
  assertFail(await a.removeProjectLink(project.id, l1.workspaceItemId), 404);
  assertFail(await a.addProjectLink(project.id, { url: 'mailto:a@b.c' }), 400, /http/i);
});

test('Flow07#10: hard delete task có link -> folder + link biến mất khỏi tree', async () => {
  const task = await a.createTask(project.id);
  const [l] = ok(await a.addTaskLink(task.id, { url: 'https://example.com/x' }));
  const folderId = l.parentId;
  ok(await a.hardDeleteTasks([task.id]));
  const ids = (await a.liveItems(project.workspaceId)).map((i) => i.id);
  assert.ok(!ids.includes(folderId), 'folder task đã xoá');
  assert.ok(!ids.includes(l.workspaceItemId), 'link đã xoá');
});
