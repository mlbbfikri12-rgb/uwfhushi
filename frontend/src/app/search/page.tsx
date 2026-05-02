import type { Metadata } from "next";
import SearchPageClient from "./search-page-client";

export const metadata: Metadata = {
  title: "Search Hotels",
  description: "Find hotels by city, date, and price.",
};

type PageProps = {
  searchParams: Record<string, string | string[] | undefined>;
};

export default function SearchPage({ searchParams }: PageProps) {
  return <SearchPageClient searchParams={searchParams} />;
}
