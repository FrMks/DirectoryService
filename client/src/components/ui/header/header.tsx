import { routes } from "@/shared/routes";
import { Menu, UserCircle2 } from "lucide-react";
import Link from "next/link";
import type { JSX } from "react";

export default function Header(): JSX.Element {
  return (
    <header className="sticky top-0 z-30 border-b border-zinc-200 bg-white/95 backdrop-blur supports-[backdrop-filter]:bg-white/80">
      <div className="mx-auto flex h-16 w-full max-w-[1440px] items-center gap-3 px-3 sm:px-4">
        <div className="flex min-w-0 items-center gap-3">
          <button
            type="button"
            aria-label="Open menu"
            className="inline-flex h-10 w-10 items-center justify-center rounded-full text-zinc-700 transition hover:bg-zinc-100"
          >
            <Menu className="h-5 w-5" />
          </button>

          <div className="flex items-center gap-2">
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-red-600 shadow-sm shadow-red-200">
              <div className="ml-0.5 h-0 w-0 border-y-[7px] border-l-[11px] border-y-transparent border-l-white" />
            </div>
            <Link href={routes.home}>
              <div className="text-sm font-semibold tracking-tight text-zinc-950">
                DirectoryService
              </div>
            </Link>
          </div>
        </div>

        <div className="ml-auto flex items-center">
          <button
            type="button"
            aria-label="Profile"
            className="inline-flex items-center rounded-full border border-zinc-200 bg-white p-1 shadow-sm shadow-zinc-100 transition hover:bg-zinc-50"
          >
            <span className="flex h-9 w-9 items-center justify-center rounded-full bg-gradient-to-br from-sky-500 via-blue-600 to-violet-600 text-white">
              <UserCircle2 className="h-5 w-5" />
            </span>
          </button>
        </div>
      </div>
    </header>
  );
}
