import { BookingDraft, BookingDraftItem } from "@/types/BookingDraft";

const BOOKING_KEY = "booking_draft";

// =========================
// SAFE PARSE
// =========================
function safeParse<T>(value: string | null): T | null {
  if (!value) return null;
  try {
    return JSON.parse(value);
  } catch {
    return null;
  }
}

// =========================
// GET DRAFT
// =========================
export function getDraft(): BookingDraft {
  if (typeof window === "undefined") {
    return { slug: "", checkIn: "", checkOut: "", items: [] };
  }

  const parsed = safeParse<BookingDraft>(
    localStorage.getItem(BOOKING_KEY)
  );

  if (!parsed) {
    return { slug: "", checkIn: "", checkOut: "", items: [] };
  }

  return parsed;
}

// =========================
// SAVE
// =========================
export function saveDraft(draft: BookingDraft) {
  localStorage.setItem(BOOKING_KEY, JSON.stringify(draft));
}

// =========================
// ADD / UPDATE ITEM
// =========================
export function addToDraft(
  context: {
    slug: string;
    checkIn: string;
    checkOut: string;
  },
  item: BookingDraftItem
) {
  const current = getDraft();

  // =========================
  // RESET CONTEXT (HOTEL / DATE)
  // =========================
  if (
    !current.slug || // 🔥 FIX empty state
    current.slug !== context.slug ||
    current.checkIn !== context.checkIn ||
    current.checkOut !== context.checkOut
  ) {
    const newDraft: BookingDraft = {
      ...context,
      items: [item],
    };

    saveDraft(newDraft);
    return;
  }

  // =========================
  // MERGE ITEM (IMMUTABLE)
  // =========================
  const existingIndex = current.items.findIndex(
    (i) =>
      i.roomTypeId === item.roomTypeId &&
      i.ratePlanId === item.ratePlanId
  );

  let newItems: BookingDraftItem[];

  if (existingIndex !== -1) {
    newItems = current.items.map((i, idx) =>
      idx === existingIndex
        ? { ...i, qty: i.qty + item.qty }
        : i
    );
  } else {
    newItems = [...current.items, item];
  }

  saveDraft({
    ...current,
    items: newItems,
  });
}

// =========================
// REMOVE ITEM (IMMUTABLE)
// =========================
export function removeDraftItem(index: number) {
  const draft = getDraft();

  const newItems = draft.items.filter((_, i) => i !== index);

  saveDraft({
    ...draft,
    items: newItems,
  });
}

// =========================
// UPDATE ITEM (IMMUTABLE)
// =========================
export function updateDraftItem(
  index: number,
  payload: Partial<BookingDraftItem>
) {
  const draft = getDraft();

  const newItems = draft.items.map((item, i) =>
    i === index ? { ...item, ...payload } : item
  );

  saveDraft({
    ...draft,
    items: newItems,
  });
}

// =========================
// CLEAR
// =========================
export function clearDraft() {
  localStorage.removeItem(BOOKING_KEY);
}

// =========================
// TOTAL PRICE
// =========================
export function getDraftTotal(): number {
  const draft = getDraft();

  return draft.items.reduce((acc, item) => {
    return acc + (item.price ?? 0) * item.qty;
  }, 0);
}

// =========================
// TOTAL ITEM (QTY)
// =========================
export function getDraftCount(): number {
  const draft = getDraft();

  return draft.items.reduce((acc, item) => acc + item.qty, 0);
}