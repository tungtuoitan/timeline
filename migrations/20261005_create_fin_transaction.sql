-- Migration: Create pro.fin_transaction + pro.fin_price_cache (drop pro.finance_snapshot)
-- Date: 20261005
-- Issue: TungRoot #1482 (đưa finance vào DB SuperApp)
-- Description:
--   fin_transaction  — multi-asset ledger, the only source of truth for money. One row = one balance
--                      change of one asset in one account (amount signed). Balance of (account, asset)
--                      = SUM(amount). A trade/P2P has several legs sharing group_key.
--   fin_price_cache  — daily close prices fetched from public APIs (Binance klines, Yahoo). Not user
--                      data, safe to truncate: the sync script refills it.
--   finance_snapshot — monthly manual snapshot from #1481, only ever created on SuperApp-dev; replaced.
--   Timestamps are UTC (#1450) → defaults use SYSUTCDATETIME().

IF OBJECT_ID('pro.finance_snapshot', 'U') IS NOT NULL
BEGIN
    DROP TABLE [pro].[finance_snapshot];
    PRINT 'Dropped table: pro.finance_snapshot';
END
GO

IF OBJECT_ID('pro.fin_transaction', 'U') IS NULL
BEGIN
    CREATE TABLE [pro].[fin_transaction] (
        id            INT IDENTITY(1,1) NOT NULL,
        user_id       INT NOT NULL,
        account       NVARCHAR(50) NOT NULL,      -- 'BIDV', 'Binance'
        occurred_at   DATETIME2 NOT NULL,         -- UTC
        asset         VARCHAR(20) NOT NULL,       -- 'VND', 'USDT', 'BTC', 'EQ_TSLA'...
        amount        DECIMAL(28,10) NOT NULL,    -- signed: + in, - out
        value_vnd     DECIMAL(19,0) NULL,         -- VND value at the time, when known (P2P fiat, bank)
        kind          VARCHAR(10) NOT NULL,
        category      NVARCHAR(100) NULL,
        counterparty  NVARCHAR(200) NULL,
        description   NVARCHAR(1000) NULL,
        group_key     VARCHAR(100) NULL,          -- legs of one event: 'convert:<id>', 'p2p:<orderNo>'
        source        VARCHAR(20) NOT NULL,
        external_id   NVARCHAR(200) NULL,         -- id at the source, dedupe on re-import/webhook retry
        raw_json      NVARCHAR(MAX) NULL,
        note          NVARCHAR(1000) NULL,
        created_at    DATETIME2 NOT NULL CONSTRAINT DF_pro_fin_transaction_created_at DEFAULT SYSUTCDATETIME(),
        updated_at    DATETIME2 NOT NULL CONSTRAINT DF_pro_fin_transaction_updated_at DEFAULT SYSUTCDATETIME(),
        deleted_at    DATETIME2 NULL,
        CONSTRAINT PK_pro_fin_transaction PRIMARY KEY (id),
        CONSTRAINT CK_pro_fin_transaction_kind CHECK (kind IN ('expense','income','invest','trade','transfer','debt','adjust')),
        CONSTRAINT CK_pro_fin_transaction_source CHECK (source IN ('sepay','binance','manual','import'))
    );
    PRINT 'Created table: pro.fin_transaction';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_pro_fin_transaction_source_external' AND object_id = OBJECT_ID('pro.fin_transaction'))
BEGIN
    CREATE UNIQUE INDEX UX_pro_fin_transaction_source_external
        ON [pro].[fin_transaction] (user_id, source, external_id)
        WHERE external_id IS NOT NULL AND deleted_at IS NULL;
    PRINT 'Created unique index: UX_pro_fin_transaction_source_external';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_pro_fin_transaction_user_occurred' AND object_id = OBJECT_ID('pro.fin_transaction'))
BEGIN
    CREATE INDEX IX_pro_fin_transaction_user_occurred
        ON [pro].[fin_transaction] (user_id, occurred_at)
        INCLUDE (account, asset, amount, value_vnd, kind)
        WHERE deleted_at IS NULL;
    PRINT 'Created index: IX_pro_fin_transaction_user_occurred';
END
GO

IF OBJECT_ID('pro.fin_price_cache', 'U') IS NULL
BEGIN
    CREATE TABLE [pro].[fin_price_cache] (
        date        DATE NOT NULL,
        asset       VARCHAR(20) NOT NULL,
        quote       VARCHAR(10) NOT NULL,         -- 'USDT' for crypto/stocks, 'VND' for USDT
        price       DECIMAL(28,10) NOT NULL,
        source      VARCHAR(20) NOT NULL,         -- 'binance', 'yahoo'
        fetched_at  DATETIME2 NOT NULL CONSTRAINT DF_pro_fin_price_cache_fetched_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_pro_fin_price_cache PRIMARY KEY (asset, quote, date)
    );
    PRINT 'Created table: pro.fin_price_cache';
END
GO

-- Rollback:
-- DROP TABLE IF EXISTS [pro].[fin_price_cache];
-- DROP TABLE IF EXISTS [pro].[fin_transaction];
