import type { Metadata } from "next";
import localFont from "next/font/local";
import "./globals.css";
import "react-day-picker/dist/style.css";
import { Providers } from "@/app/providers";

const geistSans = localFont({
  src: "./fonts/GeistVF.woff",
  variable: "--font-geist-sans",
  weight: "100 900",
});
const geistMono = localFont({
  src: "./fonts/GeistMonoVF.woff",
  variable: "--font-geist-mono",
  weight: "100 900",
});

export const metadata: Metadata = {
  metadataBase: new URL(
    process.env.NEXT_PUBLIC_SITE_URL ?? "http://localhost:3000",
  ),
  title: {
    default: "Lynn Hotel",
    template: "%s | Lynn Hotel",
  },
  description: "Book hotels across Indonesia with best price and experience",
  keywords: ["hotel", "booking", "hotel indonesia", "travel", "reservation"],
  robots: {
    index: true,
    follow: true,
  },
  openGraph: {
    type: "website",
    siteName: "Hotel Booking",
    url: process.env.NEXT_PUBLIC_SITE_URL ?? "http://localhost:3000",
    images: [
      {
        url: "/opengraph-image.jpg",
        width: 1200,
        height: 630,
      },
    ],
  },
  twitter: {
    card: "summary_large_image",
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body
        className={`${geistSans.variable} ${geistMono.variable} antialiased`}
      >
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
