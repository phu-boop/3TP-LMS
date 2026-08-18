export type TicketStatus = 'NEW' | 'IN_PROGRESS' | 'RESOLVED' | 'CLOSED';

export type Ticket = {
  id: string;
  title: string;
  description: string;
  status: TicketStatus;
  resolutionNote?: string;
  createdAt: Date;
  updatedAt: Date;
  reporter: {
    id: string;
    name: string;
    avatarUrl?: string;
  };
  attachments?: string[];
};
