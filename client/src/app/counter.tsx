"use client";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { JSX, useEffect, useState } from "react";

export default function Counter(): JSX.Element {
  const [counter, setCounter] = useState(0);

  useEffect(() => {
    console.log("Counter mounted");
  }, [counter]);

  function calculateSum(a: number, b: number): number {
    return a + b;
  }

  const handleClick = () => {
    setCounter(counter + 1);
  };

  return (
    <div className="flex flex-col gap-4">
      <CoolCount count={counter} />
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
