import { JSX } from "react";
import type { LocationCardProps } from "@/entities/locations/ui/location.card";

export function LocationCard({
  name,
  adress,
  isActive,
}: LocationCardProps): JSX.Element {
  return (
    <div className="rounded-lg border border-zinc-200 bg-white p-4 shadow-sm">
      <div className="items-start justify-around gap-4">
        <div className="space-y-1">
          <h3 className="text-base font-semibold text-zinc-900">{name}</h3>
          <p className="text-sm text-zinc-600">
            {adress.street}, {adress.city}, {adress.country}
          </p>
        </div>
      </div>

      <span
        className={
          isActive
            ? "rounded-full bg-green-100 px-2 py-1 text-xs font-medium text-green-700"
            : "rounded-full bg-zinc-100 px-2 py-1 text-xs font-medium text-zinc-600"
        }
      >
        {isActive ? "Активная" : "Неактивная"}
      </span>
    </div>
  );
}
