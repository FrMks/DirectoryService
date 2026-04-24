"use client";

import { JSX } from "react";

export function LocationsListError({
  errorMessage,
}: LocationsListErrorProps): JSX.Element {
  return (
    <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
      <p>{errorMessage}</p>
    </div>
  );
}
