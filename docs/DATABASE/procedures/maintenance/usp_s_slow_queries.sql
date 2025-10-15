CREATE OR ALTER PROCEDURE usp_s_slow_queries
    @top INT = 20,
    @min_avg_duration_ms INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP (@top)
        qs.execution_count,
        qs.total_elapsed_time / 1000 / 1000 AS total_elapsed_seconds,
        qs.total_elapsed_time / 1000 / qs.execution_count AS avg_elapsed_ms,
        qs.total_logical_reads / qs.execution_count AS avg_logical_reads,
        qs.total_physical_reads / qs.execution_count AS avg_physical_reads,
        qs.creation_time,
        qs.last_execution_time,
        SUBSTRING(
            st.text,
            (qs.statement_start_offset / 2) + 1,
            (
                CASE qs.statement_end_offset
                    WHEN -1 THEN DATALENGTH(st.text)
                    ELSE qs.statement_end_offset
                END - qs.statement_start_offset
            ) / 2 + 1
        ) AS query_text,
        qp.query_plan
    FROM sys.dm_exec_query_stats qs
    CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
    CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) qp
    WHERE qs.total_elapsed_time / 1000 / qs.execution_count > @min_avg_duration_ms
    ORDER BY avg_elapsed_ms DESC;
END;
GO

PRINT '   ✅ usp_s_slow_queries created';

-- Procedure: Get table sizes
GO

PRINT 'âœ… usp_s_slow_queries created successfully';
GO

