"use client";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useCounter } from "@/hooks/use-counter";
import { JSX, useEffect, useState } from "react";

export default function Counter(): JSX.Element {
  const { counter, click, isWin } = useCounter();

  return (
    <div className="flex flex-col gap-4">
      <CoolCount count={counter} />
      <Button onClick={click} variant={"secondary"}>
        Увеличить
      </Button>

      <Input type="text" placeholder="Max Leiter"></Input>

      {isWin && <span>Поздравляю!</span>}
    </div>
  );
}

type Props = {
  count: number;
};

function CoolCount({ count }: Props): JSX.Element {
  return <span className="text-red-500">{count}</span>;
}
