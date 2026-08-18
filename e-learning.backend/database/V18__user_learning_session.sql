-- V18__user_learning_session.sql

CREATE TABLE IF NOT EXISTS user_learning_session (
    id                  UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    learning_session_id UUID          NOT NULL,        -- Client-generated UUID cho mỗi lần xem content
    session_id          UUID          NOT NULL,        -- Auth session ID của người dùng (từ token/cookie)
    user_id             UUID          NOT NULL REFERENCES user_account(id) ON DELETE CASCADE,
    school_id           UUID          REFERENCES school(id) ON DELETE SET NULL,
    tenant_id           UUID          NOT NULL REFERENCES tenant(id) ON DELETE CASCADE,
    content_item_id     UUID          REFERENCES content_item(id) ON DELETE SET NULL,
    start_at            TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    last_update         TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    inactivity_ms       BIGINT        NOT NULL DEFAULT 0,  -- Tổng thời gian inactivity (ms)
    device_info         JSONB,                            -- UA, platform, etc.
    ip_address          INET,
    is_deleted          BOOLEAN       NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by          UUID          REFERENCES user_account(id) ON DELETE SET NULL,
    updated_at          TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_by          UUID          REFERENCES user_account(id) ON DELETE SET NULL
);

-- Unique constraint cho ON CONFLICT trong StartSessionAsync
CREATE UNIQUE INDEX IF NOT EXISTS uq_uls_learning_session_id
    ON user_learning_session (learning_session_id);

CREATE INDEX IF NOT EXISTS idx_uls_user_tenant
    ON user_learning_session (tenant_id, user_id, is_active)
    WHERE is_deleted = FALSE;

CREATE INDEX IF NOT EXISTS idx_uls_learning_session_lookup
    ON user_learning_session (learning_session_id)
    WHERE is_deleted = FALSE;

CREATE INDEX IF NOT EXISTS idx_uls_auth_session_lookup
    ON user_learning_session (session_id)
    WHERE is_deleted = FALSE;

CREATE INDEX IF NOT EXISTS idx_uls_content
    ON user_learning_session (content_item_id)
    WHERE is_deleted = FALSE;

CREATE INDEX IF NOT EXISTS idx_uls_time_range
    ON user_learning_session (tenant_id, start_at, last_update)
    WHERE is_deleted = FALSE;

CREATE INDEX IF NOT EXISTS idx_uls_school
    ON user_learning_session (school_id)
    WHERE is_deleted = FALSE;

-- Grant privileges on user_learning_session table and v_user_learning_time view to the app user.

GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE user_learning_session TO lms_dev;
GRANT SELECT ON TABLE v_user_learning_time TO lms_dev;

-- View tổng hợp thời gian học
CREATE OR REPLACE VIEW v_user_learning_time AS
SELECT
    tenant_id,
    user_id,
    school_id,
    content_item_id,
    COUNT(*)                                                      AS session_count,
    SUM(EXTRACT(EPOCH FROM (last_update - start_at)) * 1000)      AS total_session_ms,
    COALESCE(SUM(inactivity_ms), 0)                               AS total_inactivity_ms,
    SUM(EXTRACT(EPOCH FROM (last_update - start_at)) * 1000)
        - COALESCE(SUM(inactivity_ms), 0)                         AS active_learning_ms
FROM user_learning_session
WHERE is_deleted = FALSE
GROUP BY tenant_id, user_id, school_id, content_item_id;
