export function SummaryCard({
  title,
  value,
  isLoading,
}: {
  title: string;
  value: number;
  isLoading?: boolean;
}) {
  return (
    <div className="rounded-xl bg-white p-5 shadow-sm hover:shadow-md transition">
      <p className="text-sm text-slate-500">{title}</p>

      <p className="mt-2 text-3xl font-bold text-[#1a1f3c]">
        {isLoading ? "..." : value}
      </p>
    </div>
  );
}
