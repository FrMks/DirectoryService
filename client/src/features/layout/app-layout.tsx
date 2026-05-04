"use client";

import { SidebarInset, SidebarProvider } from "@/shared/components/ui/sidebar";
import { QueryClientProvider } from "@tanstack/react-query";
import Header from "../header/header";
import { AppSidebar } from "../sidebar/app.sidebar";
import { queryClient } from "@/shared/api/query-client";

export default function Layout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <body className="min-h-full bg-zinc-50 text-zinc-950">
      <QueryClientProvider client={queryClient}>
        <SidebarProvider className="min-h-screen">
          <AppSidebar />
          <SidebarInset className="min-h-screen">
            <Header />
            <main className="p-10">{children}</main>
          </SidebarInset>
        </SidebarProvider>
      </QueryClientProvider>
    </body>
  );
}
