export interface AppNotification {
  id: string;
  type: string;
  message: string;
  read: boolean;
  createdAt: string;
  ticketId: string | null;
}
