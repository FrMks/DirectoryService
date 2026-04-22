"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  Building2,
  Home,
  MapPinned,
  SquareTerminal,
  UserRoundSearch,
} from "lucide-react";
import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarTrigger,
} from "@/components/ui/sidebar";
import { routes } from "@/shared/routes";

const navigationItems = [
  { href: routes.home, label: "Главная", icon: Home },
  { href: routes.locations, label: "Локации", icon: MapPinned },
  { href: routes.departments, label: "Отделы", icon: Building2 },
  { href: routes.positions, label: "Должности", icon: UserRoundSearch },
  { href: routes.counter, label: "Counter", icon: SquareTerminal },
  { href: routes.todo, label: "Todo", icon: SquareTerminal },
] as const;

export function AppSidebar() {
  const pathname = usePathname();

  return (
    <Sidebar collapsible="icon" className="border-r border-zinc-200 bg-white">
      <SidebarHeader className="h-16 flex-row items-center justify-start gap-0 border-b border-zinc-200 p-2 text-sm font-semibold text-zinc-900">
        <SidebarTrigger className="size-8 shrink-0 rounded-md p-0 hover:bg-zinc-100" />
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu className="mb-2">
              {navigationItems.map(({ href, label, icon: Icon }) => (
                <SidebarMenuItem key={href}>
                  <SidebarMenuButton
                    asChild
                    isActive={pathname === href}
                    className="data-[active=true]:bg-zinc-900 data-[active=true]:text-white data-[active=true]:hover:bg-zinc-900 data-[active=true]:hover:text-white"
                  >
                    <Link href={href}>
                      <Icon />
                      <span>{label}</span>
                    </Link>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
    </Sidebar>
  );
}
