-- Add device_id to auth.refresh_tokens for per-device token chain scoping.
-- Existing rows get NULL (no device) — treated as RevokeAll on reuse for safety.

ALTER TABLE [auth].[refresh_tokens]
    ADD [device_id] NVARCHAR(100) NULL;

CREATE INDEX [IX_refresh_tokens_user_device]
    ON [auth].[refresh_tokens] ([user_id], [device_id]);
