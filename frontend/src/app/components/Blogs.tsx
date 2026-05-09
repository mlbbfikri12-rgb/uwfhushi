import { PublicBlog } from "@/types/home";
import Image from "next/image";

function toImageUrl(url: string) {
  return /^https?:\/\//i.test(url)
    ? url
    : "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=1200&q=80";
}

type Props = {
  blogs: PublicBlog[];
};

export default function Blogs({ blogs }: Props) {
  return (
    <section className="mx-auto max-w-7xl px-6 py-10">
      <h2 className="mb-4 text-2xl font-bold text-slate-800">From Our Blog</h2>

      <div className="grid gap-5 md:grid-cols-3">
        {blogs.map((blog) => (
          <article
            key={blog.id}
            className="group overflow-hidden rounded-2xl border border-slate-200 bg-white transition-all hover:-translate-y-1 hover:shadow-xl"
          >
            <div className="relative h-56 overflow-hidden">
              <Image
                src={toImageUrl(blog.imageUrl)}
                alt={blog.title}
                fill
                className="object-cover transition-transform duration-500 group-hover:scale-105"
              />
            </div>

            <div className="space-y-3 p-5">
              <div className="flex items-center gap-2 text-xs text-slate-400">
                <span>
                  {new Date(blog.createdAt).toLocaleDateString("id-ID", {
                    day: "numeric",
                    month: "short",
                    year: "numeric",
                  })}
                </span>
              </div>

              <h3 className="line-clamp-2 text-lg font-semibold text-slate-900">
                {blog.title}
              </h3>

              <p className="line-clamp-3 text-sm leading-relaxed text-slate-500">
                {blog.content}
              </p>

              <button className="pt-2 text-sm font-semibold text-[#1a1f3c]">
                Read More →
              </button>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
