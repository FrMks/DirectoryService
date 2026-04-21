import { JSX } from "react";

export function AppLocations(): JSX.Element {
  return (
    <section className="max-w-3xl space-y-4">
      <div className="space-y-2">
        <h1 className="text-3xl font-semibold tracking-tight text-zinc-950">
          Локации
        </h1>
        <p className="text-sm leading-6 text-zinc-600">
          Локации позволяют описывать географическую структуру компании и
          использовать её в привязке отделов и сотрудников к конкретным местам
          работы.
        </p>
      </div>
    </section>
  );
}
