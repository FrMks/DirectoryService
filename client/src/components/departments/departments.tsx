import { JSX } from "react";

export function AppDepartments(): JSX.Element {
  return (
    <section className="max-w-3xl space-y-4">
      <div className="space-y-2">
        <h1 className="text-3xl font-semibold tracking-tight text-zinc-950">
          Департаменты
        </h1>
        <p className="text-sm leading-6 text-zinc-600">
          Департаменты отражают внутреннюю организационную структуру бизнеса и
          помогают управлять подчинённостью, распределением функций и
          принадлежностью к локациям.
        </p>
      </div>
    </section>
  );
}
