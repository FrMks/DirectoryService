"use client";

import { Button } from "@/components/ui/button";
import { JSX } from "react";

export default function Counter(): JSX.Element {
  const count = calculateSum(5, 10);

  function calculateSum(a: number, b: number): number {
    return a + b;
  }

  const handleClick = () => {
    console.log(count);
  };

  return (
    <div className="flex flex-col gap-4">
      <CoolCount count={count} />
      <Button onClick={handleClick}>Увеличить</Button>
    </div>
  );
}

type Props = {
  count: number;
};

function CoolCount({ count }: Props): JSX.Element {
  return <span className="text-red-500">{count}</span>;
}
