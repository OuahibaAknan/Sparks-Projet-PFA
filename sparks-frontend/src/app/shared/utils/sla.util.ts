export function formatSla(minutesRemaining: number): string {
  if (minutesRemaining <= 0) return 'Overdue';
  const hours = Math.floor(minutesRemaining / 60);
  const minutes = minutesRemaining % 60;
  if (hours >= 24) {
    const days = Math.floor(hours / 24);
    return `${days}d remaining`;
  }
  if (hours >= 1) return `${hours}h remaining`;
  return `${minutes}m remaining`;
}
