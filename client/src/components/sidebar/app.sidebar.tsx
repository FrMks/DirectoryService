"use client";

import Link from "next/link";
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
  { href: routes.counter, label: "Счетчик", icon: SquareTerminal },
  { href: routes.locations, label: "Локации", icon: MapPinned },
  { href: routes.departments, label: "Отделы", icon: Building2 },
  { href: routes.positions, label: "Должности", icon: UserRoundSearch },
] as const;

export function AppSidebar() {
  return (
    <Sidebar
      collapsible="icon"
      className="top-16 h-[calc(100svh-4rem)] border-r border-zinc-200 bg-white"
    >
      <SidebarHeader className="border-b border-zinc-200 px-3 py-4 text-sm font-semibold text-zinc-900">
        <SidebarTrigger />
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu className="mb-2">
              {navigationItems.map(({ href, label, icon: Icon }) => (
                <SidebarMenuItem key={href}>
                  <SidebarMenuButton asChild>
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
