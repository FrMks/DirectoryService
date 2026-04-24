"use client";

import { Location } from "@/entities/locations/type";
import { JSX, useState } from "react";
import { LocationCard } from "./location-card";
import { getLocations } from "@/entities/locations/api";
import { LocationsListLoader } from "./locations-list-loader";
import { LocationsListError } from "./locations-list-error";

export function AppLocations(): JSX.Element {
  const [locations, setLocations] = useState<Location[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState("");

  async function handleLoadLocations() {
    console.log("Loading locations...");
    try {
      setLoading(true);
      setError("");

      const response = await getLocations();

      setLocations(response);
    } catch (error) {
      console.error("Failed to load locations:", error);
      setError("Failed to load locations");
    } finally {
      console.log("Finished loading locations");
      setLoading(false);
    }
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

      {loading && <LocationsListLoader />}
      {!loading && error && <LocationsListError errorMessage={error} />}

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
