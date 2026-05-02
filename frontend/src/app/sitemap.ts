import type { MetadataRoute } from "next";

async function getSlugs() {
  const siteUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";
  const [hotelsRes, blogsRes] = await Promise.all([
    fetch(`${siteUrl}/api/public/hotels/search?q=&checkIn=2026-06-01&checkOut=2026-06-02&totalRooms=1`, { cache: "no-store" }),
    fetch(`${siteUrl}/api/public/blogs`, { cache: "no-store" }),
  ]);

  const hotels = hotelsRes.ok ? await hotelsRes.json() : { hotels: [] };
  const blogs = blogsRes.ok ? await blogsRes.json() : [];

  return {
    hotelSlugs: (hotels.hotels as Array<{ branchCode: string }>).map((x) => x.branchCode.toLowerCase()),
    blogIds: (blogs as Array<{ id: string }>).map((x) => x.id),
  };
}

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const base = process.env.NEXT_PUBLIC_SITE_URL ?? "http://localhost:3000";
  const { hotelSlugs, blogIds } = await getSlugs();

  return [
    { url: `${base}/`, changeFrequency: "daily", priority: 1 },
    { url: `${base}/search`, changeFrequency: "daily", priority: 0.8 },
    ...hotelSlugs.map((slug) => ({ url: `${base}/hotel/${slug}`, changeFrequency: "daily" as const, priority: 0.9 })),
    ...blogIds.map((id) => ({ url: `${base}/blog/${id}`, changeFrequency: "weekly" as const, priority: 0.7 })),
  ];
}
