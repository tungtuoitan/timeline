/*
**Fullpath:** /api-tests/flows/08-dashboard/08-1-activity-habits.test.js
*/
'use strict';

const { before, test, after } = require('node:test');
const assert = require('node:assert/strict');
const dm = require('../../_lib/dataManager');
const { ok, assertHttp400 } = require('../../_lib/verify');

let a;
let b;
let p;
let work; // task thường
let habit; // task type=repeat

const RANGE = '?from=2026-09-28&to=2026-10-11';

before(async () => {
  a = await dm.newSession('08-1a');
  b = await dm.newSession('08-1b');
  p = await a.createProject();
  work = await a.createTask(p.id);
  habit = await a.createTask(p.id, { type: 'repeat', status: 'background_progress' });

  // Tuần 28/09: devlog CN 23:30 (+07), comment, decision, track (không tính mặc định)
  await a.addComment(work.id, { type: 'devlog', occurredAt: '2026-10-04T16:30:00Z' });
  await a.addComment(work.id, { type: 'comment', occurredAt: '2026-09-30T03:00:00Z' });
  await a.addComment(work.id, { type: 'decision', occurredAt: '2026-09-29T03:00:00Z' });
  await a.addComment(work.id, { type: 'track', occurredAt: '2026-09-30T03:00:00Z' });
  // Tuần 05/10: T2 00:30 (+07) = CN 17:30 UTC
  await a.addComment(work.id, { type: 'devlog', occurredAt: '2026-10-04T17:30:00Z' });

  // Tracker: 2 track + 1 note + 1 devlog (không phải entry)
  await a.addComment(habit.id, { type: 'track', content: 'Chạy 5km', occurredAt: '2026-09-28T23:30:00Z' }); // 29/09 06:30 VN
  await a.addComment(habit.id, { type: 'track', content: 'Chạy', occurredAt: '2026-10-02T00:00:00+07:00' });
  await a.addComment(habit.id, { type: 'comment', content: 'Mưa, không chạy', occurredAt: '2026-09-30T12:00:00+07:00' });
  await a.addComment(habit.id, { type: 'devlog', content: 'tạo tracker', occurredAt: '2026-09-28T03:00:00Z' });
});

after(async () => {
  await a?.cleanup();
  await b?.cleanup();
});

const countOf = (points, week) => points.find((x) => x.projectId === p.id && x.weekStart === week)?.count ?? 0;

test('Flow08#1: activity đếm devlog/comment/decision theo (tuần, project), bỏ track', async () => {
  const points = ok(await a.getActivity(RANGE));
  // tracker cũng là task của project → devlog (28/09) + comment (30/09) của tracker cũng được đếm
  assert.equal(countOf(points, '2026-09-28'), 5, 'tuần 28/09: devlog CN + comment + decision + devlog/comment tracker');
  assert.ok(points.every((x) => x.projectName && x.projectStatus), 'có tên + status project');
});

test('Flow08#1.1: ranh giới tuần theo giờ VN (T2 00:30 +07 thuộc tuần mới)', async () => {
  const points = ok(await a.getActivity(RANGE));
  assert.equal(countOf(points, '2026-10-05'), 1);
});

test('Flow08#1.2: types=track chỉ đếm track', async () => {
  const points = ok(await a.getActivity(`${RANGE}&types=track`));
  assert.equal(countOf(points, '2026-09-28'), 3, 'track của task thường (30/09) + 2 track tracker (29/09, 02/10)');
  assert.equal(countOf(points, '2026-10-05'), 0);
});

test('Flow08#1.3: from > to, type lạ -> HTTP 400', async () => {
  assertHttp400(await a.getActivity('?from=2026-10-11&to=2026-09-28'), 'from > to');
  assertHttp400(await a.getActivity(`${RANGE}&types=devlog,foo`), 'type lạ');
});

test('Flow08#2: habits mặc định chỉ repeat, entry track+comment theo ngày VN', async () => {
  const series = ok(await a.getHabits(RANGE));
  assert.deepEqual(series.map((x) => x.taskId), [habit.id], 'chỉ tracker');
  const [h] = series;
  assert.deepEqual(
    h.entries.map((e) => [e.date, e.type]),
    [['2026-09-29', 'track'], ['2026-09-30', 'comment'], ['2026-10-02', 'track']],
  );
  assert.equal(h.entries[1].content, 'Mưa, không chạy');
});

test('Flow08#2.1: taskIds= task thường', async () => {
  const series = ok(await a.getHabits(`${RANGE}&taskIds=${work.id}`));
  assert.deepEqual(series.map((x) => x.taskId), [work.id]);
  assert.deepEqual(series[0].entries.map((e) => e.type).sort(), ['comment', 'track']);
});

test('Flow08#2.2: excludeTaskIds bỏ tracker', async () => {
  const series = ok(await a.getHabits(`${RANGE}&excludeTaskIds=${habit.id}`));
  assert.equal(series.length, 0);
});

test('Flow08#2.3: taskIds=abc -> HTTP 400', async () => {
  assertHttp400(await a.getHabits(`${RANGE}&taskIds=abc`));
});

test('Flow08#3: user B không thấy activity/tracker của A', async () => {
  const points = ok(await b.getActivity(RANGE));
  assert.ok(points.every((x) => x.projectId !== p.id), 'không có project của A');
  assert.equal(ok(await b.getHabits(RANGE)).length, 0, 'không có tracker');
  assert.equal(ok(await b.getHabits(`${RANGE}&taskIds=${habit.id},${work.id}`)).length, 0, 'taskIds của A bị bỏ qua');
});
