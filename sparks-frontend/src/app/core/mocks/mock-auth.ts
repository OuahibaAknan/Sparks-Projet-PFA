import { DemoAccount } from '../models/auth.model';

export const DEMO_ACCOUNTS: DemoAccount[] = [
  { label: 'Generalist: Marcus Weber', email: 'marcus.weber@alten.com', password: 'demo1234', role: 'Generalist' },
  { label: 'Specialist: Dr. Isabelle Morin', email: 'i.morin@alten.com', password: 'demo1234', role: 'Specialist' },
  { label: 'Administrator: Amara Diallo', email: 'a.diallo@alten.com', password: 'demo1234', role: 'Admin' },
  { label: 'Team Lead: Youssef El Amrani', email: 'y.elamrani@alten.com', password: 'demo1234', role: 'TeamLead' },
  { label: 'Polyvalent: Léa Fontaine', email: 'l.fontaine@alten.com', password: 'demo1234', role: 'Polyvalent' },
];
