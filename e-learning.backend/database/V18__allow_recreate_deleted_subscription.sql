-- Allow re-creating subscriptions for tenant/school pairs when the previous mapping was soft-deleted.
-- The existing UNIQUE constraint prevents inserts even when the previous subscription is marked as deleted.

ALTER TABLE school_tenant_mapping
    DROP CONSTRAINT IF EXISTS school_tenant_mapping_tenant_id_school_id_key;

CREATE UNIQUE INDEX IF NOT EXISTS uq_school_tenant_mapping_active
    ON school_tenant_mapping(tenant_id, school_id)
    WHERE is_deleted = FALSE;
