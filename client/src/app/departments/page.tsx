import { AppDepartments } from "@/components/departments/departments";
import { JSX } from "react";

export default function DepartmentsPage(): JSX.Element {
  return (
    <main className="p-10">
      <AppDepartments />
    </main>
  );
}
