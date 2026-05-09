import type { Metadata } from "next";
import Homepage from "./Homepage";
import { getPublicHome } from "@/services/server/branch.service";

export const metadata: Metadata = {
  title: "Lynn Hotel - Book Your Stay with Confidence",
  description:
    "Discover the perfect hotel for your next trip with Lynn Hotel. Browse our wide selection of hotels, read reviews, and book with confidence. Your ideal stay awaits!",
};

// ISR 6 jam
export const revalidate = 60 * 10;

export default async function Page() {
  const data = await getPublicHome();
  return <Homepage initialData={data} />;
}
