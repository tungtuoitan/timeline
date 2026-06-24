-- K Attachment tables (SQL Server)

CREATE TABLE k.attachment (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    user_id     INT NOT NULL,
    title       NVARCHAR(500) NOT NULL,
    type        NVARCHAR(50)  NOT NULL CONSTRAINT df_att_type DEFAULT 'code',
    language    NVARCHAR(50)  NULL,
    content     NVARCHAR(MAX) NULL,
    sort_order  INT NOT NULL CONSTRAINT df_att_sort DEFAULT 0,
    created_at  DATETIME2 NULL,
    updated_at  DATETIME2 NULL,
    deleted_at  DATETIME2 NULL
);

CREATE INDEX idx_k_attachment_user_id ON k.attachment(user_id);

CREATE TABLE k.attachment_link (
    id             INT IDENTITY(1,1) PRIMARY KEY,
    attachment_id  INT NOT NULL REFERENCES k.attachment(id),
    entity_type    NVARCHAR(20) NOT NULL,
    entity_id      INT NOT NULL,
    created_at     DATETIME2 NULL,
    CONSTRAINT uq_att_link UNIQUE(attachment_id, entity_type, entity_id)
);

CREATE INDEX idx_k_att_link_entity ON k.attachment_link(entity_type, entity_id);
