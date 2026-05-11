
export function getImageUrl(path?: string | null) {
    if (!path) return "/images/placeholder.png";

    if (path.startsWith("http")) return path;

    return `${process.env.NEXT_PUBLIC_API_URL}/${path}`;
}