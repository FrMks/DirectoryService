"use client";

import {
  Pagination,
  PaginationContent,
  PaginationItem,
  PaginationNext,
  PaginationPrevious,
} from "@/shared/components/ui/pagination";

type LocationsPaginationProps = {
  page: number;
  totalPages: number;
  onPreviousPage: () => void;
  onNextPage: () => void;
};

export function LocationsPagination({
  page,
  totalPages,
  onPreviousPage,
  onNextPage,
}: LocationsPaginationProps) {
  const canGoPrevious = page > 1;
  const canGoNext = page < totalPages;

  return (
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
                onPreviousPage();
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
                onNextPage();
              }
            }}
          />
        </PaginationItem>
      </PaginationContent>
    </Pagination>
  );
}
