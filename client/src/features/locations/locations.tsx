"use client";

import { Location } from "@/entities/locations/type";
import { JSX, useState } from "react";
import { LocationCard } from "./location-card";
import { locationsApi } from "@/entities/locations/api";
import { LocationsListLoader } from "./locations-list-loader";
import { LocationsListError } from "./locations-list-error";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { PaginationResponse } from "@/shared/api/types";
import { LocationsPagination } from "./locations-pagination";
import { Button } from "@/shared/components/ui/button";

export function AppLocations(): JSX.Element {
  const queryClient = useQueryClient();

  const [page, setPage] = useState(1);

  const {
    data: locationsResponse,
    isError,
    isFetching,
    error,
    refetch,
  } = useQuery<PaginationResponse<Location>, Error>({
    queryFn: () =>
      locationsApi.getLocations({
        page,
        pageSize: 3,
        sortBy: "UpdatedAt",
        sortDirection: "desc",
      }),
    queryKey: ["locations", page],
  });
  const locations = locationsResponse?.items ?? [];

  const totalPages = locationsResponse?.totalPages ?? 1;

  const createLocationMutation = useMutation<string, Error>({
    mutationFn: () =>
      locationsApi.createLocation({
        name: "Новая локация 5",
        address: {
          street: "Тверская улица 5",
          city: "Москва 5",
          country: "Россия 5",
        },
        timezone: "Europe/Moscow",
      }),
    onSettled: async () => {
      setPage(1);
      await queryClient.invalidateQueries({ queryKey: ["locations"] });
    },
  });

  function handleCreateLocation() {
    createLocationMutation.mutate();
  }

  function handleLoadLocations() {
    void refetch();
  }

  function handlePreviousPage() {
    setPage((currentPage) => currentPage - 1);
  }

  function handleNextPage() {
    setPage((currentPage) => currentPage + 1);
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

      <div className="flex gap-2">
        <Button
          type="button"
          onClick={handleLoadLocations}
          className="rounded-md border px-4 py-2"
        >
          Load Locations
        </Button>

        <Button
          type="button"
          onClick={handleCreateLocation}
          disabled={createLocationMutation.isPending}
          className="rounded-md border px-4 py-2"
        >
          {createLocationMutation.isPending ? "Creating..." : "Create Location"}
        </Button>
      </div>

      {createLocationMutation.isError && (
        <LocationsListError
          errorMessage={
            createLocationMutation.error?.message ?? "Failed to create location"
          }
        />
      )}

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

      <LocationsPagination
        page={page}
        totalPages={totalPages}
        onPreviousPage={handlePreviousPage}
        onNextPage={handleNextPage}
      />
    </section>
  );
}
