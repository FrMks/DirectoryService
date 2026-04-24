import { AppPositions } from "@/features/positions/positions";
import { JSX } from "react";

export default function PositionsPage(): JSX.Element {
  return (
    <main className="p-10">
      <AppPositions />
    </main>
  );
}
