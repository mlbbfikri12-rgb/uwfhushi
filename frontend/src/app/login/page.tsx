import { Suspense } from "react";
import LoginPage from "./loginClient";

export default function Page() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <LoginPage />
    </Suspense>
  );
}