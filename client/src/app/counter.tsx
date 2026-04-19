"use client";

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
      <button
        onClick={handleClick}
        className="px-4 py-2 bg-blue-500 text-white font-medium rounded shadow-md hover:bg-blue-600 active:bg-blue-700 transition-colors duration-200"
      >
        Увеличить
      </button>
    </div>
  );
}

type Props = {
  count: number;
};

function CoolCount({ count }: Props): JSX.Element {
  return <span className="text-red-500">{count}</span>;
}
