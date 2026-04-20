import { routes } from "@/shared/routes";
import Link from "next/link";
import type { JSX } from "react";

export default function Header(): JSX.Element {
  return (
    <header className="sticky top-0 z-30 border-b border-zinc-200 bg-white/95 backdrop-blur supports-[backdrop-filter]:bg-white/80">
      <div className="mx-auto flex h-16 w-full max-w-[1440px] items-center px-3 sm:px-4">
        <Link
          href={routes.home}
          className="text-sm font-semibold tracking-tight text-zinc-950 transition hover:text-zinc-700"
        >
          DirectoryService
        </Link>
      </div>
    </header>
  );
}
