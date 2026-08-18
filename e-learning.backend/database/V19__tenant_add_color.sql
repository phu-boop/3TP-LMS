-- Add a color field to tenant records for tenant-level branding.
ALTER TABLE tenant
    ADD COLUMN IF NOT EXISTS color VARCHAR(32);
