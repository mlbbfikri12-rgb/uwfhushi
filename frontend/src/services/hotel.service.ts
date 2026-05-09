import { PricingRoom } from "@/types/admin-rateplan";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "https://argillaceous-gwenn-overindulgent.ngrok-free.dev";

export async function getHotel(slug: string) {
  const res = await fetch(`${API_URL}/api/hotel/${slug}`, {
    next: { revalidate: 10 },
  });

  if (!res.ok) throw new Error("Failed fetch hotel");

  return res.json();
}


export async function getHotelPricing(params: {
  slug: string;
  checkIn: string;
  checkOut: string;
}) {
  const query = new URLSearchParams({
    checkIn: params.checkIn,
    checkOut: params.checkOut,
  });

  //console.log(`${API_URL}/api/hotel/${params.slug}/pricing?${query}`);

  const res = await fetch(
    `${API_URL}/api/hotel/${params.slug}/pricing?${query}`, {
    headers: {
      "ngrok-skip-browser-warning": "true",
    },
  }
  );


  if (!res.ok) throw new Error("Failed fetch pricing");

  return res.json() as Promise<PricingRoom[]>;
}


// 🔥 TAMBAHAN INI
export async function getRoomDetail(
  slug: string,
  roomTypeId: string
) {
  const res = await fetch(
    `${API_URL}/api/hotel/${slug}/room/${roomTypeId}`, {
    headers: {
      "ngrok-skip-browser-warning": "true",
    },
  }
  );

  if (!res.ok) throw new Error("Failed fetch room detail");

  return res.json();
}