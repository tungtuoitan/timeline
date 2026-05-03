-- k.question: replace knowledge_id with node_id

ALTER TABLE k.question DROP CONSTRAINT FK_k_question_knowledge;
DROP INDEX IX_k_question_knowledge ON k.question;
ALTER TABLE k.question DROP COLUMN knowledge_id;

ALTER TABLE k.question ADD node_id INT NULL;
ALTER TABLE k.question ADD CONSTRAINT FK_k_question_node FOREIGN KEY (node_id) REFERENCES k.node(id) ON DELETE SET NULL;
CREATE INDEX IX_k_question_node ON k.question (node_id);
