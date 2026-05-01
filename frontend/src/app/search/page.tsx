import { Suspense } from "react";
import SearchClient from "./ClientPage";

export default function Page() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <SearchClient />
    </Suspense>
  );
}
