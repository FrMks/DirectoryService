"use client";

import { Location } from "@/entities/locations/type";
import { JSX } from "react";
import { LocationCard } from "./location-card";
import { locationsApi } from "@/entities/locations/api";
import { LocationsListLoader } from "./locations-list-loader";
import { LocationsListError } from "./locations-list-error";
import { useQuery } from "@tanstack/react-query";

export function AppLocations(): JSX.Element {
  const {
    data: locations = [],
    isError,
    isFetching,
    error,
    refetch,
  } = useQuery<Location[], Error>({
    queryFn: () => locationsApi.getLocations(),
    queryKey: ["locations"],
    enabled: false,
  });

  function handleLoadLocations() {
    void refetch();
  }

  return (
    <section className="max-w-3xl space-y-4">
      <div className="space-y-2">
        <h1 className="text-3xl font-semibold tracking-tight text-zinc-950">
          Локации
        </h1>
        <p className="text-sm leading-6 text-zinc-600">
          Локации позволяют описывать географическую структуру компании и
          использовать её в привязке отделов и сотрудников к конкретным местам
          работы.
        </p>
      </div>

      <button
        type="button"
        onClick={handleLoadLocations}
        className="rounded-md border px-4 py-2"
      >
        Load Locations
      </button>

      {isFetching && <LocationsListLoader />}
      {!isFetching && isError && (
        <LocationsListError
          errorMessage={error?.message ?? "Failed to load locations"}
        />
      )}

      <div className="space-y-3">
        {locations.map((location) => (
          <LocationCard
            key={location.id}
            name={location.name}
            adress={location.address}
            isActive={location.isActive}
          />
        ))}
      </div>
    </section>
  );
}
