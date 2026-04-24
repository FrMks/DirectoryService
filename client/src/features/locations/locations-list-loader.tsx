"use client";

import { JSX } from "react";

export function LocationsListLoader(): JSX.Element {
  return (
    <div className="rounded-lg border-zinc-200 bg-white p-4 text-sm text-zinc-600">
      <p>Загрузка локаций...</p>
    </div>
  );
}
