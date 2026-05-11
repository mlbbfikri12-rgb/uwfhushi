import type {
  GuestCheckoutItemPayload,
  GuestCheckoutPayload,
} from "@/types/booking";
import type { BookingDraft, BookingDraftItem } from "@/types/BookingDraft";

const BOOKING_KEY = "booking_draft";
const BOOKING_VERSION = 1 as const;

type BookingDraftContext = {
  slug: string;
  checkIn: string;
  checkOut: string;
  branchCode?: string;
};

type LegacyDraft = Partial<Omit<BookingDraft, "version" | "items">> & {
  version?: number;
  items?: Partial<BookingDraftItem>[];
};

export function createEmptyDraft(): BookingDraft {
  return {
    version: BOOKING_VERSION,
    slug: "",
    checkIn: "",
    checkOut: "",
    items: [],
  };
}

export function buildDraftItemKey(
  context: BookingDraftContext,
  item: Pick<BookingDraftItem, "roomTypeId" | "ratePlanId">,
) {
  return [
    context.branchCode ?? "",
    context.slug,
    context.checkIn,
    context.checkOut,
    item.roomTypeId,
    item.ratePlanId,
  ].join("/");
}

function safeParse(value: string | null): unknown {
  if (!value) return null;

  try {
    return JSON.parse(value);
  } catch {
    return null;
  }
}

