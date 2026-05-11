type EmptyStateProps = {
  title?: string;
  description?: string;
};

export function EmptyState({
  title = "No data found",
  description = "Try adjusting the filter or create a new record.",
}: EmptyStateProps) {
  return (
    <div className="rounded-lg border border-dashed border-slate-300 bg-white px-6 py-10 text-center">
      <p className="text-sm font-semibold text-slate-800">{title}</p>
      <p className="mt-1 text-sm text-slate-500">{description}</p>
    </div>
  );
}
