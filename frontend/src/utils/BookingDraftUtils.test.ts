import { beforeEach, describe, expect, it } from "vitest";
import {
  buildDraftItemKey,
  buildGuestCheckoutItems,
  buildGuestCheckoutPayload,
  getDraft,
  getDraftCount,
  getDraftTotal,
  saveDraft,
} from "@/utils/BookingDraftUtils";
import type { BookingDraftItem } from "@/types/BookingDraft";

const context = {
  branchCode: "SBY",
  slug: "hotel-surabaya",
  checkIn: "2026-05-10",
  checkOut: "2026-05-12",
};

function createItem(overrides: Partial<BookingDraftItem> = {}): BookingDraftItem {
  const base = {
    roomTypeId: "room-type-1",
    ratePlanId: "rate-plan-1",
  };

  return {
    key: buildDraftItemKey(context, base),
    ...base,
    roomTypeName: "Deluxe",
    ratePlanName: "Breakfast",
    qty: 1,
    price: 500000,
    ...overrides,
  };
}

describe("BookingDraftUtils", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("returns empty draft for malformed storage", () => {
    localStorage.setItem("booking_draft", "{not-json");

    const draft = getDraft();

    expect(draft.items).toHaveLength(0);
    expect(draft.version).toBe(1);
  });

  it("merges duplicate item keys when saving", () => {
    const item = createItem();

    saveDraft({
      version: 1,
      ...context,
      items: [item, { ...item, qty: 2 }],
    });

    const draft = getDraft();

    expect(draft.items).toHaveLength(1);
    expect(draft.items[0]?.qty).toBe(3);
  });

  it("calculates total rooms and total price", () => {
    saveDraft({
      version: 1,
      ...context,
      items: [
        createItem({ qty: 2, price: 500000 }),
        createItem({
          key: buildDraftItemKey(context, {
            roomTypeId: "room-type-2",
            ratePlanId: "rate-plan-2",
          }),
          roomTypeId: "room-type-2",
          ratePlanId: "rate-plan-2",
          qty: 1,
          price: 300000,
        }),
      ],
    });

    const draft = getDraft();

    expect(getDraftCount(draft)).toBe(3);
    expect(getDraftTotal(draft)).toBe(1300000);
  });

  it("builds aggregate guest checkout payload items", () => {
    saveDraft({
      version: 1,
      ...context,
      items: [createItem({ qty: 2 })],
    });

    const draft = getDraft();
    const items = buildGuestCheckoutItems(draft);
    const payload = buildGuestCheckoutPayload({
      customerName: "Budi",
      customerEmail: "budi@example.com",
      customerPhone: "08123",
      adultCount: 2,
      childCount: 1,
      paymentMethod: "mock",
      draft,
    });

    expect(items).toEqual([
      {
        roomTypeId: "room-type-1",
        ratePlanId: "rate-plan-1",
        checkIn: "2026-05-10",
        checkOut: "2026-05-12",
        totalRooms: 2,
      },
    ]);
    expect(payload.items).toHaveLength(1);
    expect(payload.customerEmail).toBe("budi@example.com");
  });
});
