CREATE TABLE IF NOT EXISTS ticket (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL REFERENCES tenant(id) ON DELETE CASCADE,
    school_id UUID NOT NULL REFERENCES school(id) ON DELETE CASCADE,
    creator_id UUID NOT NULL REFERENCES user_account(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    description TEXT NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'NEW',
    resolution_note TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_ticket_tenant_id ON ticket(tenant_id);
CREATE INDEX IF NOT EXISTS idx_ticket_school_id ON ticket(school_id);
CREATE INDEX IF NOT EXISTS idx_ticket_status ON ticket(status);

-- Ticket attachments (screenshots, error images)
CREATE TABLE IF NOT EXISTS ticket_attachment (
    id UUID PRIMARY KEY,
    ticket_id UUID NOT NULL REFERENCES ticket(id) ON DELETE CASCADE,
    file_name VARCHAR(255) NOT NULL,
    file_path VARCHAR(500) NOT NULL,
    content_type VARCHAR(100) NOT NULL,
    file_size BIGINT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_ticket_attachment_ticket_id ON ticket_attachment(ticket_id);
