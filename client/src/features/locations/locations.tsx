"use client";

import { Location } from "@/entities/locations/type";
import { JSX, useState } from "react";
import { LocationCard } from "./location-card";
import { locationsApi } from "@/entities/locations/api";
import { LocationsListLoader } from "./locations-list-loader";
import { LocationsListError } from "./locations-list-error";
import { useQuery } from "@tanstack/react-query";
import { PaginationResponse } from "@/shared/api/types";
import {
  Pagination,
  PaginationContent,
  PaginationItem,
  PaginationPrevious,
  PaginationNext,
} from "@/shared/components/ui/pagination";

export function AppLocations(): JSX.Element {
  const [page, setPage] = useState(1);

  const {
    data: locationsResponse,
    isError,
    isFetching,
    error,
    refetch,
  } = useQuery<PaginationResponse<Location>, Error>({
    queryFn: () => locationsApi.getLocations({ page, pageSize: 10 }),
    queryKey: ["locations", page],
  });
  const locations = locationsResponse?.items ?? [];

  const totalPages = locationsResponse?.totalPages ?? 1;
  const canGoPrevious = page > 1;
  const canGoNext = page < totalPages;

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
      <Pagination>
        <PaginationContent>
          <PaginationItem>
            <PaginationPrevious
              href="#"
              aria-disabled={!canGoPrevious}
              className={
                !canGoPrevious ? "pointer-events-none opacity-50" : undefined
              }
              onClick={(event) => {
                event.preventDefault();
                if (canGoPrevious) {
                  setPage((currentPage) => currentPage - 1);
                }
              }}
            />
          </PaginationItem>

          <PaginationItem>
            {page} / {totalPages}
          </PaginationItem>

          <PaginationItem>
            <PaginationNext
              href="#"
              aria-disabled={!canGoNext}
              className={
                !canGoNext ? "pointer-events-none opacity-50" : undefined
              }
              onClick={(event) => {
                event.preventDefault();
                if (canGoNext) {
                  setPage((currentPage) => currentPage + 1);
                }
              }}
            />
          </PaginationItem>
        </PaginationContent>
      </Pagination>
    </section>
  );
}
