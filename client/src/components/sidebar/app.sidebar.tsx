"use client";

import { Link, Sidebar } from "lucide-react";
import {
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarHeader,
} from "../ui/sidebar";
import { routes } from "@/shared/routes";

export function AppSidebar() {
  return (
    <Sidebar>
      <SidebarHeader>Меню</SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupContent>
            <Link href={routes.home}>Главная</Link>
            <Link href={routes.counter}>Счетчик</Link>
            <Link href={routes.locations}>Локации</Link>
            <Link href={routes.departments}>Отделы</Link>
            <Link href={routes.positions}>Должности</Link>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
    </Sidebar>
  );
}
