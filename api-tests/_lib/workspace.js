/*
**Fullpath:** /api-tests/_lib/workspace.js
*/
'use strict';

/**
 * workspace.js — `/api/workspace/{id}/...` (WorkspaceController). Mỗi project có 1 workspace
 * (project.workspaceId). Item: entityType 2 folder, 3 note, 4 file (link = file mimeType text/x-uri).
 */

/** `GET /api/workspace/{id}/tree/v2` -> raw response; `body.object.flatData` = mọi item (cả đã soft delete). */
function getTree(s, workspaceId) {
  return s.call('GET', `/api/workspace/${workspaceId}/tree/v2`);
}

/** flatData của tree, bỏ item đã soft delete. */
async function liveItems(s, workspaceId) {
  const res = await getTree(s, workspaceId);
  return (res.body?.object?.flatData ?? []).filter((i) => !i.deletedAt);
}

/**
 * `POST /api/workspace/{id}/items/batch` — action-based (create/add/move/updateFolder/delete/restore).
 * Preconditions: workspace + mọi item `id`/`parentId` trong lô thuộc user — không thì HTTP 404.
 */
function batchItems(s, workspaceId, items) {
  return s.call('POST', `/api/workspace/${workspaceId}/items/batch`, items);
}

module.exports = { getTree, liveItems, batchItems };
