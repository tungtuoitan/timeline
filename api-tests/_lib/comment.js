/*
**Fullpath:** /api-tests/_lib/comment.js
*/
'use strict';

/**
 * comment.js — `/api/taskcomment` (TaskCommentController -> TaskCommentService -> Repository).
 * Loại: comment | decision | devlog | track (TaskCommentTypes).
 */

/**
 * `GET /api/taskcomment?taskId=&type=` — type CSV, type lạ -> body.status 400. Task không thuộc
 * user -> body.status 404. Không trả comment đã xoá. Sắp theo `occurredAt ?? createdAt` tăng dần.
 */
function getComments(s, taskId, type) {
  const q = type ? `&type=${encodeURIComponent(type)}` : '';
  return s.call('GET', `/api/taskcomment?taskId=${taskId}${q}`);
}

/**
 * `POST /api/taskcomment` — `id: 0` tạo (type null -> "comment"), `id > 0` sửa (type/occurredAt
 * null = giữ nguyên). `occurredAt` PHẢI có offset/Z (không có -> HTTP 400).
 * Preconditions: task thuộc user; sửa thì comment phải thuộc user VÀ nằm trên đúng `taskId`;
 *   reply thì `parentCommentId` phải trên cùng task — sai -> body.status 404.
 */
function upsertComment(s, body) {
  return s.call('POST', '/api/taskcomment', body);
}

/** Tạo 1 comment, throw nếu fail. @returns comment vừa tạo. */
async function addComment(s, taskId, body = {}) {
  const res = await upsertComment(s, { taskId, content: 'APITEST comment', ...body });
  if (!res.body?.success) throw new Error(`addComment fail: ${JSON.stringify(res.body)}`);
  return res.body.data[0];
}

/**
 * `DELETE /api/taskcomment/{id}` — soft delete comment + mọi reply. Không tồn tại -> body.status
 * 404; comment của người khác -> body.status 403.
 */
function deleteComment(s, id) {
  return s.call('DELETE', `/api/taskcomment/${id}`);
}

module.exports = { getComments, upsertComment, addComment, deleteComment };
