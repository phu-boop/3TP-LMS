ALTER TABLE content_comment
    ADD COLUMN IF NOT EXISTS is_public BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS is_admin BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS is_edited BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS is_pinned BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS school_id UUID REFERENCES school(id) ON DELETE SET NULL;

CREATE INDEX IF NOT EXISTS idx_content_comment_visibility ON content_comment(content_item_id, status, is_public) WHERE is_deleted = FALSE;
CREATE INDEX IF NOT EXISTS idx_content_comment_school_id ON content_comment(content_item_id, school_id) WHERE is_deleted = FALSE;
