/*
**Fullpath:** /api-tests/flows/07-links/07-3-ownership.test.js
*/
'use strict';

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const dm = require('../../_lib/dataManager');
const { ok, assertDenied } = require('../../_lib/verify');

let a;
let b;
let pa;
let ta;
let la; // link task của A
let lpa; // link project của A

before(async () => {
  a = await dm.newSession('07-3a');
  b = await dm.newSession('07-3b');
  pa = await a.createProject();
  ta = await a.createTask(pa.id);
  [la] = ok(await a.addTaskLink(ta.id, { url: 'https://example.com/a', name: 'A link' }));
  [lpa] = ok(await a.addProjectLink(pa.id, { url: 'https://example.com/pa' }));
});

after(async () => {
  await a?.cleanup();
  await b?.cleanup();
});

test('Flow07#11: B không đọc/thêm/xoá link, không sửa file, không batch vào workspace của A', async () => {
  assertDenied(await b.getTaskLinks(ta.id), 'đọc link task');
  assertDenied(await b.addTaskLink(ta.id, { url: 'https://evil.example' }), 'thêm link task');
  assertDenied(await b.removeTaskLink(ta.id, la.workspaceItemId), 'xoá link task');
  assertDenied(await b.getProjectLinks(pa.id), 'đọc link project');
  assertDenied(await b.addProjectLink(pa.id, { url: 'https://evil.example' }), 'thêm link project');
  assertDenied(await b.removeProjectLink(pa.id, lpa.workspaceItemId), 'xoá link project');
  assertDenied(await b.updateFile(la.entityId, { name: 'hacked' }), 'sửa file');

  // B gắn item của A vào task của B
  const pb = await b.createProject();
  const tb = await b.createTask(pb.id);
  const res = await b.addTaskLink(tb.id, { workspaceItemId: la.workspaceItemId });
  assert.equal(res.body?.success, false, 'không gắn được item workspace khác');

  // workspace batch / move-cross
  const batch = await b.batchItems(pa.workspaceId, [{
    action: 'create', entityType: 4, parentId: null,
    fileData: { name: 'x', url: 'https://evil.example', mimeType: 'text/x-uri' },
  }]);
  assert.equal(batch.http, 404, `batch vào workspace A: HTTP ${batch.http}`);
  const del = await b.batchItems(pb.workspaceId, [{ action: 'delete', id: la.workspaceItemId }]);
  assert.equal(del.http, 404, `xoá item của A qua workspace B: HTTP ${del.http}`);
  const move = await b.call('POST', `/api/workspace/${pb.workspaceId}/items/move-cross`, {
    itemIds: [la.workspaceItemId], targetWorkspaceId: pb.workspaceId, targetParentId: null,
  });
  assert.equal(move.http, 404, `move-cross item của A: HTTP ${move.http}`);

  // dữ liệu A không đổi
  const list = ok(await a.getTaskLinks(ta.id));
  assert.equal(list.length, 1);
  assert.equal(list[0].name, 'A link');
  assert.equal(ok(await a.getProjectLinks(pa.id)).length, 1);
});
