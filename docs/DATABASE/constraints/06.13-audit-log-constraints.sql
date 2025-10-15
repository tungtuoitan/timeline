-- ============================================
-- FILE: constraints/06.13-audit-log.sql
-- PURPOSE: Audit log table and trigger for sensitive operations
-- DEPENDENCIES: 02-tables-core.sql, 03-tables-workspace.sql, 07-tables-entities.sql
-- ============================================

PRINT '🔒 Creating audit log table and trigger...';
GO

-- Create audit_log table
CREATE TABLE audit_log (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    action NVARCHAR(100) NOT NULL,
    entity_type NVARCHAR(50) NOT NULL,
    entity_id BIGINT NOT NULL,
    details NVARCHAR(MAX), -- JSON with operation details
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT fk_audit_log_user FOREIGN KEY (user_id) 
        REFERENCES users(id)
);

PRINT '   ✅ audit_log table created';

-- Trigger: Log sensitive operations
CREATE OR ALTER TRIGGER tr_audit_sensitive_operations
ON DATABASE
FOR DDL_DATABASE_LEVEL_EVENTS
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @event_data XML = EVENTDATA();
    DECLARE @user_id INT = CONVERT(INT, @event_data.value('(/EVENT_INSTANCE/UserName)[1]', 'nvarchar(128)'));
    DECLARE @object_type NVARCHAR(50) = @event_data.value('(/EVENT_INSTANCE/ObjectType)[1]', 'nvarchar(50)');
    DECLARE @object_id BIGINT = NULL; -- Adjust based on actual event data
    DECLARE @action NVARCHAR(100) = @event_data.value('(/EVENT_INSTANCE/EventType)[1]', 'nvarchar(100)');
    
    IF @user_id IS NULL
        SET @user_id = -1; -- System user or unknown
    
    INSERT INTO audit_log (
        user_id,
        action,
        entity_type,
        entity_id,
        details
    )
    VALUES (
        @user_id,
        @action,
        @object_type,
        @object_id,
        @event_data
    );
END;
GO

PRINT '   ✅ tr_audit_sensitive_operations created';

GO

PRINT '✅ Audit log table and trigger created successfully!';
PRINT '📊 Next step: Run constraints/06.14-validation-procedures.sql';
GO