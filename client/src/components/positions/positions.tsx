import { JSX } from "react";

export function AppPositions(): JSX.Element {
  return (
    <section className="max-w-3xl space-y-4">
      <div className="space-y-2">
        <h1 className="text-3xl font-semibold tracking-tight text-zinc-950">
          Позиции
        </h1>
        <p className="text-sm leading-6 text-zinc-600">
          Позиции задают справочник ролей компании и используются для описания
          кадровой и функциональной структуры подразделений.
        </p>
      </div>
    </section>
  );
}
