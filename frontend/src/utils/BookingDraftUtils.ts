import { BookingDraft, BookingDraftItem } from "@/types/BookingDraft";

const BOOKING_KEY = "booking_draft";

function safeParse<T>(value: string | null): T | null {
    if (!value) return null;
    try {
        return JSON.parse(value);
    } catch {
        return null;
    }
}

export function getDraft(): BookingDraft {
    if (typeof window === "undefined") return { items: [] };

    const parsed = safeParse<BookingDraft>(
        localStorage.getItem(BOOKING_KEY)
    );

    return parsed && Array.isArray(parsed.items)
        ? parsed
        : { items: [] };
}

export function saveDraft(draft: BookingDraft) {
    localStorage.setItem(BOOKING_KEY, JSON.stringify(draft));
}

export function addToBookingDraft(item: BookingDraftItem) {
    const existing = getBookingDraft();

    const newDraft: BookingDraft = {
        items: [...(existing?.items ?? []), item],
    };

    localStorage.setItem(BOOKING_KEY, JSON.stringify(newDraft));
}

export function getBookingDraft(): BookingDraft | null {
    if (typeof window === "undefined") return null;

    const raw = localStorage.getItem(BOOKING_KEY);
    if (!raw) return null;

    try {
        return JSON.parse(raw);
    } catch {
        return null;
    }
}

export function clearBookingDraft() {
    localStorage.removeItem(BOOKING_KEY);
}

export function clearDraft() {
    localStorage.removeItem(BOOKING_KEY);
}
export function addToDraft(item: BookingDraftItem) {
    const draft = getDraft();

    const existingIndex = draft.items.findIndex(
        (i) =>
            i.roomTypeId === item.roomTypeId &&
            i.ratePlanId === item.ratePlanId &&
            i.checkIn === item.checkIn &&
            i.checkOut === item.checkOut
    );

    if (existingIndex !== -1) {
        // 🔥 merge (increase room count)
        draft.items[existingIndex].totalRooms += item.totalRooms;
    } else {
        draft.items.push(item);
    }

    saveDraft(draft);
}
export function removeDraftItem(index: number) {
    const draft = getDraft();

    draft.items.splice(index, 1);

    saveDraft(draft);
}
export function updateDraftItem(
    index: number,
    payload: Partial<BookingDraftItem>
) {
    const draft = getDraft();

    draft.items[index] = {
        ...draft.items[index],
        ...payload,
    };

    saveDraft(draft);
}
export function getDraftTotal(): number {
    const draft = getDraft();

    return draft.items.reduce((acc, item) => {
        return acc + (item.price ?? 0) * item.totalRooms;
    }, 0);
}