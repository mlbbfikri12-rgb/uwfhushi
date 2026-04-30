import { Suspense } from "react";
import RegisterClient from "./registerClient";

export default function Page() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <RegisterClient />
    </Suspense>
  );
}
