-- Task 1747: Reflect tab for projects (Type / I think / Reality rows, JSON array string)
-- Idempotent. Apply on SuperApp-dev first; SuperApp-pro only after Tung confirms.
IF COL_LENGTH('pro.project', 'reflect_json') IS NULL
BEGIN
    ALTER TABLE pro.project ADD reflect_json NVARCHAR(MAX) NULL;
    PRINT 'Added pro.project.reflect_json';
END
ELSE
    PRINT 'pro.project.reflect_json already exists';
