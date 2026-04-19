"use client";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
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
      <Button onClick={handleClick} variant={"secondary"}>
        Увеличить
      </Button>

      <Input type="text" placeholder="Max Leiter"></Input>
    </div>
  );
}

type Props = {
  count: number;
};

function CoolCount({ count }: Props): JSX.Element {
  return <span className="text-red-500">{count}</span>;
}
