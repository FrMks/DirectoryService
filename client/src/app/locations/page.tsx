import { AppLocations } from "@/features/locations/locations";
import { JSX } from "react";

export default function LocationsPage(): JSX.Element {
  return (
    <main className="p-10">
      <AppLocations />
    </main>
  );
}
