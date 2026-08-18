CREATE TABLE email_outbox_message (
    id UUID PRIMARY KEY,
    to_email VARCHAR(255) NOT NULL,
    subject VARCHAR(500) NOT NULL,
    template_name VARCHAR(100) NOT NULL,
    payload_json TEXT NOT NULL,
    status INT NOT NULL DEFAULT 0, -- 0: Pending, 1: Processing, 2: Sent, 3: Failed
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processed_at TIMESTAMPTZ,
    error TEXT
);

CREATE INDEX idx_email_outbox_status ON email_outbox_message(status, created_at);