function isObject(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function toNumber(value: unknown, fallback: number) {
  return typeof value === "number" && Number.isFinite(value) ? value : fallback;
}

function toString(value: unknown, fallback = "") {
  return typeof value === "string" ? value : fallback;
}

function toBooleanOrUndefined(value: unknown) {
  return typeof value === "boolean" ? value : undefined;
}

function normalizeItem(
  value: unknown,
  context: BookingDraftContext,
): BookingDraftItem | null {
  if (!isObject(value)) return null;

  const roomTypeId = toString(value.roomTypeId);
  const ratePlanId = toString(value.ratePlanId);

  if (!roomTypeId || !ratePlanId) return null;

  const item: BookingDraftItem = {
    key: toString(value.key) || buildDraftItemKey(context, { roomTypeId, ratePlanId }),
    roomTypeId,
    ratePlanId,
    roomTypeName: toString(value.roomTypeName) || undefined,
    ratePlanName: toString(value.ratePlanName) || undefined,
    imageUrl: toString(value.imageUrl) || undefined,
    qty: Math.max(1, Math.floor(toNumber(value.qty, 1))),
    price: typeof value.price === "number" ? value.price : undefined,
    MaxAdults: typeof value.MaxAdults === "number" ? value.MaxAdults : undefined,
    MaxChildren: typeof value.MaxChildren === "number" ? value.MaxChildren : undefined,
    isBreakFast: toBooleanOrUndefined(value.isBreakFast),
    isRefundable: toBooleanOrUndefined(value.isRefundable),
  };

  return item;
}

function normalizeDraft(value: unknown): BookingDraft {
  if (!isObject(value)) return createEmptyDraft();

  const legacy = value as LegacyDraft;
  const context: BookingDraftContext = {
    slug: toString(legacy.slug),
    branchCode: toString(legacy.branchCode) || undefined,
    checkIn: toString(legacy.checkIn),
    checkOut: toString(legacy.checkOut),
  };

  const normalizedItems = Array.isArray(legacy.items)
    ? legacy.items
        .map((item) => normalizeItem(item, context))
        .filter((item): item is BookingDraftItem => item !== null)
    : [];

  return {
    version: BOOKING_VERSION,
    ...context,
    items: mergeDraftItems(context, normalizedItems),
  };
}

function canUseStorage() {
  return typeof window !== "undefined" && typeof window.localStorage !== "undefined";
}

export function getDraft(): BookingDraft {
  if (!canUseStorage()) return createEmptyDraft();

  return normalizeDraft(safeParse(localStorage.getItem(BOOKING_KEY)));
}

export function saveDraft(draft: BookingDraft) {
  if (!canUseStorage()) return;

  const normalized = normalizeDraft(draft);
  localStorage.setItem(BOOKING_KEY, JSON.stringify(normalized));
}

export function mergeDraftItems(
  context: BookingDraftContext,
  items: BookingDraftItem[],
) {
  const byKey = new Map<string, BookingDraftItem>();

  for (const item of items) {
    const key = item.key || buildDraftItemKey(context, item);
    const existing = byKey.get(key);

    if (existing) {
      byKey.set(key, {
        ...existing,
        ...item,
        key,
        qty: existing.qty + item.qty,
      });
      continue;
    }

    byKey.set(key, { ...item, key, qty: Math.max(1, item.qty) });
  }

  return Array.from(byKey.values());
}

export function addToDraft(context: BookingDraftContext, item: BookingDraftItem) {
  const current = getDraft();
  const normalizedItem = normalizeItem(item, context);
  if (!normalizedItem) return;

  const isDifferentContext =
    !current.slug ||
    current.slug !== context.slug ||
    current.branchCode !== context.branchCode ||
    current.checkIn !== context.checkIn ||
    current.checkOut !== context.checkOut;

  if (isDifferentContext) {
    saveDraft({
      version: BOOKING_VERSION,
      ...context,
      items: [normalizedItem],
    });
    return;
  }

  saveDraft({
    ...current,
    items: mergeDraftItems(context, [...current.items, normalizedItem]),
  });
}

export function removeDraftItem(index: number) {
  const draft = getDraft();
  saveDraft({
    ...draft,
    items: draft.items.filter((_, itemIndex) => itemIndex !== index),
  });
}

export function removeDraftItemByKey(key: string) {
  const draft = getDraft();
  saveDraft({
    ...draft,
    items: draft.items.filter((item) => item.key !== key),
  });
}

export function updateDraftItem(index: number, payload: Partial<BookingDraftItem>) {
  const draft = getDraft();
  const context = {
    slug: draft.slug,
    branchCode: draft.branchCode,
    checkIn: draft.checkIn,
    checkOut: draft.checkOut,
  };

  const items = draft.items
    .map((item, itemIndex) =>
      itemIndex === index
        ? normalizeItem({ ...item, ...payload }, context)
        : item,
    )
    .filter((item): item is BookingDraftItem => item !== null);

  saveDraft({
    ...draft,
    items,
  });
}

export function clearDraft() {
  if (!canUseStorage()) return;
  localStorage.removeItem(BOOKING_KEY);
}

export function getDraftTotal(draft: BookingDraft = getDraft()): number {
  return draft.items.reduce((acc, item) => acc + (item.price ?? 0) * item.qty, 0);
}

export function getDraftCount(draft: BookingDraft = getDraft()): number {
  return draft.items.reduce((acc, item) => acc + item.qty, 0);
}

export function buildGuestCheckoutItems(
  draft: BookingDraft = getDraft(),
): GuestCheckoutItemPayload[] {
  return draft.items.map((item) => ({
    roomTypeId: item.roomTypeId,
    ratePlanId: item.ratePlanId,
    checkIn: draft.checkIn,
    checkOut: draft.checkOut,
    totalRooms: item.qty,
  }));
}

export function buildGuestCheckoutPayload(args: {
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  adultCount: number;
  childCount: number;
  paymentMethod?: string;
  notes?: string;
  draft?: BookingDraft;
}): GuestCheckoutPayload {
  const draft = args.draft ?? getDraft();

  return {
    customerName: args.customerName,
    customerEmail: args.customerEmail,
    customerPhone: args.customerPhone,
    adultCount: args.adultCount,
    childCount: args.childCount,
    paymentMethod: args.paymentMethod,
    notes: args.notes,
    items: buildGuestCheckoutItems(draft),
  };
}
