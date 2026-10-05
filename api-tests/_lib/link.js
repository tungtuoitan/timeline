/*
**Fullpath:** /api-tests/_lib/link.js
*/
'use strict';

/**
 * link.js — link của task/project + sửa file (LinkController -> LinkService, task #1477).
 * 1 link = 1 dòng dbo.files (mimeType `text/x-uri`, url ngoài) nằm trong workspace của project:
 *   - link task: trong folder của task (`task.folderWorkspaceItemId`, BE tự tạo nếu chưa có),
 *     gắn task qua `pro.task_workspace_item`;
 *   - link project: trong folder `Links` ở gốc workspace project (BE tự tạo).
 * Mỗi phần tử list: `{workspaceItemId, workspaceId, parentId, entityType, entityId, name, url,
 *   mimeType, isLink, taskWorkspaceItemId, createdAt}`.
 */

/** `GET /api/task/{id}/links` — item gắn qua task_workspace_item + link nằm trong folder task. */
function getTaskLinks(s, taskId) {
  return s.call('GET', `/api/task/${taskId}/links`);
}

/**
 * `POST /api/task/{id}/links` — body `{url, name?}` (tạo link mới) hoặc `{workspaceItemId}`
 * (gắn item có sẵn cùng workspace). Thêm trùng = không tạo dòng mới.
 * Side-effects: có thể tạo folder task (set task.folderWorkspaceItemId) + file + workspace item.
 */
function addTaskLink(s, taskId, body) {
  return s.call('POST', `/api/task/${taskId}/links`, body);
}

/** `DELETE /api/task/{id}/links/{workspaceItemId}` — bỏ gắn; link nằm trong folder task bị soft delete. */
function removeTaskLink(s, taskId, workspaceItemId) {
  return s.call('DELETE', `/api/task/${taskId}/links/${workspaceItemId}`);
}

/** `GET /api/project/{id}/links` — link trong folder `Links` của workspace project. */
function getProjectLinks(s, projectId) {
  return s.call('GET', `/api/project/${projectId}/links`);
}

/** `POST /api/project/{id}/links` — body `{url, name?}`. */
function addProjectLink(s, projectId, body) {
  return s.call('POST', `/api/project/${projectId}/links`, body);
}

/** `DELETE /api/project/{id}/links/{workspaceItemId}` — soft delete link. */
function removeProjectLink(s, projectId, workspaceItemId) {
  return s.call('DELETE', `/api/project/${projectId}/links/${workspaceItemId}`);
}

/** `PATCH /api/file/{id}` — body `{name?, url?}`; url chỉ đổi được với link. */
function updateFile(s, fileId, body) {
  return s.call('PATCH', `/api/file/${fileId}`, body);
}

module.exports = {
  getTaskLinks, addTaskLink, removeTaskLink, getProjectLinks, addProjectLink, removeProjectLink, updateFile,
};
