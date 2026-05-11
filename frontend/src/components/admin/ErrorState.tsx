type ErrorStateProps = {
  title?: string;
  message?: string;
  onRetry?: () => void;
};

export function ErrorState({
  title = "Unable to load data",
  message = "Please retry the request.",
  onRetry,
}: ErrorStateProps) {
  return (
    <div className="rounded-lg border border-red-200 bg-red-50 px-6 py-6">
      <p className="text-sm font-semibold text-red-700">{title}</p>
      <p className="mt-1 text-sm text-red-600">{message}</p>
      {onRetry && (
        <button
          type="button"
          onClick={onRetry}
          className="mt-4 rounded-md bg-red-700 px-3 py-2 text-sm font-semibold text-white"
        >
          Retry
        </button>
      )}
    </div>
  );
}
