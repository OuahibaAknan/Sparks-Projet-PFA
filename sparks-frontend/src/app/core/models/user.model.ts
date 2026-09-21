export type UserRole = 'Generalist' | 'Specialist' | 'Admin' | 'TeamLead' | 'Polyvalent';

export type UserAvailability = 'Available' | 'Away' | 'Offline';

export type UserStatus = 'Active' | 'Inactive';

export interface User {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole;
  status: UserStatus;
  availability: UserAvailability;
  activeTickets: number;
  initials: string;
  avatarColor: string;
  createdAt: string;
}

export interface CreateUserPayload {
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole;
}

export interface UpdateUserPayload {
  firstName?: string;
  lastName?: string;
  role?: UserRole;
  status?: UserStatus;
  availability?: UserAvailability;
}
