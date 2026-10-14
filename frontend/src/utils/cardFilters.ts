import type { CardDto, DuePreset } from "../types";

export function filterCards(
  cards: CardDto[],
  opts: {
    query: string;
    assigneeId: string;
    labelId: string;
    duePreset: DuePreset;
  }
): CardDto[] {
  const q = opts.query.trim().toLowerCase();
  const now = new Date();
  const weekEnd = new Date(now);
  weekEnd.setDate(weekEnd.getDate() + 7);

  return cards.filter((card) => {
    if (q && !card.title.toLowerCase().includes(q)) return false;
    if (opts.assigneeId && card.assigneeId !== opts.assigneeId) return false;
    if (opts.labelId && !(card.labelIds ?? []).includes(opts.labelId)) return false;

    if (opts.duePreset === "none") {
      if (card.dueDateUtc) return false;
    } else if (opts.duePreset === "overdue") {
      if (!card.dueDateUtc || new Date(card.dueDateUtc) >= now) return false;
    } else if (opts.duePreset === "thisWeek") {
      if (!card.dueDateUtc) return false;
      const due = new Date(card.dueDateUtc);
      if (due < now || due > weekEnd) return false;
    }

    return true;
  });
}
