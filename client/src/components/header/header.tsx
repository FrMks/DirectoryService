import { routes } from "@/shared/routes";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Menu } from "lucide-react";
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
            <Avatar className="size-9">
              <AvatarImage src="/avatar.png" alt="Profile avatar" />
              <AvatarFallback>FM</AvatarFallback>
            </Avatar>
          </button>
        </div>
      </div>
    </header>
  );
}
