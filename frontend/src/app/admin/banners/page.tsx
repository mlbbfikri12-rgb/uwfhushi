"use client";

import { useState } from "react";

type Banner = {
  id: string;
  title: string;
  imageUrl: string;
  isActive: boolean;
};

export default function AdminBannersPage() {
  const [banners, setBanners] = useState<Banner[]>([]);
  const [title, setTitle] = useState("");
  const [imageUrl, setImageUrl] = useState("");

  const handleAdd = () => {
    if (!title || !imageUrl) return;

    const newBanner: Banner = {
      id: crypto.randomUUID(),
      title,
      imageUrl,
      isActive: true,
    };

    setBanners((prev) => [...prev, newBanner]);
    setTitle("");
    setImageUrl("");
  };

  const toggleActive = (id: string) => {
    setBanners((prev) =>
      prev.map((b) => (b.id === id ? { ...b, isActive: !b.isActive } : b)),
    );
  };

  const removeBanner = (id: string) => {
    setBanners((prev) => prev.filter((b) => b.id !== id));
  };

  return (
    <main className="p-6 space-y-6">
      <h1 className="text-xl font-bold">Banner Management</h1>

      {/* FORM */}
      <div className="bg-white border rounded-xl p-4 space-y-3">
        <input
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="Banner Title"
          className="w-full border rounded px-3 py-2"
        />

        <input
          value={imageUrl}
          onChange={(e) => setImageUrl(e.target.value)}
          placeholder="Image URL"
          className="w-full border rounded px-3 py-2"
        />

        <button
          onClick={handleAdd}
          className="bg-[#1a1f3c] text-white px-4 py-2 rounded"
        >
          Add Banner
        </button>
      </div>

      {/* LIST */}
      <div className="space-y-3">
        {banners.map((banner) => (
          <div
            key={banner.id}
            className="flex items-center justify-between border rounded-lg p-4 bg-white"
          >
            <div>
              <p className="font-semibold">{banner.title}</p>
              <p className="text-xs text-slate-500">{banner.imageUrl}</p>
            </div>

            <div className="flex gap-2">
              <button
                onClick={() => toggleActive(banner.id)}
                className={`px-3 py-1 text-xs rounded ${
                  banner.isActive
                    ? "bg-green-100 text-green-700"
                    : "bg-slate-100 text-slate-600"
                }`}
              >
                {banner.isActive ? "Active" : "Inactive"}
              </button>

              <button
                onClick={() => removeBanner(banner.id)}
                className="px-3 py-1 text-xs rounded bg-red-100 text-red-600"
              >
                Delete
              </button>
            </div>
          </div>
        ))}

        {banners.length === 0 && (
          <p className="text-sm text-slate-400">Belum ada banner</p>
        )}
      </div>
    </main>
  );
}
