import { Suspense } from "react";
import Homepage from "./Homepage";

export default function Page() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <Homepage />
    </Suspense>
  );
}
